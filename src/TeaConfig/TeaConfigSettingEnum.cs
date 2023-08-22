using System;
using System.Reflection;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

using TeaLib.GuiExtensions;

namespace TeaLib
{
	namespace TeaConfig
	{
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingEnumAttribute : TeaConfigSettingAttribute 
		{
			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType)
			{
				Type genericEnumType = typeof(TeaConfigSettingEnum<>).MakeGenericType(new Type[] {propertyType});
				TeaConfigSetting enumSetting = Activator.CreateInstance(genericEnumType, propertyCode, Category) as TeaConfigSetting;

				return enumSetting;
			}
		}

		public class TeaConfigSettingEnum<T> : TeaConfigSetting where T : struct, Enum
		{
			public TeaConfigSettingEnum(string code, string category) : base(code, category) {}
			
			public override string GetStringFromValue(object value) => value.ToString();
			public override string StringSet(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 word parameter required");

				string param = args.PopWord();

				if (string.IsNullOrEmpty(param)) throw new TeaConfigArgumentException("Parameter cannot be empty");

				if (!Enum.TryParse<T>(param, true, out T parsedEnum)) throw new TeaConfigArgumentException($"Parameter is invalid. Choose one from: {string.Join(", ", Enum.GetNames(typeof(T)))}");

				Set(parsedEnum);

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged)
			{
				string[] values = Enum.GetNames<T>();
				string[] names = Enum.GetNames<T>(); // Eventually this should get replaced with `values` run though Lang.Get()
				string selectedValue = value != null ? (string)value : StringGet();
				int selectedIndex = Array.IndexOf(values, selectedValue);

				GuiElementDropDownFixed input = new(capi, values, names, selectedIndex, (value, selected) => { onChanged(value); }, bounds, CairoFont.WhiteSmallText(), false);

				return input;
			}
		}
	}
}