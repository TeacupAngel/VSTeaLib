#if DEBUG

using System;
using System.Collections.Generic;
using Vintagestory.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using ProtoBuf;
using System.Reflection;

using TeaLib.TeaConfig;

using Cairo;

namespace TeaLib
{
	namespace Debug
	{
		public class TeaLibDebugSystemConfig : TeaConfigSystemBase
		{
			public override bool ShouldLoad(EnumAppSide forSide) => true;
			public override string ConfigID => "tealibdbg";

			public override void LoadConfigs(ICoreAPI api)
			{
				ServerConfig = LoadConfig<TeaLibDebugConfigServer>(api);
				ClientConfig = LoadConfig<TeaLibDebugConfigClient>(api);
			}

			public TeaLibDebugConfigServer GetSyncedConfig()
			{
				return (TeaLibDebugConfigServer)ServerConfig;
			}

			public TeaLibDebugConfigClient GetClientConfig()
			{
				return (TeaLibDebugConfigClient)ClientConfig;
			}
		}
	}
}

#endif