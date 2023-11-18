using System;
using System.Reflection;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using TeaLib.GuiExtensions;

namespace TeaLib
{
	namespace TeaConfig
	{
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingEnumAttribute : TeaConfigSettingAttribute 
		{
			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType, ConfigSettingNotifyDelegate notifyDelegate)
			{
				if (!propertyType.IsAssignableTo(typeof(Enum)))
				{
					notifyDelegate($"{propertyCode} is type {propertyType.Name}, but its attribute type is Enum. Setting skipped", true);
					return null;
				}

				Type genericEnumType = typeof(TeaConfigSettingEnum<>).MakeGenericType(new Type[] {propertyType});
				TeaConfigSetting setting = Activator.CreateInstance(genericEnumType, propertyCode, Category, Flags) as TeaConfigSetting;

				return setting;
			}
		}

		public class TeaConfigSettingEnum<T> : TeaConfigSetting where T : struct, Enum
		{
			public TeaConfigSettingEnum(string code, string category, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags) {}
			
			public override string SetAsString(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 word parameter required");

				string param = args.PopWord();

				if (string.IsNullOrEmpty(param)) throw new TeaConfigArgumentException("Parameter cannot be empty");

				if (!Enum.TryParse<T>(param, true, out T parsedEnum)) throw new TeaConfigArgumentException($"Parameter is invalid. Choose one from: {string.Join(", ", Enum.GetNames(typeof(T)))}");

				Set(parsedEnum);

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged, string settingLangKey)
			{
				string[] values = Enum.GetNames<T>();
				string[] names = Enum.GetNames<T>().Select(value => Lang.Get($"{settingLangKey}-option-{value.ToLowerInvariant()}")).ToArray();
				string selectedValue = value != null ? (string)value : GetAsString();
				int selectedIndex = Array.IndexOf(values, selectedValue);

				GuiElementDropDownFixed input = new(capi, values, names, selectedIndex, (value, selected) => { onChanged(value); }, bounds, CairoFont.WhiteSmallText(), false);

				return input;
			}
		}
	}
}