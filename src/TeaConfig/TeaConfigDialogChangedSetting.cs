using System;
using System.Diagnostics.CodeAnalysis;

namespace TeaLib
{
	namespace TeaConfig
	{
		public struct TeaConfigDialogChangedSetting
		{
			public string ConfigID {get; private set;}
			public EnumTeaConfigDialogSettingSide SettingSide {get; private set;}

			public TeaConfigSetting Setting {get; private set;}

			public TeaConfigDialogChangedSetting(string configID, EnumTeaConfigDialogSettingSide settingSide, TeaConfigSetting setting)
			{
				ConfigID = configID;
				SettingSide = settingSide;
				Setting = setting;
			}

			public override readonly bool Equals([NotNullWhen(true)] object obj)
			{
				return obj is TeaConfigDialogChangedSetting otherSetting
					&& ConfigID == otherSetting.ConfigID 
					&& SettingSide == otherSetting.SettingSide 
					&& Setting.Code == otherSetting.Setting.Code;
			}

			public override readonly int GetHashCode()
			{
				return HashCode.Combine(ConfigID.GetHashCode(), SettingSide.GetHashCode(), Setting.Code);
			}
		}
	}
}