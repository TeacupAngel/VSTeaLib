using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingString : TeaConfigSetting
		{
			public TeaConfigSettingString(string code, string category, bool allowEmpty = false) : base(code, category) {}
			
			public override string GetStringFromValue(object value) => value.ToString();
			public override string StringSet(CmdArgs args) => throw new NotImplementedException();

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged)
			{
				string inputValue = value != null ? (string)value : StringGet();

				GuiElementTextInput input = new(capi, bounds, null, CairoFont.WhiteSmallText());
				input.SetValue(inputValue, false);
				input.SetPlaceHolderText(placeholder);
				// We need to set it *after* SetValue, so that the initial value doesn't immediately get saved as a config change
				input.OnTextChanged = (text) => { onChanged(text); };

				return input;
			}
		}
	}
}