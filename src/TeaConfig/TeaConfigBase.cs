using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Vintagestory.API.Common;
using ProperVersion;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TeaLib
{
	namespace TeaConfig
	{
		public struct TeaConfigMigration
		{
			public SemVer Version {get; private set;}
			public Action<TeaConfigBase, ICoreAPI> Action {get; private set;}
		}

		[JsonObject(MemberSerialization.OptOut)]
		public abstract class TeaConfigBase
		{
			[JsonIgnore]
			public abstract EnumTeaConfigApiSide ConfigType {get;}

			public string ConfigVersion;

			[JsonIgnore]
			public SemVer SemVerVersion {get; private set;}

			public bool SetVersion(ICoreAPI api, string version, ModInfo modInfo)
			{
				if (!SemVer.TryParse(version, out SemVer newSemVerVersion, out string configError))
				{
					api.Logger.Error($"{modInfo.Name}: Error trying to parse mod config version. Best guess: {newSemVerVersion} (error: {configError})");
				}

				bool wasUpdated = newSemVerVersion > SemVerVersion;
				SemVerVersion = newSemVerVersion;
				ConfigVersion = version;
				return wasUpdated;
			}

			public void InitialiseVersion(ICoreAPI api, ModInfo modInfo)
			{
				SetVersion(api, ConfigVersion, modInfo);
			}

			public bool ApplyMigrations(ICoreAPI api, ModInfo modInfo)
			{
				if (ConfigVersion == null)
				{
					api.Logger.Error($"Config version was null! Cannot migrate because data was lost, setting version to latest");
					SetVersion(api, modInfo.Version, modInfo);
					return true;
				}

				if (!SemVer.TryParse(ConfigVersion, out SemVer configVersion, out string configError))
				{
					api.Logger.Error($"Error trying to parse config version. Best guess: {configVersion} (error: {configError})");
				}

				bool needsSave = false;

				TeaConfigMigration[] migrationList = GetMigrations();

				if (migrationList != null)
				{
					for (int i = migrationList.Length - 1; i >=0; i--)
					{
						TeaConfigMigration migration = migrationList[i];

						if (migration.Version > configVersion)
						{
							migration.Action(this, api);
							needsSave = true;

							continue;
						}
							
						break;
					}
				}

				needsSave |= SetVersion(api, modInfo.Version, modInfo);

				return needsSave;
			}

			public void Save(ICoreAPI api, string configName)
			{
				string filename = $"{configName}_{ConfigType.ToString().ToLower()}";

				api.StoreModConfig(this, $"{filename}.json");
			}

			public T Load<T>(ICoreAPI api, string configName) where T : TeaConfigBase
			{
				string filename = $"{configName}_{ConfigType.ToString().ToLower()}";

				return api.LoadModConfig<T>($"{filename}.json");
			}

			public virtual TeaConfigMigration[] GetMigrations()
			{
				return null;
			}

			[JsonIgnore]
			protected ReadOnlyCollection<TeaConfigSetting> _configSettings;
			[JsonIgnore]
			public ReadOnlyCollection<TeaConfigSetting> ConfigSettings { get => _configSettings; }

			public void CreateConfigSettings(ConfigSettingNotifyDelegate notifyDelegate) 
			{
				List<TeaConfigSetting> tempConfigSettingList = new();

				foreach(PropertyInfo propertyInfo in GetType().GetProperties())
				{
					TeaConfigSettingAttribute settingAttribute = propertyInfo.GetCustomAttribute<TeaConfigSettingAttribute>();
					if (settingAttribute == null) continue;

					TeaConfigSetting setting = settingAttribute.GetTeaConfigSetting(propertyInfo.Name, propertyInfo.PropertyType, notifyDelegate);
					if (setting != null) tempConfigSettingList.Add(setting);
				}

				_configSettings = tempConfigSettingList.AsReadOnly();
			}

			public void InitialiseSettings()
			{
				foreach (TeaConfigSetting configSetting in _configSettings)
				{
					configSetting.Config = this;
				}
			}
		}
	}
}