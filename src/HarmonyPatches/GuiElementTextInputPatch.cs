using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
		public class GuiElementTextInputPatch : VSHarmonyPatchBase
		{
			public override void Execute(Harmony harmony, ICoreAPI api)
			{
				// TODO: .NET7 TO-FIX
				/*harmony.Patch(AccessTools.Method(typeof(GuiElementTextInput), "RenderInteractiveElements"),
					transpiler: GetPatchMethod("RenderInteractiveElementsTranspiler") 
				);*/
			}

			// Ideas for fixing placeholder text position
			// - add `(Bounds.InnerHeight - placeHolderTextTexture.Height) / 2` to Y position
			// --- possibly doable by leaving as much as possible in place and only changing highlightBounds to Bounds
			// - replace highlightBounds with Bounds
			/* public static IEnumerable<CodeInstruction> RenderInteractiveElementsTranspiler(IEnumerable<CodeInstruction> instructions)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions);

				// STEP 1: Switch `api.Render.GlScissor(...)` and `api.Render.GlScissorFlag(true)` for `api.Render.PushScissor(Bounds, true)` - PushScissor stacks, 
				// unlike GlScissor/GlScissorFlag, so it works with GUI containers
				
				// First, insert `api.Render.PushScissor(Bounds, true);` 
				codeMatcher
				.Start()
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(GuiElement), "api"),
					new CodeInstruction(OpCodes.Callvirt, typeof(ICoreClientAPI).GetProperty("Render", BindingFlags.Public | BindingFlags.Instance).GetGetMethod()),
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(GuiElement), "Bounds"),
					new CodeInstruction(OpCodes.Ldc_I4_1),
					CodeInstruction.Call(typeof(IRenderAPI), "PushScissor")
				);

				// Find the start of `api.Render.GlScissor(...)`
				codeMatcher.MatchStartForward(
					new CodeMatch(OpCodes.Ldarg_0, name: "labeled_ldarg_0"),
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElement), "api")),
					new CodeMatch(instruction => instruction.Calls(typeof(ICoreClientAPI).GetProperty("Render", BindingFlags.Public | BindingFlags.Instance).GetGetMethod())),
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElement), "Bounds")),
					new CodeMatch(instruction => instruction.Calls(typeof(ElementBounds).GetProperty("renderX", BindingFlags.Public | BindingFlags.Instance).GetGetMethod()))
				);

				// Save labels from these instructions to restore them after these instructions get deleted
				List<Label> extractedLabels = codeMatcher.NamedMatch("labeled_ldarg_0").ExtractLabels();

				int removeStart = codeMatcher.Pos;

				// Find the end of `api.Render.GlScissorFlag(true)`
				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(instruction => instruction.Calls(typeof(IRenderAPI).GetMethod("GlScissorFlag", BindingFlags.Public | BindingFlags.Instance)))
				);

				int removeEnd = codeMatcher.Pos;

				// Remove GlScissor and GlScissorFlag(true)
				codeMatcher.RemoveInstructionsInRange(removeStart, removeEnd);
				codeMatcher.Advance(removeStart - removeEnd);

				// Restore the previously deleted labels
				codeMatcher.Instruction.WithLabels(extractedLabels);

				// Find and remove GlScissorFlag(false)
				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Ldc_I4_0),
					new CodeMatch(instruction => instruction.Calls(typeof(IRenderAPI).GetMethod("GlScissorFlag", BindingFlags.Public | BindingFlags.Instance)))
				);

				removeEnd = codeMatcher.Pos;
				removeStart = removeEnd - 1;

				codeMatcher
				.RemoveInstructionsInRange(removeStart, removeEnd)
				.Advance(removeStart - removeEnd);

				// This left an `api.Render` stub there, which will crash on its own. Insert `.PopScissor()` to complete it
				codeMatcher.Insert(
					CodeInstruction.Call(typeof(IRenderAPI), "PopScissor")
				);

				// STEP 2: Fix placeholder text rendering position by copying it from the method call rendering the main text

				// First, find the instruction governing the bounds of the main text rendering
				codeMatcher
				.End()
				.MatchEndBackwards(
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElementEditableTextBase), "textTexture")),
					new CodeMatch(CodeInstruction.LoadField(typeof(LoadedTexture), "TextureId"))
				)
				.Advance(1);

				int copyStart = codeMatcher.Pos;

				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElement), "Bounds")),
					new CodeMatch(instruction => instruction.Calls(typeof(ElementBounds).GetProperty("renderY", BindingFlags.Public | BindingFlags.Instance).GetGetMethod()))
				);

				int copyEnd = codeMatcher.Pos;

				// Save the instructions we just found
				List<CodeInstruction> copiedInstructions = codeMatcher.InstructionsInRange(copyStart, copyEnd);

				// Find the placeholder text bounds instructions we want to remove
				codeMatcher
				.Start()
				.MatchEndForward(
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElementTextInput), "placeHolderTextTexture")),
					new CodeMatch(CodeInstruction.LoadField(typeof(LoadedTexture), "TextureId"))
				)
				.Advance(1);

				removeStart = codeMatcher.Pos;

				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Div),
					new CodeMatch(OpCodes.Add),
					new CodeMatch(OpCodes.Conv_I4),
					new CodeMatch(OpCodes.Conv_R4)
				);

				removeEnd = codeMatcher.Pos;

				// Remove the placeholder bounds instructions
				codeMatcher
				.RemoveInstructionsInRange(removeStart, removeEnd)
				.Advance(removeStart - removeEnd);

				// ... and replace them by pasting in the instructions we copied earlier
				codeMatcher.Insert(copiedInstructions);

				return codeMatcher.InstructionEnumeration();
			} */

			public static IEnumerable<CodeInstruction> RenderInteractiveElementsTranspiler(IEnumerable<CodeInstruction> instructions)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions);

				// STEP 1: Switch `api.Render.GlScissor(...)` and `api.Render.GlScissorFlag(true)` for `api.Render.PushScissor(Bounds, true)` - PushScissor stacks, 
				// unlike GlScissor/GlScissorFlag, so it works with GUI containers
				
				// First, insert `api.Render.PushScissor(Bounds, true);` 
				codeMatcher
				.Start()
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(GuiElement), "api"),
					new CodeInstruction(OpCodes.Callvirt, typeof(ICoreClientAPI).GetProperty("Render", BindingFlags.Public | BindingFlags.Instance).GetGetMethod()),
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(GuiElement), "Bounds"),
					new CodeInstruction(OpCodes.Ldc_I4_1),
					CodeInstruction.Call(typeof(IRenderAPI), "PushScissor")
				);

				// Find the start of `api.Render.GlScissor(...)`
				codeMatcher.MatchStartForward(
					new CodeMatch(OpCodes.Ldarg_0, name: "labeled_ldarg_0"),
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElement), "api")),
					new CodeMatch(instruction => instruction.Calls(typeof(ICoreClientAPI).GetProperty("Render", BindingFlags.Public | BindingFlags.Instance).GetGetMethod())),
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElement), "Bounds")),
					new CodeMatch(instruction => instruction.Calls(typeof(ElementBounds).GetProperty("renderX", BindingFlags.Public | BindingFlags.Instance).GetGetMethod()))
				);

				// Save labels from these instructions to restore them after these instructions get deleted
				List<Label> extractedLabels = codeMatcher.NamedMatch("labeled_ldarg_0").ExtractLabels();

				int removeStart = codeMatcher.Pos;

				// Find the end of `api.Render.GlScissorFlag(true)`
				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(instruction => instruction.Calls(typeof(IRenderAPI).GetMethod("GlScissorFlag", BindingFlags.Public | BindingFlags.Instance)))
				);

				int removeEnd = codeMatcher.Pos;

				// Remove GlScissor and GlScissorFlag(true)
				codeMatcher.RemoveInstructionsInRange(removeStart, removeEnd);
				codeMatcher.Advance(removeStart - removeEnd);

				// Restore the previously deleted labels
				codeMatcher.Instruction.WithLabels(extractedLabels);

				// Find and remove GlScissorFlag(false)
				codeMatcher.MatchEndForward(
					new CodeMatch(OpCodes.Ldc_I4_0),
					new CodeMatch(instruction => instruction.Calls(typeof(IRenderAPI).GetMethod("GlScissorFlag", BindingFlags.Public | BindingFlags.Instance)))
				);

				removeEnd = codeMatcher.Pos;
				removeStart = removeEnd - 1;

				codeMatcher
				.RemoveInstructionsInRange(removeStart, removeEnd)
				.Advance(removeStart - removeEnd);

				// This left an `api.Render` stub there, which will crash on its own. Insert `.PopScissor()` to complete it
				codeMatcher.Insert(
					CodeInstruction.Call(typeof(IRenderAPI), "PopScissor")
				);

				// STEP 2: Fix placeholder text rendering position by copying it from the method call rendering the main text

				// Find the `api.Render.GlToggleBlend(true);` call
				codeMatcher
				.Start()
				.MatchEndForward(
					new CodeMatch(instruction => instruction.Calls(AccessTools.Method(typeof(IRenderAPI), "GlToggleBlend")))
				);

				// In the call to `Render2DTexturePremultipliedAlpha`, replace all references to `highlightBounds` with `Bounds`
				// (the first reference to `highlightBounds` is in `Render2DTexture`, we don't want to replace that one)
				codeMatcher
				.MatchStartForward(
					new CodeMatch(CodeInstruction.LoadField(typeof(GuiElementTextInput), "highlightBounds"))
				)
				.Repeat((codeMatcher) => {
					codeMatcher
					.RemoveInstruction()
					.InsertAndAdvance(CodeInstruction.LoadField(typeof(GuiElement), "Bounds"));
				}); // TODO: Add a "fail" method as the second argument here!

				return codeMatcher.InstructionEnumeration();
			}
		}
	}
}