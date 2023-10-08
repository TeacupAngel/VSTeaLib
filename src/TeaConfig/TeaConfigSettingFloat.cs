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
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingFloatAttribute : TeaConfigSettingAttribute 
		{
			public float Min {get; set;} = float.MinValue;
			public float Max {get; set;} = float.MaxValue;

			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType)
			{
				return new TeaConfigSettingFloat(propertyCode, Category, Min, Max, Flags);
			}
		}

		public class TeaConfigSettingFloat : TeaConfigSetting
		{
			private readonly float _minValue;
			private readonly float _maxValue;

			public TeaConfigSettingFloat(string code, string category, float minValue = float.MinValue, float maxValue = float.MaxValue, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags)
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

			private void SendInput(string textInput, TeaConfigSettingOnChanged onChanged)
			{
				float result;

				if (!float.TryParse(textInput, CultureInfo.InvariantCulture, out result)) result = (float)Get();

				onChanged(result);
			}
		}
	}
}