// By Fulgen
// Usage example: 
/*
Composer
	.BeginChildElements(ElementBounds.Fixed(0.0, 45.0, fixedWidth, 600.0).WithAlignment(EnumDialogArea.CenterFixed))
		.AddScrollableArea(elementBounds3, (composer, bounds) =>
		{
			composer.AddCellList(bounds.WithFixedPadding(10.0), OnRequireNewCell, OnMouseDownOnCellLeft, null, new(), "mods");
		}, key: "scrollableArea")
	.EndChildElements()
*/

using System;
using System.Reflection;
using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace GuiExtensions
	{
		public delegate void ScrollableAreaAddContents(GuiElementContainer container, ElementBounds bounds);

		public class GuiElementScrollableArea : GuiElement
		{
			public ElementBounds ClippingBounds { get; private set; }
			public ElementBounds ContentBounds { get; private set; }

			public GuiElementScrollbar Scrollbar { get; private set; }

			public float ScrollbarWidth { get; set; } = 16;

			private static int _scrollableId;

			private readonly string _containerKey;

			public GuiElementScrollableArea(GuiComposer composer, ElementBounds bounds, ScrollableAreaAddContents addContentsDelegate, string key = null, double padding = 8.0, Action<float> onNewScrollbarValue = null, int insetDepth = 4, float insetBrightness = 0.85f) : base(composer.Api, bounds)
			{
				ElementBounds areaBounds = ElementBounds.Percentual(EnumDialogArea.LeftTop, 1, 1)
				.WithHorizontalSizing(ElementSizing.PercentualSubstractFixed)
				.WithFixedWidth(ScrollbarWidth)
				.WithParentMutual(bounds);

				ElementBounds scrollbarBounds = ElementBounds.Percentual(EnumDialogArea.RightTop, 1, 1)
				.WithHorizontalSizing(ElementSizing.Fixed)
				.WithFixedWidth(ScrollbarWidth)
				.WithParentMutual(bounds);

				ClippingBounds = ElementBounds.Percentual(EnumDialogArea.LeftTop, 1, 1)
				.AsBorderBoxBounds()
				.WithParentMutual(areaBounds);

				ContentBounds = ElementBounds.Percentual(EnumDialogArea.LeftTop, 1, 1)
				.WithFixedPadding(padding)
				.WithVerticalSizing(ElementSizing.FitToChildren)
				.AsBorderBoxBounds()
				.AllowFitToChildrenEmpty()
				.WithParentMutual(ClippingBounds);
				ContentBounds.fixedY = 0;

				Scrollbar = new GuiElementScrollbar(composer.Api, (value) =>
					{
						ContentBounds.fixedY = -value;
						ContentBounds.CalcWorldBounds();
						onNewScrollbarValue?.Invoke(value);
					}, 
					scrollbarBounds);

				_containerKey = (key ?? $"scrollable_area_{_scrollableId++}") + "_container";

				composer
					.AddInset(areaBounds, insetDepth, insetBrightness)
					.AddInteractiveElement(Scrollbar)
					.BeginClipOuterBounds(ClippingBounds)
					.AddContainer(ContentBounds, _containerKey);

				addContentsDelegate(composer.GetContainer(_containerKey), ContentBounds);

				composer.EndClipOuterBounds();
			}

			public void ScrollToTop()
			{
				Scrollbar.SetScrollbarPosition(0);
			}

			public void ScrollToBottom()
			{
				Scrollbar.ScrollToBottom();
			}

			public void CalcTotalHeight()
			{				
				Scrollbar.SetHeights((float) ClippingBounds.OuterHeight, (float) ContentBounds.OuterHeight);
			}

			public void RebuildContents(GuiComposer composer, ScrollableAreaAddContents addContentsDelegate)
			{
				GuiElementContainer container = composer.GetContainer(_containerKey);
				container.Elements.Clear();
				addContentsDelegate(composer.GetContainer(_containerKey), ContentBounds);
				container.ComposeElements(null, null);
			}
		}

		public static partial class GuiComposerHelpers
		{
			public static GuiComposer AddScrollableArea(this GuiComposer composer, ElementBounds bounds, ScrollableAreaAddContents _addContentsDelegate, string key = null, double padding = 8.0, Action<float> onNewScrollbarValue = null, int insetDepth = 4, float insetBrightness = 0.85f)
			{
				if (!composer.Composed)
				{
					composer.AddStaticElement(new GuiElementScrollableArea(composer, bounds, _addContentsDelegate, key, padding, onNewScrollbarValue, insetDepth, insetBrightness), key);
				}

				return composer;
			}

			public static GuiElementScrollableArea GetScrollableArea(this GuiComposer composer, string key)
			{
				return composer.GetElement(key) as GuiElementScrollableArea;
			}
		}
	}
}