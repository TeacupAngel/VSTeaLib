using System;
using Vintagestory.API.Common;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingCustom : TeaConfigSetting
		{
			public delegate string TeaConfigSettingGetDeletage(); 
			public delegate string TeaConfigSettingSetDeletage(CmdArgs args); 

			public TeaConfigSettingCustom(string code, string category, TeaConfigSettingGetDeletage get, TeaConfigSettingSetDeletage set, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags)
			{
				_get = get;
				_set = set;
			}

			private readonly TeaConfigSettingGetDeletage _get;
			private readonly TeaConfigSettingSetDeletage _set;

			public override string GetAsString() => _get();
			public override string SetAsString(CmdArgs args) => _set(args);
		}
	}
}