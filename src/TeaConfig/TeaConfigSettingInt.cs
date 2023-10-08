using System;
using Vintagestory.API.Common;

using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingInt : TeaConfigSetting
		{
			private readonly int _minValue;
			private readonly int _maxValue;

			public TeaConfigSettingInt(string code, string category, int minValue = int.MinValue, int maxValue = int.MaxValue, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags)
			{
				_minValue = minValue;
				_maxValue = maxValue;
			}
			public override string GetStringFromValue(object value) => throw new NotImplementedException();
			public override string StringSet(CmdArgs args) => throw new NotImplementedException();
		}
	}
}