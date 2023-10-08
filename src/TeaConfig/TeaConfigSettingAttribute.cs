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
			public string Category;
			public TeaConfigSettingFlags Flags = TeaConfigSettingFlags.None;

			public abstract TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType);
		}
	}
}