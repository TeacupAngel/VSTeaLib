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
			public override EnumTeaConfigApiSide ConfigType {get;} = EnumTeaConfigApiSide.Server;

			// Config data
			[TeaConfigSettingFloat(Category = "difficulty", Min = 0, Max = 100)]
			public float DrifterScariness {get; set;} = 0.5f;
			[TeaConfigSettingFloat(Category = "difficulty", Min = 0, Max = 1000)]
			public float RngUnfainess {get; set;} = 10f;
			[TeaConfigSettingString(Category = "opinions", AllowEmpty = false)]
			public string TastiestFood {get; set;} = "Cheese";
		}
	}
}

#endif