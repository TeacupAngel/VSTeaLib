#if DEBUG

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Client;

using TeaLib.TeaConfig;

namespace TeaLib
{
	namespace Debug
	{
		public class TeaGUIDebugSystem : ModSystem
		{
			private ICoreClientAPI _capi;
			private DebugContainerDialog _containerDialog;

			public override void StartClientSide(ICoreClientAPI api)
			{
				api.ChatCommands
				.GetOrCreate("teadbg")
				.BeginSubCommand("gui")
					.BeginSubCommand("containers")
						.HandleWith(OnDebugContainers)
					.EndSubCommand()
				.EndSubCommand()
				.BeginSubCommand("config")
					.HandleWith(OnDebugModConfig)
				.EndSubCommand();

				_capi = api;
			}

			private TextCommandResult OnDebugContainers(TextCommandCallingArgs args)
			{
				_containerDialog ??= new DebugContainerDialog(_capi);
				_containerDialog.ComposeDialog();
				_containerDialog.TryOpen();

				return TextCommandResult.Success("Test container dialog open");
			}

			private TextCommandResult OnDebugModConfig(TextCommandCallingArgs args)
			{
				TeaConfigModSettings[] modSettings = new TeaConfigModSettings[] 
				{
					new TeaConfigModSettings 
					{  
						ConfigID = "testmodone",
						ConfigName = "Test Mod 1 (both)",
						ServerSettings = Array.AsReadOnly(new TeaConfigSetting[] {
							new TeaConfigSettingString("serverstring1", "strings"),
							new TeaConfigSettingString("serverstring2", "strings"),
							new TeaConfigSettingString("serverstring3", "strings"),
							new TeaConfigSettingFloat("serverfloat1", "floats"),
							new TeaConfigSettingFloat("serverfloat2", "floats"),
							new TeaConfigSettingFloat("serverfloat3", "floats"),
							new TeaConfigSettingBool("serverbool1", "bools"),
							new TeaConfigSettingBool("serverbool2", "bools"),
							new TeaConfigSettingBool("serverbool3", "bools"),
						}),
						ClientSettings = Array.AsReadOnly(new TeaConfigSetting[] {
							new TeaConfigSettingString("clientstring1", "strings"),
							new TeaConfigSettingString("clientstring2", "strings"),
							new TeaConfigSettingString("clientstring3", "strings"),
							new TeaConfigSettingFloat("clientfloat1", "floats"),
							new TeaConfigSettingFloat("clientfloat2", "floats"),
							new TeaConfigSettingFloat("clientfloat3", "floats"),
							new TeaConfigSettingBool("clientbool1", "bools"),
							new TeaConfigSettingBool("clientbool2", "bools"),
							new TeaConfigSettingBool("clientbool3", "bools"),
						}),
					},
					new TeaConfigModSettings 
					{  
						ConfigID = "testmodtwo",
						ConfigName = "Test Mod 2 (server only)",
						ServerSettings = Array.AsReadOnly(new TeaConfigSetting[] {
							new TeaConfigSettingFloat("otherserverfloat1", "general"),
							new TeaConfigSettingBool("otherserverbool1", "general"),
							new TeaConfigSettingString("otherserverstring1", "other"),
							new TeaConfigSettingString("otherserverstring2", "other"),
							new TeaConfigSettingString("otherserverstring3", "other"),
						}),
					},
					new TeaConfigModSettings 
					{  
						ConfigID = "testmodthree",
						ConfigName = "Test Mod 3 (client only)",
						ClientSettings = Array.AsReadOnly(new TeaConfigSetting[] {
							new TeaConfigSettingFloat("otherclientfloat1", "general"),
							new TeaConfigSettingBool("otherclientbool1", "general"),
							new TeaConfigSettingString("otherclientstring1", "other"),
							new TeaConfigSettingString("otherclientstring2", "other"),
							new TeaConfigSettingString("otherclientstring3", "other"),
						}),
					}
				};

				TeaConfigDialog.GetLoaded(_capi).OpenWithData(modSettings);

				return TextCommandResult.Success("");
			}

			public override void Dispose()
			{
				_capi = null;

				_containerDialog = null;
			}
		}
	}
}

#endif