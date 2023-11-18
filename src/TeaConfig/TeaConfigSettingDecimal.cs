using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TeaLib
{
	namespace TeaConfig
	{
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class TeaConfigSettingDecimalAttribute<T> : TeaConfigSettingAttribute where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
		{
			public T Min {get; set;} = T.MinValue;
			public T Max {get; set;} = T.MaxValue;

			public override TeaConfigSetting GetTeaConfigSetting(string propertyCode, Type propertyType, ConfigSettingNotifyDelegate notifyDelegate)
			{
				if (propertyType != typeof(T))
				{
					if (propertyType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IFloatingPointIeee754<>)))
					{
						notifyDelegate($"{propertyCode} is type {propertyType.Name}, but its attribute is decimal type {typeof(T).Name}. Setting may not behave as expected.", false);
					}
					else
					{
						notifyDelegate($"{propertyCode} is type {propertyType.Name}, but its attribute type is {typeof(T).Name}. Setting skipped", true);
						return null;
					}
				}

				Type genericEnumType = typeof(TeaConfigSettingDecimal<>).MakeGenericType(new Type[] {propertyType});
				TeaConfigSetting setting = Activator.CreateInstance(genericEnumType, propertyCode, Category, Min, Max, Flags) as TeaConfigSetting;

				return setting;
			}
		}

		public class TeaConfigSettingDecimal<T> : TeaConfigSetting where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
		{
			private readonly T _minValue;
			private readonly T _maxValue;

			public TeaConfigSettingDecimal(string code, string category, T minValue, T maxValue, TeaConfigSettingFlags flags = TeaConfigSettingFlags.None) : base(code, category, flags)
			{
				_minValue = minValue;
				_maxValue = maxValue;
			}

			public override string ConvertValueToString(object value) => ((T)value).ToString(null, CultureInfo.InvariantCulture);
			public override string SetAsString(CmdArgs args)
			{
				if (!(args.Length > 0)) throw new TeaConfigArgumentException("1 decimal number parameter required");

				if (!T.TryParse(args.PopWord(), CultureInfo.InvariantCulture, out T result)) throw new TeaConfigArgumentException("Parameter is not a proper decimal number");
					
				Set(T.Clamp(result, _minValue, _maxValue));

				return Get().ToString();
			}

			public override GuiElement GetInputElement(ICoreClientAPI capi, ElementBounds bounds, object value, string placeholder, TeaConfigSettingOnChanged onChanged, string settingLangKey)
			{
				string inputValue = value != null ? ConvertValueToString(value) : GetAsString();

				GuiElementNumberInput input = new(capi, bounds, null, CairoFont.WhiteSmallText());
				input.SetValue(inputValue, false);
				input.SetPlaceHolderText(placeholder);
				// We need to create this callback *after* calling SetValue, so that the initial value doesn't immediately get interpreted as a config change
				input.OnTextChanged = (textInput) => { SendInput(textInput,  onChanged); };

				return input;
			}

			private void SendInput(string textInput, TeaConfigSettingOnChanged onChanged)
			{
				if (!T.TryParse(textInput, CultureInfo.InvariantCulture, out T result)) result = (T)Get();

				onChanged(result);
			}
		}
	}
}