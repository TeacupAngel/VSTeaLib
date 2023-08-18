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
			public override bool ShouldLoad(EnumAppSide forSide) => false;

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
					T loadedConfig = api.LoadModConfig<T>($"{config.ConfigName}.json");

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

					ConfigLoadError($"{Mod.Info.Name}: Failed to load {GetConfigTypeString(config.ConfigType)} config file {config.ConfigName}.json! Default values restored", config, api);
				}

				try
				{
					saveConfig |= config.ApplyMigrations(api, Mod.Info);
				}
				catch
				{
					ConfigLoadError($"{Mod.Info.Name}: Failed to migrate {GetConfigTypeString(config.ConfigType)} config file {config.ConfigName}.json to newest version. Could migrate up to {config.Version}", config, api);
				}

				if (saveConfig)
				{
					config.Save(api);
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
				if (ServerNetworkChannel == null && api is ICoreServerAPI serverAPI)
				{
					ServerNetworkChannel = serverAPI.Network.GetChannel("teaconfigserver");
					ServerNetworkChannel ??= serverAPI.Network.RegisterChannel("teaconfigserver")
						.RegisterMessageType<TeaConfigServerBroadcastPacket>()
						.RegisterMessageType<TeaConfigClientServerChangePacket>()
						.SetMessageHandler<TeaConfigClientServerChangePacket>(OnClientServerChangePacket)
						.RegisterMessageType<TeaConfigServerChangeResponsePacket>();

					return;
				}

				if (ClientNetworkChannel == null && api is ICoreClientAPI clientAPI)
				{
					ClientNetworkChannel = clientAPI.Network.GetChannel("teaconfigserver");
					ClientNetworkChannel ??= clientAPI.Network.RegisterChannel("teaconfigserver")
						.RegisterMessageType<TeaConfigServerBroadcastPacket>()
						.SetMessageHandler<TeaConfigServerBroadcastPacket>(OnServerBroadcastPacket)
						.RegisterMessageType<TeaConfigClientServerChangePacket>()
						.RegisterMessageType<TeaConfigServerChangeResponsePacket>()
						.SetMessageHandler<TeaConfigServerChangeResponsePacket>(OnServerChangeResponsePacket);
				}
			}

			public void SaveConfig(ICoreAPI api, TeaConfigBase config)
			{
				config.Save(api);

				if (config.ConfigType == EnumTeaConfigApiSide.Server) OnServerConfigChanged(api as ICoreServerAPI);
			}

			private TextCommandResult CommandSet(ICoreAPI api, TextCommandCallingArgs args)
			{
				string settingCode = (string)args[0];

				TeaConfigSetResult result = SetSettingValue(api, settingCode, args.RawArgs, out TeaConfigSetting setting);

				string resultMessage = result.type switch
				{
					EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER => $"{Mod.Info.Name}: client settings cannot be set on server",
					EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT => $"{Mod.Info.Name}: setting '{setting.Code}' can't be changed on the client",
					EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE => $"{Mod.Info.Name}: setting '{setting.Code}' can't be changed",
					EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE => $"{Mod.Info.Name}: {result.resultInfo}",
					EnumTeaConfigSetResultType.ERROR_EXCEPTION => $"{Mod.Info.Name}: error setting '{setting.Code}': {result.resultInfo}",
					EnumTeaConfigSetResultType.ERROR_NO_VALUE => $"{Mod.Info.Name}: no value provided to change the setting to",
					EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING => $"{Mod.Info.Name}: unknown setting '{settingCode}'",
					EnumTeaConfigSetResultType.SUCCESS => result.resultInfo != null ? $"{Mod.Info.Name}: set '{setting.Code}' to {result.resultInfo}"
							: $"{Mod.Info.Name}: setting '{setting.Code}' successfully changed",
					_ => $"{Mod.Info.Name}: unknown result"
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
							resultInfo = setting.StringSet(args);
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
					EnumTeaConfigGetResultType.ERROR_CLIENT_SETTING_ON_SERVER => $"{Mod.Info.Name}: client settings cannot be read on server",
					EnumTeaConfigGetResultType.ERROR_GET_NOT_AVAILABLE => $"{Mod.Info.Name}: cannot use get with '{setting.Code}",
					EnumTeaConfigGetResultType.ERROR_EXCEPTION => $"{Mod.Info.Name}: error reading '{setting.Code}': {result.resultInfo}",
					EnumTeaConfigGetResultType.ERROR_UNKNOWN_SETTING => $"{Mod.Info.Name}: unknown setting '{settingCode}'",
					EnumTeaConfigGetResultType.SUCCESS => $"{Mod.Info.Name}: '{setting.Code}' is currently {result.resultInfo}",
					EnumTeaConfigGetResultType.SUCCESS_NO_VALUE_EXPOSED => $"{Mod.Info.Name}: setting '{setting.Code}' shows no value to be displayed",
					_ => $"{Mod.Info.Name}: unknown result"
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
						result = setting.StringGet();
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
				ReadOnlyCollection<TeaConfigSetting> settingList = api.Side == EnumAppSide.Server ? ServerConfig.ConfigSettings : ClientConfig.ConfigSettings;
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

				api.Event.PlayerJoin += OnPlayerJoin;
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

			public class TeaConfigSerializationBinder : SerializationBinder
			{
				public TeaConfigSerializationBinder() {}

				public override Type BindToType(string assemblyName, string typeName)
				{
					Type returnType = Assembly.GetExecutingAssembly().GetType(typeName);

					if (!typeof(TeaConfigBase).IsAssignableFrom(returnType))
					{
						throw new Exception("Tried to pass unallowed class as config");
					}

					return returnType;
				}

				public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
				{
					assemblyName = "#dll"; // The things we have to do because the Newtonsoft.Json version used is 7 years old :/
					typeName = serializedType.FullName;
				}
			}

			private void SendServerConfigToPlayer(IServerPlayer player)
			{			
				TeaConfigServerBroadcastPacket packet = new() {
					ConfigID = ConfigID,
					Data = JsonConvert.SerializeObject(ServerConfig, new JsonSerializerSettings() {
						TypeNameHandling = TypeNameHandling.All,
						Binder = new TeaConfigSerializationBinder()
					})
				};

				ServerNetworkChannel.SendPacket(packet, player);
			}

			public void OnClientServerChangePacket(IServerPlayer fromPlayer, TeaConfigClientServerChangePacket packet)
			{
				EnumTeaConfigChangeResponseState state;
				Dictionary<string, string> stateData = new();

				// TODO: Check for controlserver privilege
				// I'm so stupid lol. We can just ignore anything that comes from untrusted users, maybe put it in the logs
				if (!fromPlayer.HasPrivilege(Privilege.controlserver))
				{
					state = EnumTeaConfigChangeResponseState.NoPrivilege;
					sapi.Logger.Warning($"{Mod.Info.Name}: Player {fromPlayer.PlayerName} tried to change server settings despite not having the privileges to do so!");
				}
				else
				{
					bool reloadRequired = true;
					int successCount = 0;
					int errorCount = 0;

					foreach (KeyValuePair<string, string> settingPair in packet.Data)
					{
						string settingCode = settingPair.Key;
						string settingValue = settingPair.Value;

						TeaConfigSetResult result = SetSettingValue(sapi, settingCode, new CmdArgs(settingValue), out _);

						switch (result.type)
						{
							case EnumTeaConfigSetResultType.SUCCESS: successCount++; break;
							case EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER: sapi.Logger.Error($"{Mod.Info.Name}: attempted to set client setting '{settingCode}' on server"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT: sapi.Logger.Error($"{Mod.Info.Name}: attempted to set server setting '{settingCode}' on client"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE: sapi.Logger.Error($"{Mod.Info.Name}: attempted to change un-changeable setting '{settingCode}'"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE: sapi.Logger.Error($"{Mod.Info.Name}: wrong value when changing setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_EXCEPTION: sapi.Logger.Error($"{Mod.Info.Name}: error when setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_NO_VALUE: sapi.Logger.Error($"{Mod.Info.Name}: no value provided to change setting '{settingCode}'"); errorCount++; break;
							case EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING: sapi.Logger.Error($"{Mod.Info.Name}: attempted to change unknown setting '{settingCode}'"); errorCount++; break;
							default: sapi.Logger.Error($"{Mod.Info.Name}: unknown error when changing setting '{settingCode}'"); errorCount++; break;
						}
					}

					SaveConfig(sapi, ServerConfig);

					state = EnumTeaConfigChangeResponseState.RequestSuccess;

					stateData["successCount"] = successCount.ToString();
					stateData["errorCount"] = errorCount.ToString();
					stateData["reloadRequired"] = reloadRequired ? "1" : "0";
				}

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
						bool reloadRequired = true;

						foreach (KeyValuePair<string, IAttribute> settingPair in clientDataTree)
						{
							string settingCode = settingPair.Key;
							string settingValue = (settingPair.Value as StringAttribute).value;

							TeaConfigSetResult result = SetSettingValue(capi, settingCode, new CmdArgs(settingValue), out _);

							switch (result.type)
							{
								case EnumTeaConfigSetResultType.SUCCESS: successCount++; break;
								case EnumTeaConfigSetResultType.ERROR_CLIENT_SETTING_ON_SERVER: capi.Logger.Error($"{Mod.Info.Name}: attempted to set client setting '{settingCode}' on server"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_SERVER_SETTING_ON_CLIENT: capi.Logger.Error($"{Mod.Info.Name}: attempted to set server setting '{settingCode}' on client"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_SET_NOT_AVAILABLE: capi.Logger.Error($"{Mod.Info.Name}: attempted to change un-changeable setting '{settingCode}'"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_EXCEPTION_PARSE: capi.Logger.Error($"{Mod.Info.Name}: wrong value when changing setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_EXCEPTION: capi.Logger.Error($"{Mod.Info.Name}: error when setting '{settingCode}': {result.resultInfo}"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_NO_VALUE: capi.Logger.Error($"{Mod.Info.Name}: no value provided to change setting '{settingCode}'"); errorCount++; break;
								case EnumTeaConfigSetResultType.ERROR_UNKNOWN_SETTING: capi.Logger.Error($"{Mod.Info.Name}: attempted to change unknown setting '{settingCode}'"); errorCount++; break;
								default: capi.Logger.Error($"{Mod.Info.Name}: unknown error when changing setting '{settingCode}'"); errorCount++; break;
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
				
				TeaConfigBase incomingConfig;

				try
				{
					incomingConfig = JsonConvert.DeserializeObject<TeaConfigBase>(packet.Data.Replace("#dll", Assembly.GetExecutingAssembly().GetName().FullName), new JsonSerializerSettings() 
					{
						TypeNameHandling = TypeNameHandling.All,
						Binder = new TeaConfigSerializationBinder()
					});
				}
				catch (Exception)
				{
					capi.ShowChatMessage($"Failed to deserialize mod {packet.ConfigID} config sent from server!");
					return;
				}
			
				if (incomingConfig == null)
				{
					capi.ShowChatMessage($"Failed to synchronise {packet.ConfigID} config sent from server!");
					return;
				}

				// Copy new values into existing config instance, so that we don't have to re-register settings; not the most elegant, but it happens only on sync anyway
				// In the future, we can introduce delta sharing where only the changed variables will be sent, instead of everything
				// There won't be any more need to send the type either, saving bandwidth and closing a potential security hole
				foreach (PropertyInfo property in ServerConfig.GetType().GetProperties().Where(p => p.CanWrite))
				{
					property.SetValue(ServerConfig, property.GetValue(incomingConfig, null), null);
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