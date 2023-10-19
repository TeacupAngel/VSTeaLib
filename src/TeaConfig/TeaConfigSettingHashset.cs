using System;
using Vintagestory.API.Common;

using Cairo;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingHashset : TeaConfigSetting
		{
			public TeaConfigSettingHashset(string code, string category, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags) {}

			public override string SetAsString(CmdArgs args) => throw new NotImplementedException();
		}
	}
}