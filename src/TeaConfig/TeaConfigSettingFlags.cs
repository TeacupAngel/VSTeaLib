using System;

namespace TeaLib
{
	namespace TeaConfig
	{
		[Flags]
		public enum TeaConfigSettingFlags 
		{
			None = 0,
			RestartNotNeeded = 1
		}
	}
}