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
using TeaLib.TeaConfig;

namespace TeaLib
{
	namespace HarmonyPatches
	{
		[VSHarmonyPatch(EnumPatchType.Client)]
		public class HarmonyPatchGuiDialogEscapeMenu : VSHarmonyPatchBase
		{
			private static ICoreClientAPI capi;

			public override void Execute(Harmony harmony, ICoreAPI api)
			{
				capi = api as ICoreClientAPI;

				Type guiDialogEscapeMenuType = AccessTools.TypeByName("Vintagestory.Client.NoObf.GuiDialogEscapeMenu");

				harmony.Patch(AccessTools.Method(guiDialogEscapeMenuType, "EscapeMenuHome"),
					transpiler: GetPatchMethod("EscapeMenuHomeTranspiler") 
				);
			}

			public static IEnumerable<CodeInstruction> EscapeMenuHomeTranspiler(IEnumerable<CodeInstruction> instructions)
			{
				CodeMatcher codeMatcher = new(instructions);

				/* STEP 1: Insert this bit of code into the place where we'll be adding our button

				 ```
				 .Execute(delegate
				 {
					bposy += 0.5f;
				 })
				 ```

				 It goes just after this line in the original code: 
				 `.AddButton(Lang.Get("mainmenu-settings"), gameSettingsMenu.OpenSettingsMenu, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth))`
				*/

				// Match the position of this bit of code: `.AddIf(game.IsSingleplayer && !game.OpenedToLan)` and note down the position just after it
				Type guiDialogEscapeMenuType = AccessTools.TypeByName("Vintagestory.Client.NoObf.GuiDialogEscapeMenu");

				codeMatcher
				.Start()
				.MatchEndForward(
					new CodeMatch(CodeInstruction.LoadArgument(0)),
					new CodeMatch(CodeInstruction.LoadField(guiDialogEscapeMenuType, "game")),
					new CodeMatch(CodeInstruction.LoadField(typeof(ClientMain), "IsSingleplayer")),
					new CodeMatch(new CodeInstruction(OpCodes.Brfalse_S)),
					new CodeMatch(CodeInstruction.LoadArgument(0)),
					new CodeMatch(CodeInstruction.LoadField(guiDialogEscapeMenuType, "game")),
					new CodeMatch(CodeInstruction.LoadField(typeof(ClientMain), "OpenedToLan")),
					new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_0)),
					new CodeMatch(new CodeInstruction(OpCodes.Ceq)),
					new CodeMatch(new CodeInstruction(OpCodes.Br_S)),
					new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_0)),
					new CodeMatch(instruction => instruction.Calls(AccessTools.Method(typeof(GuiComposer), "AddIf")))
				)
				.Advance(1);

				int copyStart = codeMatcher.Pos;

				// Match the next `Execute` method call, and note down its position
				codeMatcher
				.MatchEndForward(
					new CodeMatch(instruction => instruction.Calls(AccessTools.Method(typeof(GuiComposer), "Execute")))
				);

				int copyEnd = codeMatcher.Pos;

				// Now copy all the instructions between the two positions we just marked
				List<CodeInstruction> copiedInstructions = codeMatcher.InstructionsInRange(copyStart, copyEnd);

				// Search backwards for the place just after this line:
				// `.AddButton(Lang.Get("mainmenu-settings"), gameSettingsMenu.OpenSettingsMenu, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth))`
				MethodInfo addButtonMethod = AccessTools.Method(typeof(Vintagestory.API.Client.GuiComposerHelpers), "AddButton", 
					new Type[] {typeof(GuiComposer), typeof(string), typeof(ActionConsumable), typeof(ElementBounds), typeof(EnumButtonStyle), typeof(string)});

				codeMatcher
				.MatchEndBackwards(new CodeMatch(OpCodes.Ldstr, "mainmenu-settings"))
				.MatchEndForward(new CodeMatch(instruction => instruction.Calls(addButtonMethod)))
				.Advance(1);

				// Finally, insert the copied instructions
				codeMatcher.InsertAndAdvance(copiedInstructions);

				// STEP 2: copy the following line into the current position, then change
				// `.AddButton(Lang.Get("mainmenu-settings"), gameSettingsMenu.OpenSettingsMenu, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth))`

				// Mark down our current position to copy into later
				int insertPos = codeMatcher.Pos;

				// Search backwards for the start of this line, and mark the position:
				// `.AddButton(Lang.Get("pause-open2lan"), onOpenToLan, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth))`
				codeMatcher.MatchEndForward(new CodeMatch(OpCodes.Ldstr, "pause-open2lan"));
				copyStart = codeMatcher.Pos;

				// Search forwards for the end of the line, and mark this position too
				codeMatcher
				.MatchEndForward(new CodeMatch(instruction => instruction.Calls(addButtonMethod)));
				copyEnd = codeMatcher.Pos;

				// Copy the instructions between the positions we just marked down, then insert them just after the 
				copiedInstructions = codeMatcher.InstructionsInRange(copyStart, copyEnd);
				codeMatcher.Start().Advance(insertPos);

				codeMatcher.Insert(copiedInstructions);

				// Find the button string and method called on click, and change them to our own
				codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "pause-open2lan"));
				codeMatcher.Instruction.operand = "tealib:mod-settings";

				codeMatcher
				.MatchStartForward(
					new CodeMatch(CodeInstruction.LoadArgument(0)),
					new CodeMatch(OpCodes.Ldftn)
				)
				.RemoveInstructions(2)
				.Insert(
					new CodeInstruction(OpCodes.Ldnull),
					new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(HarmonyPatchGuiDialogEscapeMenu),"OpenModSettings"))
				);

				return codeMatcher.InstructionEnumeration();
			}

			public static bool OpenModSettings()
			{
				// Close the mod config (lots of work to call an internal class methods haha)
				Type guiDialogEscapeMenuType = AccessTools.TypeByName("Vintagestory.Client.NoObf.GuiDialogEscapeMenu");
				MethodInfo tryCloseMethodInfo = AccessTools.Method(guiDialogEscapeMenuType, "TryClose");

				GuiDialog escapeMenuInstance = capi.Gui.LoadedGuis.Find(dialog => dialog.GetType() == guiDialogEscapeMenuType);
				tryCloseMethodInfo.Invoke(escapeMenuInstance, new object[0]);

				// Open our mod settings
				capi.ModLoader.GetModSystem<TeaConfigDialogSystem>().OpenModDialog();

				return true;
			}

			public override void Dispose()
			{
				capi = null;
			}
		}
	}
}