using System;
using Vintagestory.API.Common;

using Cairo;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingHashset : TeaConfigSetting
		{
			public TeaConfigSettingHashset(string code, string category) : base(code, category) {}

			public override string GetStringFromValue(object value) => throw new NotImplementedException();
			public override string StringSet(CmdArgs args) => throw new NotImplementedException();
		}
	}
}