#if DEBUG

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TeaLib
{
	namespace Debug
	{
		public class TeaDebugSystem : ModSystem
		{
			public override void Start(ICoreAPI api)
			{
				api.ChatCommands
				.GetOrCreate("teadbg")
				.BeginSubCommand("msg")
					.HandleWith(OnDebugMessage)
				.EndSubCommand()
				.RequiresPrivilege(Privilege.controlserver);
			}

			private TextCommandResult OnDebugMessage(TextCommandCallingArgs args)
			{
				return TextCommandResult.Success("TeaLib: Debug Message");
			}
		}
	}
}

#endif