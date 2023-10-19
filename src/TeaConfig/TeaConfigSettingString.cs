using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace TeaConfig
	{	
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingStringAttribute : TeaConfigSettingAttribute 
		{
			public bool AllowEmpty = false;

			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType)
			{
				return new TeaConfigSettingString(propertyCode, Category, AllowEmpty, Flags);
			}
		}

		public class TeaConfigSettingString : TeaConfigSetting
		{
			private readonly bool _allowEmpty;

			public TeaConfigSettingString(string code, string category, bool allowEmpty = false, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags) 
			{
				_allowEmpty = allowEmpty;
			}

			public override string SetAsString(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 string parameter required");

				string param = args.PopWord();

				if (!_allowEmpty && String.IsNullOrEmpty(param)) throw new TeaConfigArgumentException("Parameter cannot be empty");
					
				Set(param);

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged, string settingLangKey)
			{
				string stringValue = (string)value;
				string inputValue = _allowEmpty || !String.IsNullOrEmpty(stringValue) ? stringValue : GetAsString();

				GuiElementTextInput input = new(capi, bounds, null, CairoFont.WhiteSmallText());
				input.SetValue(inputValue, false);
				input.SetPlaceHolderText(placeholder);
				// We need to set it *after* SetValue, so that the initial value doesn't immediately get saved as a config change
				input.OnTextChanged = (text) => { SendInput(text, onChanged); };

				return input;
			}

			private void SendInput(string textInput, TeaConfigSettingOnChanged onChanged)
			{
				string result = _allowEmpty || !String.IsNullOrEmpty(textInput) ? textInput : GetAsString();

				onChanged(result);
			}
		}
	}
}