using System;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TeaLib
{
	namespace TeaConfig
	{
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingBoolAttribute : TeaConfigSettingAttribute 
		{
			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType, ConfigSettingNotifyDelegate notifyDelegate)
			{
				if (propertyType != typeof(bool))
				{
					notifyDelegate($"{propertyCode} is type {propertyType.Name}, but its attribute type is Bool. Setting skipped", true);
					return null;
				}

				return new TeaConfigSettingBool(propertyCode, Category, Flags);
			}
		}

		public class TeaConfigSettingBool : TeaConfigSetting
		{
			private static readonly string[] trueAliases = ["on", "yes", "true", "1"];
			private static readonly string[] falseAliases = ["off", "no", "false", "0"];

			public TeaConfigSettingBool(string code, string category, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags) {}

			public override string SetAsString(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 boolean parameter required (Choose 'on' or 'off', or 'yes/no', 'true/false', '1/0')");

				string param = args.PopWord();

				if (string.IsNullOrEmpty(param)) throw new TeaConfigArgumentException("Parameter cannot be empty");

				param = param.ToLower();

				bool boolValue;

				if (trueAliases.Contains(param)) boolValue = true;
				else if (falseAliases.Contains(param)) boolValue = false;
				else throw new TeaConfigArgumentException("Parameter is invalid. Choose 'on' or 'off' (or 'yes/no', 'true/false', '1/0')");

				Set(boolValue);

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged, string settingLangKey)
			{
				bool inputValue = value != null ? (bool)value : (bool)Get();

				GuiElementSwitch input = new(capi, (state) => { onChanged(state); }, bounds);
				input.SetValue(inputValue); // GuiElementSwitch.SetValue doesn't call the eventhandler (unlike other input elements) so this is fine

				return input;
			}
		}
	}
}