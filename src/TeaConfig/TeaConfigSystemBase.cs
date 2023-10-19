using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;

namespace TeaLib
{
	namespace TeaConfig
	{
		public class TeaConfigArgumentException : ArgumentException 
		{
			public TeaConfigArgumentException(string message) : base(message) {}
		}

		public abstract class TeaConfigSystemBase : ModSystem
		{
			public const string ON_CONFIG_SAVE_COMPLETE = "tealib.onConfigSaveComplete";

			public enum EnumTeaConfigChangeResponseState
			{
				NoPrivilege,
				RequestSuccess
			}

			[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
			public class TeaConfigServerBroadcastPacket
			{
				public string ConfigID {get; set;}
				public string Data {get; set;}
			}

			[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
			public class TeaConfigClientServerChangePacket
			{
				public string ConfigID {get; set;}
				public Dictionary<string, string> Data {get; set;}
			}

			[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
			public class TeaConfigServerChangeResponsePacket
			{
				public string ConfigID {get; set;}
				public EnumTeaConfigChangeResponseState State {get; set;}
				public Dictionary<string, string> StateData {get; set;}
			}

			#region Common
			public TeaConfigBase ServerConfig {get; protected set;}
			public TeaConfigBase ClientConfig {get; protected set;}

			public virtual string ConfigID => Mod.Info.ModID;
			public virtual string ConfigName => Mod.Info.Name;

			public override double ExecuteOrder() => 0.001;
			public override bool ShouldLoad(EnumAppSide forSide) => GetType() != typeof(TeaConfigSystemBase);

			public abstract void LoadConfigs(ICoreAPI api);

			public T LoadConfig<T>(ICoreAPI api) where T : TeaConfigBase, new()
			{
				T config = new();

				if (config.ConfigType == EnumTeaConfigApiSide.Client && api.Side == EnumAppSide.Server) return null;

				config.CreateConfigSettings();
				config.InitialiseSettings();

				if (config.ConfigType == EnumTeaConfigApiSide.Server)
				{
					CreateNetworkChannels(api);

					// Server configs are loaded only serverside and then sent to clients; the client loads a default until server values can be sent
					if (api.Side == EnumAppSide.Client) 
					{
						config.CreateConfigSettings();
						config.InitialiseSettings();
						return config;
					}
				}

				bool saveConfig = true;

				try
				{
					T loadedConfig = config.Load<T>(api, ConfigID);

					if (loadedConfig != null)
					{
						config = loadedConfig;
						config.InitialiseVersion(api, Mod.Info);
						saveConfig = false;
					}
					else
					{
						config.SetVersion(api, Mod.Info.Version, Mod.Info);
					}
				}
				catch
				{
					config.SetVersion(api, Mod.Info.Version, Mod.Info);

					ConfigLoadError($"{ConfigName}: Failed to load {GetConfigTypeString(config.ConfigType)} config! Default values restored", config, api);
				}

				try
				{
					saveConfig |= config.ApplyMigrations(api, Mod.Info);
				}
				catch
				{
					ConfigLoadError($"{ConfigName}: Failed to migrate {GetConfigTypeString(config.ConfigType)} config to newest version. Could migrate up to {config.Version}", config, api);
				}

				if (saveConfig)
				{
					config.Save(api, ConfigID);
				}

				config.CreateConfigSettings();
				config.InitialiseSettings();

				return config;
			}

			private void ConfigLoadError(string message, TeaConfigBase config, ICoreAPI api)
			{
				if (api is ICoreServerAPI serverAPI)
				{
					serverAPI.Logger.Log(EnumLogType.Warning, message);

					if (serverAPI.World.AllOnlinePlayers.Length > 0)
					{
						serverAPI.SendMessageToGroup(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
					}
					else
					{
						serverAPI.Event.PlayerNowPlaying += (IServerPlayer byPlayer) => 
						{
							if (byPlayer.HasPrivilege("controlserver"))
							{
								byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
							}
						};
					}
				}
				else
				{
					(api as ICoreClientAPI).ShowChatMessage(message);
				}
			}

			private string GetConfigTypeString(EnumTeaConfigApiSide configType)
			{
				return configType switch
				{
					EnumTeaConfigApiSide.Server => "server",
					EnumTeaConfigApiSide.Client => "client",
					_ => "unknown type of"
				};
			}

			private void CreateNetworkChannels(ICoreAPI api)
			{
				string channelName = $"teaconfig_server_{ConfigID}";

				if (ServerNetworkChannel == null && api is ICoreServerAPI serverAPI)
				{
					ServerNetworkChannel = serverAPI.Network.GetChannel(channelName);
					ServerNetworkChannel ??= serverAPI.Network.RegisterChannel(channelName)
						.RegisterMessageType<TeaConfigServerBroadcastPacket>()
						.RegisterMessageType<TeaConfigClientServerChangePacket>()
						.SetMessageHandler<TeaConfigClientServerChangePacket>(OnClientServerChangePacket)
						.RegisterMessageType<TeaConfigServerChangeResponsePacket>();

					return;
				}

				if (ClientNetworkChannel == null && api is ICoreClientAPI clientAPI)
				{
					ClientNetworkChannel = clientAPI.Network.GetChannel(channelName);
					ClientNetworkChannel ??= clientAPI.Network.RegisterChannel(channelName)
						.RegisterMessageType<TeaConfigServerBroadcastPacket>()
						.SetMessageHandler<TeaConfigServerBroadcastPacket>(OnServerBroadcastPacket)
						.RegisterMessageType<TeaConfigClientServerChangePacket>()
						.RegisterMessageType<TeaConfigServerChangeResponsePacket>()
						.SetMessageHandler<TeaConfigServerChangeResponsePacket>(OnServerChangeResponsePacket);
				}
			}

			public void SaveConfig(ICoreAPI api, TeaConfigBase config)
			{
				config.Save(api, ConfigID);

				if (config.ConfigType == EnumTeaConfigApiSide.Server) OnServerConfigChanged(api as ICoreServerAPI);
			}

			private TextCommandResult CommandSet(ICoreAPI api, TextCommandCallingArgs args)
			{
				string settingCode = (string)args[0];

				TeaConfigSetResult result = SetSettingValue(api, settingCode, args.RawArgs, out TeaConfigSetting setting);

				string resultMessage = result.type switch
				{
					EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER => $"{ConfigName}: client settings cannot be set on server",
					EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT => $"{ConfigName}: setting '{setting.Code}' can't be changed on the client",
					EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE => $"{ConfigName}: setting '{setting.Code}' can't be changed",
					EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE => $"{ConfigName}: {result.resultInfo}",
					EnumTeaConfigSetResultType.ERROR_EXCEPTION => $"{ConfigName}: error setting '{setting.Code}': {result.resultInfo}",
					EnumTeaConfigSetResultType.ERROR_NO_VALUE => $"{ConfigName}: no value provided to change the setting to",
					EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING => $"{ConfigName}: unknown setting '{settingCode}'",
					EnumTeaConfigSetResultType.SUCCESS => result.resultInfo != null ? $"{ConfigName}: set '{setting.Code}' to {result.resultInfo}"
							: $"{ConfigName}: setting '{setting.Code}' successfully changed",
					_ => $"{ConfigName}: unknown result"
				};

				if (result.type == EnumTeaConfigSetResultType.SUCCESS)
				{
					SaveConfig(api, setting.Config);
					return TextCommandResult.Success(resultMessage);
				}

				return TextCommandResult.Error(resultMessage);
			}

			public struct TeaConfigSetResult
			{
				public EnumTeaConfigSetResultType type;
				public string resultInfo;
			}

			public enum EnumTeaConfigSetResultType
			{
				ERROR_CLIENT_SETTING_ON_SERVER,
				ERROR_SERVER_SETTING_ON_CLIENT,
				ERROR_SET_NOT_AVAILABLE,
				ERROR_EXCEPTION_PARSE,
				ERROR_EXCEPTION,
				ERROR_NO_VALUE,
				ERROR_UNKNOWN_SETTING,
				SUCCESS
			}

			// Returns true on setting being changed successfully, false otherwise
			private TeaConfigSetResult SetSettingValue(ICoreAPI api, string settingCode, CmdArgs args, out TeaConfigSetting setting)
			{
				ReadOnlyCollection<TeaConfigSetting> settingList = api.Side == EnumAppSide.Server ? ServerConfig.ConfigSettings : ClientConfig.ConfigSettings;
				setting = settingList.Where(setting => setting.Code == settingCode).FirstOrDefault();

				if (setting != null)
				{
					if (args.Length > 0)
					{
						if (setting.Config.ConfigType == EnumTeaConfigApiSide.Client && api.Side == EnumAppSide.Server) 
						{
							return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER };
						}
						// Change to a check to see if this config setting exists on the client 
						//if (setting.Config.ConfigType == EnumTeaConfigType.Server && api.Side == EnumAppSide.Client) return;

						if (setting.Config.ConfigType == EnumTeaConfigApiSide.Server && api.Side == EnumAppSide.Client)
						{
							return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT };
						}

						if (setting.Set == null)
						{
							return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE };
						}

						string resultInfo = null;

						try
						{
							resultInfo = setting.SetAsString(args);
						}
						catch (Exception exception)
						{
							EnumTeaConfigSetResultType exceptionResultType = exception is TeaConfigArgumentException ? EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE : EnumTeaConfigSetResultType.ERROR_EXCEPTION;
							return new TeaConfigSetResult { type = exceptionResultType, resultInfo = exception.Message };
						}

						return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.SUCCESS, resultInfo = resultInfo };
					}
					else
					{
						return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.ERROR_NO_VALUE };
					}
				}
				else
				{
					return new TeaConfigSetResult { type = EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING };
				}
			}

			private TextCommandResult CommandGet(ICoreAPI api, TextCommandCallingArgs args)
			{
				string settingCode = (string)args[0];

				TeaConfigGetResult result = GetSettingValue(api, settingCode, out TeaConfigSetting setting);

				string resultMessage = result.type switch
				{
					EnumTeaConfigGetResultType.ERROR_CLIENT_SETTING_ON_SERVER => $"{ConfigName}: client settings cannot be read on server",
					EnumTeaConfigGetResultType.ERROR_GET_NOT_AVAILABLE => $"{ConfigName}: cannot use get with '{setting.Code}",
					EnumTeaConfigGetResultType.ERROR_EXCEPTION => $"{ConfigName}: error reading '{setting.Code}': {result.resultInfo}",
					EnumTeaConfigGetResultType.ERROR_UNKNOWN_SETTING => $"{ConfigName}: unknown setting '{settingCode}'",
					EnumTeaConfigGetResultType.SUCCESS => $"{ConfigName}: '{setting.Code}' is currently {result.resultInfo}",
					EnumTeaConfigGetResultType.SUCCESS_NO_VALUE_EXPOSED => $"{ConfigName}: setting '{setting.Code}' shows no value to be displayed",
					_ => $"{ConfigName}: unknown result"
				};

				return result.type == EnumTeaConfigGetResultType.SUCCESS || result.type == EnumTeaConfigGetResultType.SUCCESS_NO_VALUE_EXPOSED 
					? TextCommandResult.Success(resultMessage) : TextCommandResult.Error(resultMessage);
			}

			public struct TeaConfigGetResult
			{
				public EnumTeaConfigGetResultType type;
				public string resultInfo;
			}

			public enum EnumTeaConfigGetResultType
			{
				ERROR_CLIENT_SETTING_ON_SERVER,
				ERROR_GET_NOT_AVAILABLE,
				ERROR_EXCEPTION,
				ERROR_UNKNOWN_SETTING,
				SUCCESS,
				SUCCESS_NO_VALUE_EXPOSED
			}

			private TeaConfigGetResult GetSettingValue(ICoreAPI api, string settingCode, out TeaConfigSetting setting)
			{
				ReadOnlyCollection<TeaConfigSetting> settingList = api.Side == EnumAppSide.Server ? ServerConfig.ConfigSettings : ClientConfig.ConfigSettings;
				setting = settingList.Where(setting => setting.Code == settingCode).FirstOrDefault();

				if (setting != null)
				{
					if (setting.Config.ConfigType == EnumTeaConfigApiSide.Client && api.Side == EnumAppSide.Server) {
						return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.ERROR_CLIENT_SETTING_ON_SERVER };
					}
					// Change to a check to see if this config setting exists on the client 
					//if (setting.Config.ConfigType == EnumTeaConfigType.Server && api.Side == EnumAppSide.Client) return;

					if (setting.Get == null)
					{
						return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.ERROR_GET_NOT_AVAILABLE };
					}

					string result = null;

					try
					{
						result = setting.GetAsString();
					}
					catch (Exception exception)
					{
						return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.ERROR_EXCEPTION, resultInfo = exception.Message };
					}

					if (result != null)
					{
						return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.SUCCESS, resultInfo = result }; 
					}

					return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.SUCCESS_NO_VALUE_EXPOSED }; 
				}
				else
				{
					return new TeaConfigGetResult { type = EnumTeaConfigGetResultType.ERROR_UNKNOWN_SETTING };
				}
			}

			private void CreateChatCommands(ICoreAPI api)
			{
				ReadOnlyCollection<TeaConfigSetting> settingList = api.Side == EnumAppSide.Server ? ServerConfig?.ConfigSettings : ClientConfig?.ConfigSettings;
				if (settingList == null) return;

				string[] settingSuggestions = settingList.Select(setting => setting.Code).ToArray();

				api.ChatCommands
				.GetOrCreate("tmodconfig")
				.BeginSubCommand(ConfigID)
					.BeginSubCommand("set")
						.HandleWith((args) => {return CommandSet(api, args);})
						.WithArgs(
							api.ChatCommands.Parsers.Word("setting", settingSuggestions),
							api.ChatCommands.Parsers.Unparsed("value")
						)
					.EndSubCommand()
					.BeginSubCommand("get")
						.HandleWith((args) => {return CommandGet(api, args);})
						.WithArgs(
							api.ChatCommands.Parsers.Word("setting", settingSuggestions)
						)
					.EndSubCommand()
					// TODO: Consider making this root-level alias opt-out
					.WithRootAlias(ConfigID)
				.EndSubCommand();

				if (api.Side == EnumAppSide.Server)	{
					api.ChatCommands.Get("tmodconfig").RequiresPrivilege(Privilege.controlserver);
					api.ChatCommands.Get(ConfigID).RequiresPrivilege(Privilege.controlserver);
				}
			}

			public override void Start(ICoreAPI api)
			{
				LoadConfigs(api);
				CreateChatCommands(api);
			}
			#endregion

			#region Server
			protected ICoreServerAPI sapi {get; private set;}
			protected IServerNetworkChannel ServerNetworkChannel {get; private set;}

			public override void StartServerSide(ICoreServerAPI api)
			{
				sapi = api;

				if (ServerConfig != null) api.Event.PlayerJoin += OnPlayerJoin;
			}

			private void OnPlayerJoin(IServerPlayer player)
			{
				SendServerConfigToPlayer(player);
			}

			public void OnServerConfigChanged(ICoreServerAPI api)
			{
				foreach (IServerPlayer player in api.World.AllOnlinePlayers.Cast<IServerPlayer>())
				{
					SendServerConfigToPlayer(player);
				}
			}

			private void SendServerConfigToPlayer(IServerPlayer player)
			{
				if (ServerConfig == null)
				{
					sapi.Logger.Warning($"{ConfigName}: Tried to send non-existent server config to player {player.PlayerName}!");
					return;
				}

				TeaConfigServerBroadcastPacket packet = new() {
					ConfigID = ConfigID,
					Data = JsonConvert.SerializeObject(ServerConfig)
				};

				ServerNetworkChannel.SendPacket(packet, player);
			}

			public void OnClientServerChangePacket(IServerPlayer fromPlayer, TeaConfigClientServerChangePacket packet)
			{
				if (ConfigID != packet.ConfigID) return;

				EnumTeaConfigChangeResponseState state;
				Dictionary<string, string> stateData = new();

				if (!fromPlayer.HasPrivilege(Privilege.controlserver))
				{
					state = EnumTeaConfigChangeResponseState.NoPrivilege;
					sapi.Logger.Warning($"{ConfigName}: Player {fromPlayer.PlayerName} tried to change server settings despite not having the privileges to do so!");
					return;
				}

				bool reloadRequired = false;
				int successCount = 0;
				int errorCount = 0;

				foreach (KeyValuePair<string, string> settingPair in packet.Data)
				{
					string settingCode = settingPair.Key;
					string settingValue = settingPair.Value;

					TeaConfigSetResult result = SetSettingValue(sapi, settingCode, new CmdArgs(settingValue), out TeaConfigSetting configSetting);

					switch (result.type)
					{
						case EnumTeaConfigSetResultType.SUCCESS: successCount++; reloadRequired |= !configSetting.Flags.HasFlag(TeaConfigSettingFlags.RestartNotNeeded); break;
						case EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER: sapi.Logger.Error($"{ConfigName}: attempted to set client setting '{settingCode}' on server"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT: sapi.Logger.Error($"{ConfigName}: attempted to set server setting '{settingCode}' on client"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE: sapi.Logger.Error($"{ConfigName}: attempted to change un-changeable setting '{settingCode}'"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE: sapi.Logger.Error($"{ConfigName}: wrong value when changing setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_EXCEPTION: sapi.Logger.Error($"{ConfigName}: error when setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_NO_VALUE: sapi.Logger.Error($"{ConfigName}: no value provided to change setting '{settingCode}'"); errorCount++; break;
						case EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING: sapi.Logger.Error($"{ConfigName}: attempted to change unknown setting '{settingCode}'"); errorCount++; break;
						default: sapi.Logger.Error($"{ConfigName}: unknown error when changing setting '{settingCode}'"); errorCount++; break;
					}
				}

				SaveConfig(sapi, ServerConfig);

				state = EnumTeaConfigChangeResponseState.RequestSuccess;

				stateData["successCount"] = successCount.ToString();
				stateData["errorCount"] = errorCount.ToString();
				stateData["reloadRequired"] = reloadRequired ? "1" : "0";

				TeaConfigServerChangeResponsePacket responsePacket = new() {
					ConfigID = ConfigID,
					State = state,
					StateData = stateData
				};

				ServerNetworkChannel.SendPacket(responsePacket, fromPlayer);
			}
			#endregion

			#region Client
			protected ICoreClientAPI capi {get; set;}
			protected IClientNetworkChannel ClientNetworkChannel {get; set;}

			public override void StartClientSide(ICoreClientAPI api)
			{
				capi = api;

				TeaConfigDialogSystem.CollectRegisteredSettingsEvent += () => 
				{
					return new TeaConfigModSettings()
					{
						ConfigID = ConfigID,
						ConfigName = ConfigName,
						ClientSettings = ClientConfig?.ConfigSettings,
						ServerSettings = ServerConfig?.ConfigSettings
					};
				};

				capi.Event.RegisterEventBusListener(OnSettingDialogSaved, filterByEventName: TeaConfigDialogSystem.ON_DIALOG_SAVED_EVENT);
			}

			private void OnSettingDialogSaved(string eventName, ref EnumHandling handling, IAttribute data)
			{
				TreeAttribute dataTree = data as TreeAttribute;
				TreeAttribute configListTree = dataTree?["configs"] as TreeAttribute;
				if (configListTree == null) return;

				foreach (KeyValuePair<string, IAttribute> configPair in configListTree)
				{
					if (configPair.Key != ConfigID) continue;

					TreeAttribute configSidesTree = configListTree[configPair.Key] as TreeAttribute;

					if (configSidesTree?[Enum.GetName(EnumTeaConfigApiSide.Client)] is TreeAttribute clientDataTree)
					{
						int successCount = 0;
						int errorCount = 0;
						bool reloadRequired = false;

						foreach (KeyValuePair<string, IAttribute> settingPair in clientDataTree)
						{
							string settingCode = settingPair.Key;
							string settingValue = (settingPair.Value as StringAttribute).value;

							TeaConfigSetResult result = SetSettingValue(capi, settingCode, new CmdArgs(settingValue), out TeaConfigSetting configSetting);

							switch (result.type)
							{
								case EnumTeaConfigSetResultType.SUCCESS: successCount++; reloadRequired |= !configSetting.Flags.HasFlag(TeaConfigSettingFlags.RestartNotNeeded); break;
								case EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER: capi.Logger.Error($"{ConfigName}: attempted to set client setting '{settingCode}' on server"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT: capi.Logger.Error($"{ConfigName}: attempted to set server setting '{settingCode}' on client"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE: capi.Logger.Error($"{ConfigName}: attempted to change un-changeable setting '{settingCode}'"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE: capi.Logger.Error($"{ConfigName}: wrong value when changing setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_EXCEPTION: capi.Logger.Error($"{ConfigName}: error when setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_NO_VALUE: capi.Logger.Error($"{ConfigName}: no value provided to change setting '{settingCode}'"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING: capi.Logger.Error($"{ConfigName}: attempted to change unknown setting '{settingCode}'"); errorCount++; break;
								default: capi.Logger.Error($"{ConfigName}: unknown error when changing setting '{settingCode}'"); errorCount++; break;
							}
						}

						SaveConfig(capi, ClientConfig);

						TreeAttribute saveCompleteEventDataTree = new();
						saveCompleteEventDataTree.SetString("configID", ConfigID);
						saveCompleteEventDataTree.SetString("sideName", Enum.GetName(EnumTeaConfigApiSide.Client));
						saveCompleteEventDataTree.SetString("state", EnumTeaConfigChangeResponseState.RequestSuccess.ToString());

						// TODO: Extension method to turn a TreeAttribute into a dictionary (and the other way around?)
						TreeAttribute stateDataTree = new();
						stateDataTree.SetString("successCount", successCount.ToString());
						stateDataTree.SetString("errorCount", errorCount.ToString());
						stateDataTree.SetString("reloadRequired", reloadRequired ? "1" : "0");

						saveCompleteEventDataTree.SetAttribute("stateData", stateDataTree);

						capi.Event.PushEvent(ON_CONFIG_SAVE_COMPLETE, saveCompleteEventDataTree);
					}

					if (configSidesTree?[Enum.GetName(EnumTeaConfigApiSide.Server)] is TreeAttribute serverDataTree)
					{
						TeaConfigClientServerChangePacket packet = new() {
							ConfigID = ConfigID,
							Data = new Dictionary<string, string>()
						};

						foreach (KeyValuePair<string, IAttribute> settingPair in serverDataTree)
						{
							string settingCode = settingPair.Key;
							string settingValue = (settingPair.Value as StringAttribute).value;

							packet.Data[settingCode] = settingValue;
						}

						ClientNetworkChannel.SendPacket(packet);
					}

					break;
				}

				handling = EnumHandling.PassThrough;
			}

			private void OnServerChangeResponsePacket(TeaConfigServerChangeResponsePacket packet)
			{
				if (packet.ConfigID != ConfigID) return;
				
				TreeAttribute saveCompleteEventDataTree = new();
				saveCompleteEventDataTree.SetString("configID", ConfigID);
				saveCompleteEventDataTree.SetString("sideName", Enum.GetName(EnumTeaConfigApiSide.Server));
				saveCompleteEventDataTree.SetString("state", packet.State.ToString());

				TreeAttribute stateDataTree = new();
				if (packet.StateData != null)
				{
					foreach (KeyValuePair<string, string> kvPair in packet.StateData) stateDataTree.SetString(kvPair.Key, kvPair.Value);
				}
				saveCompleteEventDataTree.SetAttribute("stateData", stateDataTree);

				capi.Event.PushEvent(ON_CONFIG_SAVE_COMPLETE, saveCompleteEventDataTree);
			}

			private void OnServerBroadcastPacket(TeaConfigServerBroadcastPacket packet)
			{
				if (packet.ConfigID != ConfigID) return;

				try
				{
					JsonConvert.PopulateObject(packet.Data, ServerConfig);
				}
				catch (Exception exception)
				{
					capi.ShowChatMessage($"Failed to synchronise {packet.ConfigID} config sent from server! Mod will likely not work correctly on your computer, please report this issue.");
					capi.Logger.Error($"Failed to deserialize {packet.ConfigID} config sent from server!\n{exception}");
				}
			}
			#endregion

			#region Dispose
			public override void Dispose()
			{
				ServerConfig = null;
				ClientConfig = null;

				sapi = null;
				ServerNetworkChannel = null;

				capi = null;
				ClientNetworkChannel = null;
			}
			#endregion
		}
	}
}