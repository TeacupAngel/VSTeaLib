using System.Collections.ObjectModel;

namespace TeaLib
{
	namespace TeaConfig
	{
		public class TeaConfigModSettings
		{
			public string ConfigID { get; set; }
			public string ConfigName { get; set; }
			public ReadOnlyCollection<TeaConfigSetting> ServerSettings { get; set; }
			public ReadOnlyCollection<TeaConfigSetting> ClientSettings { get; set; }
		}
	}
}