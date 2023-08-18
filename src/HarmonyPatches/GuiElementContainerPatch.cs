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
		public class GuiElementContainerPatch : VSHarmonyPatchBase
		{
			public override void Execute(Harmony harmony, ICoreAPI api)
			{
				harmony.Patch(typeof(GuiElementContainer).GetMethod("OnMouseUp"),
					prefix: GetPatchMethod("OnMouseUpPrefix") 
				);

				harmony.Patch(typeof(GuiElementContainer).GetMethod("OnMouseDown"),
					prefix: GetPatchMethod("OnMouseDownPrefix") 
				);

				harmony.Patch(typeof(GuiElementContainer).GetMethod("OnKeyDown"),
					prefix: GetPatchMethod("OnKeyDownPrefix") 
				);

				harmony.Patch(typeof(GuiElementContainer).GetMethod("OnKeyPress"),
					prefix: GetPatchMethod("OnKeyPressPrefix") 
				);
			}

			// Fixes two crashes:
			// 1. When multiple GuiElementContainers exist on a page (or if anything else handles a mouse event?) this stops containers from thinking every child element
			// 		had mouse button pressed on it
			// 2. When an empty GuiElementContainers tries to handle a mouse click
			static bool OnMouseUpPrefix(GuiElementContainer __instance, ICoreClientAPI api, MouseEvent args)
			{
				return !args.Handled && __instance.Elements.Count > 0;
			}

			static bool OnMouseDownPrefix(GuiElementContainer __instance, ICoreClientAPI api, MouseEvent args)
			{
				return !args.Handled && __instance.Elements.Count > 0;
			}

			static bool OnKeyDownPrefix(GuiElementContainer __instance, ICoreClientAPI api, KeyEvent args)
			{
				return !args.Handled && __instance.Elements.Count > 0;
			}

			static bool OnKeyPressPrefix(GuiElementContainer __instance, ICoreClientAPI api, KeyEvent args)
			{
				return !args.Handled && __instance.Elements.Count > 0;
			}
		}
	}
}