using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public abstract class TeaConfigSetting
		{
			public delegate void TeaConfigSettingOnChanged(object input);

			public string Code {get; protected set;}
			public string Category {get; protected set;}
			public TeaConfigSettingFlags Flags {get; protected set;}

			public TeaConfigBase Config {get; set;}

			public TeaConfigSetting(string code, string category, TeaConfigSettingFlags flags)
			{
				Code = code;
				Category = category;
				Flags = flags;
			}

			public object Get() => Config.GetType().GetProperty(Code).GetValue(Config);
			public void Set(object value)
			{
				PropertyInfo propertyInfo = Config.GetType().GetProperty(Code);

				propertyInfo.SetValue(Config, value);
			}

			public abstract string GetStringFromValue(object value);

			public virtual string StringGet() => GetStringFromValue(Get()).ToString();
			public abstract string StringSet(CmdArgs args);

			public virtual GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged, string settingLangKey) { return null; }
		}
	}
}