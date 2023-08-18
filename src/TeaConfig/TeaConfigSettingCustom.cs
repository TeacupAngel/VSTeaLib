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

			public TeaConfigSettingCustom(string code, string category, TeaConfigSettingGetDeletage get, TeaConfigSettingSetDeletage set) : base(code, category)
			{
				_get = get;
				_set = set;
			}

			private readonly TeaConfigSettingGetDeletage _get;
			private readonly TeaConfigSettingSetDeletage _set;

			public override string GetStringFromValue(object value) => value.ToString();
			public override string StringGet() => _get();
			public override string StringSet(CmdArgs args) => _set(args);
		}
	}
}