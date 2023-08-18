using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

using Cairo;

namespace TeaLib
{
	namespace TeaConfig
	{	
		public class TeaConfigSettingFloat : TeaConfigSetting
		{
			private readonly float _minValue;
			private readonly float _maxValue;

			public TeaConfigSettingFloat(string code, string category, float minValue = float.MinValue, float maxValue = float.MaxValue) : base(code, category)
			{
				_minValue = minValue;
				_maxValue = maxValue;
			}

			public override string GetStringFromValue(object value) => ((float)value).ToString(CultureInfo.InvariantCulture);
			public override string StringSet(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 decimal number parameter required");

				float? param = args.PopFloat();

				if (!param.HasValue) throw new TeaConfigArgumentException("Parameter is not a proper decimal number");
					
				Set(GameMath.Clamp(param.Value, _minValue, _maxValue));

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged)
			{
				string inputValue = value != null ? ((float)value).ToString(CultureInfo.InvariantCulture) : StringGet();

				GuiElementNumberInput input = new(capi, bounds, null, CairoFont.WhiteSmallText());
				input.SetValue(inputValue, false);
				input.SetPlaceHolderText(placeholder);
				// We need to set it *after* SetValue, so that the initial value doesn't immediately get saved as a config change
				input.OnTextChanged = (textInput) => { SendInput(textInput,  onChanged); };

				return input;
			}

			public void SendInput(string textInput, TeaConfigSettingOnChanged onChanged)
			{
				if (float.TryParse(textInput, CultureInfo.InvariantCulture, out float result)) onChanged(result);
			}
		}
	}
}