using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Client;

using HarmonyLib;

namespace TeaLib
{
	namespace HarmonyPatches
	{
		[VSHarmonyPatch(EnumPatchType.Client)]
		public class ElementBoundsPatch : VSHarmonyPatchBase
		{
			public override void Execute(Harmony harmony, ICoreAPI api)
			{
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"CopyOnlySize"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"CopyOffsetedSibling"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"BelowCopy"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"RightCopy"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"FlatCopy"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);
				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"ForkChildOffseted"),
					transpiler: GetPatchMethod("PercentWidthTranspiler") 
				);

				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"CalcWorldBounds"),
					transpiler: GetPatchMethod("CalcWorldBoundsTranspiler") 
				);

				harmony.Patch(AccessTools.Method(typeof(ElementBounds),"buildBoundsFromChildren"),
					prefix: GetPatchMethod("buildBoundsFromChildrenPrefix") 
				);
			}

			// In methods where a new copy of the bounds is being made, the new copy's `percentWidth` is wrongly being assigned the original's `percentHeight` instead
			// This same transpiler can fix all 6 of these methods
			public static IEnumerable<CodeInstruction> PercentWidthTranspiler(IEnumerable<CodeInstruction> instructions)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions);

				// Find where `percentWidth` is stored, delete the previous instruction, and insert the proper load of `percentWidth`
				codeMatcher
				.MatchStartForward(new CodeMatch(CodeInstruction.StoreField(typeof(ElementBounds), "percentWidth")))
				.Advance(-1)
				.RemoveInstruction()
				.Insert(CodeInstruction.LoadField(typeof(ElementBounds), "percentWidth"));

				return codeMatcher.InstructionEnumeration();
			}

			// In CalcWorldBounds, during the calculation of vertical sizing, there's a line that mistakenly compares horizontal sizing: 
			// `if (horizontalSizing == ElementSizing.PercentualSubstractFixed)`
			public static IEnumerable<CodeInstruction> CalcWorldBoundsTranspiler(IEnumerable<CodeInstruction> instructions)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions);

				// Step 1:
				// Find where `if (horizontalSizing == ElementSizing.PercentualSubstractFixed)`, 
				// making sure it's right after the `absPaddingY` field is stored so that we edit the right place
				//
				// Then we remove `horizontalSizing` we ended on, and replace it with the proper `verticalSizing` instead
				codeMatcher
				.MatchEndForward(
					new CodeMatch(CodeInstruction.StoreField(typeof(ElementBounds), "absPaddingY")),
					new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
					new CodeMatch(CodeInstruction.LoadField(typeof(ElementBounds), "horizontalSizing"))
				)
				.RemoveInstruction()
				.Insert(CodeInstruction.LoadField(typeof(ElementBounds), "verticalSizing"));

				// Step 2:
				

				return codeMatcher.InstructionEnumeration();
			}

			// This one has been bugging me for a while. FitToChildren sizing can't be mixed with Percentual sizing, even if one is horizontal
			// and the other is vertical, and thus they shouldn't affect each other at all!
			// This patch is a prefix rather than a transpiler, didn't seem worth it to transpile in this case
			public static bool buildBoundsFromChildrenPrefix(ElementBounds __instance)
			{
				if (__instance.ChildBounds == null || __instance.ChildBounds.Count == 0)
				{
					throw new Exception("Cant build bounds from children elements, there are no children!");
				}

				double width = 0;
				double height = 0;

				foreach (ElementBounds bounds in __instance.ChildBounds)
				{
					if (bounds == __instance)
					{
						throw new Exception("Endless loop detected. Bounds instance is contained itself in its ChildBounds List. Fix your code please :P");
					}

					// Alignment can only happen once the max size is known, so ignore it for now
					EnumDialogArea prevAlign = bounds.Alignment;
					bounds.Alignment = EnumDialogArea.None;

					bounds.CalcWorldBounds();

					if (bounds.horizontalSizing != ElementSizing.Percentual)
					{
						width = Math.Max(width, bounds.OuterWidth + bounds.relX);
					}
					if (bounds.verticalSizing != ElementSizing.Percentual)
					{
						height = Math.Max(height, bounds.OuterHeight + bounds.relY);
					}

					// Reassign actual alignment, now as we can calculate the alignment
					bounds.Alignment = prevAlign;
				}

				// AngelFix - replaced this part of the method entirely with what follows below
				/*if (width == 0 || height == 0)
				{
					throw new Exception("Couldn't build bounds from children, there were probably no child elements using fixed sizing! (or they were size 0)");
				}

				if (__instance.horizontalSizing != ElementSizing.Fixed)
				{
					__instance.absInnerWidth = width;
				}

				if (__instance.verticalSizing != ElementSizing.Fixed)
				{
					__instance.absInnerHeight = height;
				}*/

				if (__instance.horizontalSizing == ElementSizing.FitToChildren)
				{
					if (width == 0) throw new Exception("Couldn't build horizontal bounds from children, there were probably no child elements using fixed sizing! (or they were size 0)");

					__instance.absInnerWidth = width;
				}

				if (__instance.verticalSizing == ElementSizing.FitToChildren)
				{
					if (height == 0) throw new Exception("Couldn't build vertical bounds from children, there were probably no child elements using fixed sizing! (or they were size 0)");

					__instance.absInnerHeight = height;
				}

				// Indicates to Harmony to skip the original
				return false;
			}
		}
	}
}