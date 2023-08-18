#if DEBUG

using System;
using System.Collections.ObjectModel;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Newtonsoft.Json;
using TeaLib.TeaConfig;

namespace TeaLib
{
	namespace Debug
	{
		public class TeaLibDebugConfigServer : TeaConfigBase
		{
			public override string ConfigName {get;} = "TeaLibDebugConfigServer";
			public override EnumTeaConfigApiSide ConfigType {get;} = EnumTeaConfigApiSide.Server;

			// Config data		
			public float DrifterScariness {get; set;} = 0.5f;
			public float RngUnfainess {get; set;} = 10f;
			public string TastiestFood {get; set;} = "Cheese";

			public override void CreateConfigSettings()
			{
				base.CreateConfigSettings();

				_configSettings = Array.AsReadOnly(new TeaConfigSetting[] 
				{
					new TeaConfigSettingFloat("DrifterScariness", "difficulty", 0, 1000),
					new TeaConfigSettingFloat("RngUnfainess", "difficulty", 0, 1000),
					new TeaConfigSettingString("TastiestFood", "opinions"),
				});
			}
		}
	}
}

#endif