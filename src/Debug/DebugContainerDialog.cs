using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using TeaLib.GuiExtensions;

using Cairo;

namespace TeaLib
{
	namespace Debug
	{
		public class DebugContainerDialog : GuiDialog
		{
			public override string ToggleKeyCombinationCode => null;
			public override bool PrefersUngrabbedMouse => false;

			public DebugContainerDialog(ICoreClientAPI capi) : base(capi)
			{
				ComposeDialog();
			}

			public void ComposeDialog(bool clear = false)
			{
				// Auto-sized dialog at the center of the screen
				ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedOffset(0, 0);

				if (clear)
				{
					SingleComposer.Clear(dialogBounds);
				}

				int containerWidth = 200;
				int containerHeight = 350;
				int containerMargin = 16;

				// Background boundaries
				ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
				bgBounds.BothSizing = ElementSizing.FitToChildren;

				// Module area
				ElementBounds fullBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, GuiStyle.TitleBarHeight, containerWidth * 2 + containerMargin, containerHeight * 2 + containerMargin);
				ElementBounds topLeftContainerBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, containerWidth, containerHeight);
				ElementBounds topRightContainerBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 0, containerWidth, containerHeight);
				ElementBounds bottomLeftContainerBounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0, 0, containerWidth, containerHeight);
				ElementBounds bottomRightContainerBounds = ElementBounds.Fixed(EnumDialogArea.RightBottom, 0, 0, containerWidth, containerHeight);

				bgBounds.WithChildren(fullBounds);
				fullBounds.WithChildren(topLeftContainerBounds, topRightContainerBounds, bottomLeftContainerBounds, bottomRightContainerBounds);

				SingleComposer = capi.Gui.CreateCompo("lsystemdebuggerdialog", dialogBounds)
					.AddShadedDialogBG(bgBounds, true)
					.AddDialogTitleBar("Container Debug Dialog", OnTitleBarClose)
					.AddScrollableArea(topLeftContainerBounds, (composer, scrollableBounds) => {
						ComposeContainerTopLeft(composer, scrollableBounds);
					}, key: "topLeftContainer")
					.AddScrollableArea(topRightContainerBounds, (composer, scrollableBounds) => {
						ComposeContainerTopLeft(composer, scrollableBounds);
					}, key: "topRightContainer")
					.AddScrollableArea(bottomLeftContainerBounds, (composer, scrollableBounds) => {
						ComposeContainerTopLeft(composer, scrollableBounds);
					}, key: "bottomLeftContainer")
					.AddScrollableArea(bottomRightContainerBounds, (composer, scrollableBounds) => {
						ComposeContainerTopLeft(composer, scrollableBounds);
					}, key: "bottomRightContainer")
					.Compose()
				;

				SingleComposer.GetScrollableArea("topLeftContainer").CalcTotalHeight();
				SingleComposer.GetScrollableArea("topRightContainer").CalcTotalHeight();
				SingleComposer.GetScrollableArea("bottomLeftContainer").CalcTotalHeight();
				SingleComposer.GetScrollableArea("bottomRightContainer").CalcTotalHeight();
			}

			public void ComposeContainerTopLeft(GuiElementContainer container, ElementBounds listBounds)
			{
				ElementBounds currentBounds = null;

				CairoFont variableFont = CairoFont.WhiteSmallText().WithFontSize(14f);

				for (int i = 0; i < 25; i++)
				{
					currentBounds = currentBounds != null ? 
						ElementBounds.Percentual(EnumDialogArea.CenterMiddle, 1, 1).WithFixedSize(0, 32).FixedUnder(currentBounds, 4) : 
						ElementBounds.Percentual(EnumDialogArea.CenterMiddle, 1, 1).WithFixedSize(0, 32);

					currentBounds.horizontalSizing = ElementSizing.Percentual;
					//currentBounds.verticalSizing = ElementSizing.FitToChildren; // Bleh, I really need this but it's broken :S
					// ElementSizing.FitToChildren crashes when a child has ElementSizing.Percentual, even when Percentual is horizontal and 
					// FitToChildren is vertical, so logically they shouldn't clash. Error is in buildBoundsFromChildren()
					currentBounds.verticalSizing = ElementSizing.Fixed;
					listBounds.WithChild(currentBounds);

					ElementBounds currentKeyBounds = currentBounds.FlatCopy();
					currentKeyBounds.Alignment = EnumDialogArea.LeftTop;
					currentKeyBounds.percentWidth = 0.6;

					GuiElementDynamicText dynamicTextKey = new(capi, $"Option {i}:", variableFont, currentKeyBounds);
					container.Add(dynamicTextKey);

					ElementBounds currentValueBounds = currentBounds.FlatCopy();
					currentValueBounds.Alignment = EnumDialogArea.RightTop;
					currentValueBounds.percentWidth = 0.4;

					//GuiElementScrollableTextInput textInput = new GuiElementScrollableTextInput(capi, currentValueBounds, (string val) => {/*ACTION*/}, CairoFont.WhiteSmallText());
					GuiElementTextInput textInput = new(capi, currentValueBounds, (string val) => {/*ACTION*/}, CairoFont.WhiteSmallText());
					textInput.SetPlaceHolderText("PLACEHOLDER");

					container.Add(textInput);
					if (i > 0) textInput.LoadValue(new List<string> {i.ToString()});
				}
			}

			private void OnTitleBarClose()
			{
				TryClose();
			}
		}
	}
}