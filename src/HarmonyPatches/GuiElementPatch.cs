using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vintagestory.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;
using Vintagestory.GameContent;
using Vintagestory.Essentials;

using HarmonyLib;

using Cairo;

using TeaLib.GuiExtensions;

namespace TeaLib
{
	namespace HarmonyPatches
	{
		[VSHarmonyPatch(EnumPatchType.Client)]
		public class GuiElementPatch : VSHarmonyPatchBase
		{
			public override void Execute(Harmony harmony, ICoreAPI api)
			{
				harmony.Patch(AccessTools.Method(typeof(GuiElement),"OnMouseUpOnElement"),
					prefix: GetPatchMethod("OnMouseUpOnElementPrefix") 
				);
			}

			// Fixes two crashes:
			// 1. When multiple GuiElementContainers exist on a page (or if anything else handles a mouse event?) this stops containers from thinking every child element
			// 		had mouse button pressed on it
			// 2. When an empty GuiElementContainers tries to handle a mouse click
			static void OnMouseUpOnElementPrefix(GuiElement __instance, ref ICoreClientAPI api, ref MouseEvent args)
			{
				args.Handled = true;
			}
		}
	}
}