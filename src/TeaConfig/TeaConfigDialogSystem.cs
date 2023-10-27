using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Linq;
using Vintagestory.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using ProtoBuf;
using System.Reflection;
using Newtonsoft.Json;

using Cairo;

namespace TeaLib
{
	namespace TeaConfig
	{
		public class TeaConfigDialogSystem : ModSystem
		{
			public const string ON_DIALOG_SAVED_EVENT = "tealib.onDialogConfigSaved";

			// Load just before TeaConfigSystem
			public override double ExecuteOrder() => 0.0009;

			public override bool ShouldLoad(EnumAppSide forSide) => true;

			public delegate TeaConfigModSettings CollectRegisteredSettingsEventhandler();
			public static event CollectRegisteredSettingsEventhandler CollectRegisteredSettingsEvent;

			private ICoreClientAPI _capi;
			private HashSet<(string configID, string sideString)> _pendingConfigs = new();
			private int _pendingSuccessCount = 0;
			private int _pendingErrorCount = 0;
			private bool _pendingRequiresRestart = false;
			private bool _pendingFromServer = false;
			private long _saveTimeoutId = -1;

			public override void StartClientSide(ICoreClientAPI api)
			{
				_capi = api;

				api.ChatCommands
				.GetOrCreate("tmodconfig")
				.HandleWith((args) => {
					OpenModDialog();
					return TextCommandResult.Success(""); 
				});

				_capi.Gui.RegisterDialog(new TeaConfigDialog(api));
				_capi.Event.RegisterEventBusListener(OnConfigSaveComplete, filterByEventName: TeaConfigSystemBase.ON_CONFIG_SAVE_COMPLETE);
			}

			public void OpenModDialog()
			{
				List<TeaConfigModSettings> settingsList = new();

				foreach (Delegate eventhandler in CollectRegisteredSettingsEvent.GetInvocationList())
				{
					if (eventhandler.DynamicInvoke() is TeaConfigModSettings settingList) settingsList.Add(settingList);
				}

				TeaConfigDialog.GetLoaded(_capi).OpenWithData(settingsList);
			}

			public void SendSavedSettings(TreeAttribute dataTree)
			{
				if (dataTree.GetAttribute("configs") is not TreeAttribute configsTree) return;

				foreach (KeyValuePair<string, IAttribute> configPair in configsTree)
				{
					string configID = configPair.Key;
					TreeAttribute configSidesTree = configPair.Value as TreeAttribute;

					foreach (KeyValuePair<string, IAttribute> sidePair in configSidesTree)
					{
						string sideName = sidePair.Key;

						_pendingConfigs.Add((configID, sideName));
					}
				}

				_capi.Event.PushEvent(ON_DIALOG_SAVED_EVENT, dataTree);

				_saveTimeoutId = _capi.Event.RegisterCallback((time) => {
					OnAllConfigSavesComplete();
				}, 10000);
			}

			private void OnConfigSaveComplete(string eventName, ref EnumHandling handling, IAttribute data)
			{
				if (data is not TreeAttribute dataTree) return;
				if (!dataTree.HasAttribute("configID") || !dataTree.HasAttribute("sideName")) return;

				_pendingConfigs.Remove((dataTree.GetString("configID"), dataTree.GetString("sideName")));

				if (dataTree["stateData"] is not TreeAttribute stateDateTree) return;

				if (int.TryParse(stateDateTree.GetString("successCount", "0"), out int incomingSuccessCount)) _pendingSuccessCount += incomingSuccessCount;
				if (int.TryParse(stateDateTree.GetString("errorCount", "0"), out int incomingErrorCount)) _pendingErrorCount += incomingErrorCount;

				_pendingRequiresRestart |= stateDateTree.GetString("reloadRequired", "0") == "1";
				_pendingFromServer |= stateDateTree.GetString("sideName") == Enum.GetName(EnumTeaConfigApiSide.Server);

				if (_pendingConfigs.Count <= 0) OnAllConfigSavesComplete();
			}

			private void OnAllConfigSavesComplete()
			{
				if (_pendingConfigs.Count > 0)
				{
					_capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-no-response", _pendingConfigs.Count));

					string message = "";

					if (_pendingSuccessCount > 0) message += $"{Lang.Get("tealib:chat-message-config-save-success-partial", _pendingSuccessCount)} ";
					if (_pendingErrorCount > 0) message += Lang.Get("tealib:chat-message-config-save-error-partial", _pendingErrorCount);

					_capi.ShowChatMessage(message);
				}
				else
				{
					if (_pendingErrorCount == 0 && _pendingSuccessCount > 0)
					{
						_capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-success"));
					}
					else if (_pendingErrorCount > 0)
					{
						_capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-error", _pendingSuccessCount, _pendingErrorCount));
					}
				}
				
				if (_pendingRequiresRestart && _pendingSuccessCount > 0)
				{
					if (_capi.IsSinglePlayer)
					{
						_capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-reload-world"));
					}
					else
					{
						if (_pendingFromServer) _capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-restart-server"));
						else _capi.ShowChatMessage(Lang.Get("tealib:chat-message-config-save-rejoin-server"));
					}
				}

				_pendingSuccessCount = 0;
				_pendingErrorCount = 0;
				_pendingRequiresRestart = false;
				_pendingFromServer = false;

				if (_saveTimeoutId > 0) _capi.Event.UnregisterCallback(_saveTimeoutId);
				_saveTimeoutId = -1;
			}

			public override void Dispose()
			{
				CollectRegisteredSettingsEvent = null;

				_capi = null;
			}
		}
	}
}