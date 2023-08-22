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
			[TeaConfigSettingEnum(Category = "test")]
			public TeaLibDebugConfigEnum EnumSetting {get; set;} = TeaLibDebugConfigEnum.OptionB;

			[TeaConfigSettingBool(Category = "test")]
			public bool BoolSetting {get; set;} = true;
		}
	}
}

#endif