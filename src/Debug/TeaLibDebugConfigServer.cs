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
			public enum EnumDifficultyLevels
			{
				Hard,
				Harder,
				Insane,
				NotFun,
				WhatTheF
			}

			public override EnumTeaConfigApiSide ConfigType {get;} = EnumTeaConfigApiSide.Server;

			// Config data
			[TeaConfigSettingDecimal<float>(Category = "difficulty", Min = 0, Max = 100)]
			public float DrifterScariness {get; set;} = 0.5f;
			
			[TeaConfigSettingDecimal<float>(Category = "difficulty", Min = 0, Max = 1000)]
			public float RngUnfainess {get; set;} = 10f;

			[TeaConfigSettingEnum(Category = "difficulty")]
			public EnumDifficultyLevels DifficultyLevel {get; set;} = EnumDifficultyLevels.Hard;

			[TeaConfigSettingString(Category = "opinions", AllowEmpty = false)]
			public string TastiestFood {get; set;} = "Cheese";

			[TeaConfigSettingDecimal<float>(Category = "decimals", Min = -10f, Max = 10f)]
			public float Float {get; set;} = 10f;

			[TeaConfigSettingDecimal<double>(Category = "decimals")]
			public double Double {get; set;} = double.MaxValue;

			[TeaConfigSettingDecimal<Half>(Category = "decimals")]
			public Half Half {get; set;} = (Half)5;

			[TeaConfigSettingDecimal<float>(Category = "wrongs")]
			public string FloatExplode {get; set;} = "";

			[TeaConfigSettingEnum(Category = "wrongs")]
			public string EnumExplode {get; set;} = "";

			[TeaConfigSettingBool(Category = "wrongs")]
			public string BoolExplode {get; set;} = "";
		}
	}
}

#endif