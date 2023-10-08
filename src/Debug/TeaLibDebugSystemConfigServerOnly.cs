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
		public class TeaLibDebugSystemConfigServerOnly : TeaConfigSystemBase
		{
			public override string ConfigID => "tealibdbgserver";
			public override string ConfigName => $"{base.ConfigName} (Server-only)";

			public override void LoadConfigs(ICoreAPI api)
			{
				ServerConfig = LoadConfig<TeaLibDebugConfigServer>(api);
			}

			public TeaLibDebugConfigServer GetServerConfig()
			{
				return (TeaLibDebugConfigServer)ServerConfig;
			}
		}
	}
}

#endif