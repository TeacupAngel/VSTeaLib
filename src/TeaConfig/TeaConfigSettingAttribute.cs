using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TeaLib
{
	namespace TeaConfig
	{
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public abstract class TeaConfigSettingAttribute : Attribute 
		{
			protected string _category;

			public TeaConfigSettingAttribute(string category)
			{
				_category = category;
			}

			public abstract TeaConfigSetting GetTeaConfigSetting(string code);
		}
	}
}