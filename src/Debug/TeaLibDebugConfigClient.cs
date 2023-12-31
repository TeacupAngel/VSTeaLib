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
			public override EnumTeaConfigApiSide ConfigType {get;} = EnumTeaConfigApiSide.Client;

			// Config data
			[TeaConfigSettingEnum(Category = "general")]
			public TeaLibDebugConfigEnum EnumSetting {get; set;} = TeaLibDebugConfigEnum.OptionB;

			[TeaConfigSettingBool(Category = "general")]
			public bool BoolSetting {get; set;} = true;

			[TeaConfigSettingDecimal<float>(Category = "general", Min = 0, Max = 100, Flags = TeaConfigSettingFlags.RestartNotNeeded)]
			public float ReloadNotRequired {get; set;} = 0.5f;
		}
	}
}

#endif