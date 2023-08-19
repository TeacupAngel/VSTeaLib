using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Vintagestory.API.Common;
using ProperVersion;
using Newtonsoft.Json;

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

			public string Version;

			[JsonIgnore]
			public SemVer SemVerVersion {get; private set;}

			public bool SetVersion(ICoreAPI api, string version, ModInfo modInfo)
			{
				if (!SemVer.TryParse(version, out SemVer configVersion, out string configError))
				{
					api.Logger.Error($"{modInfo.Name}: Error trying to parse mod config version. Best guess: {configVersion} (error: {configError})");
				}

				bool wasUpdated = configVersion > SemVerVersion;
				SemVerVersion = configVersion;
				Version = version;
				return wasUpdated;
			}

			public void InitialiseVersion(ICoreAPI api, ModInfo modInfo)
			{
				SetVersion(api, Version, modInfo);
			}

			public bool ApplyMigrations(ICoreAPI api, ModInfo modInfo)
			{
				if (Version == null)
				{
					api.Logger.Error($"Config version was null! Cannot migrate because data was lost, setting version to latest");
					SetVersion(api, modInfo.Version, modInfo);
					return true;
				}

				if (!SemVer.TryParse(Version, out SemVer configVersion, out string configError))
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
			public virtual void CreateConfigSettings() {} // TODO: Before release, this should be changed to automatically create config settings based on [Attributes]

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