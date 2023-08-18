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
		public enum TeaLibDebugConfigEnum
		{
			OptionA,
			OptionB,
			OptionC
		}

		public class TeaLibDebugConfigClient : TeaConfigBase
		{
			public override string ConfigName {get;} = "TeaLibDebugConfigClient";
			public override EnumTeaConfigApiSide ConfigType {get;} = EnumTeaConfigApiSide.Client;

			// Config data
			public TeaLibDebugConfigEnum EnumSetting {get; set;} = TeaLibDebugConfigEnum.OptionB;
			public bool BoolSetting {get; set;} = true;

			public override void CreateConfigSettings()
			{
				base.CreateConfigSettings();
				
				_configSettings = Array.AsReadOnly(new TeaConfigSetting[] 
				{
					new TeaConfigSettingEnum<TeaLibDebugConfigEnum>("EnumSetting", "test"),
					new TeaConfigSettingBool("BoolSetting", "test"),
				});
			}
		}
	}
}

#endif