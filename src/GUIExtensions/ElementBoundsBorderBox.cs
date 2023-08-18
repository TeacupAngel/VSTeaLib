using System;
using System.Reflection;
using Cairo;
using Vintagestory.API.Client;

using HarmonyLib;

namespace TeaLib
{
	namespace GuiExtensions
	{
		// The equivalent of a CSS element with box-sizing: border-box
		public class ElementBoundsBorderBox : ElementBounds
		{
			private bool _fitToChildrenAllowEmpty = false;

			public ElementBoundsBorderBox AllowFitToChildrenEmpty()
			{
				_fitToChildrenAllowEmpty = true;
				return this;
			}

			public override void CalcWorldBounds()
			{
				//requiresrelculation = false;
				AccessTools.Field(typeof(ElementBounds), "requiresrelculation").SetValue(this, false);

				absOffsetX = scaled(fixedOffsetX);
				absOffsetY = scaled(fixedOffsetY);

				if (horizontalSizing == ElementSizing.FitToChildren && verticalSizing == ElementSizing.FitToChildren)
				{
					absFixedX = scaled(fixedX);
					absFixedY = scaled(fixedY);

					absPaddingX = scaled(fixedPaddingX);
					absPaddingY = scaled(fixedPaddingY);

					//buildBoundsFromChildren();
					// No point in saving this MethodInfo to a variable, it never gets called more than once at a time
					Angelfix_BoundsFromChildren();
				}
				else
				{
					switch (horizontalSizing)
					{
						case ElementSizing.Fixed:
							absFixedX = scaled(fixedX);
							if (LeftOfBounds != null) absFixedX += LeftOfBounds.absFixedX + LeftOfBounds.OuterWidth;

							absInnerWidth = scaled(fixedWidth);
							absPaddingX = scaled(fixedPaddingX);
							break;

						case ElementSizing.Percentual:
						case ElementSizing.PercentualSubstractFixed:
							absFixedX = percentX * ParentBounds.InnerWidth;
							absPaddingX = scaled(fixedPaddingX) + percentPaddingX * ParentBounds.InnerWidth;
							absInnerWidth = percentWidth * ParentBounds.InnerWidth - absPaddingX * 2;

							if (horizontalSizing == ElementSizing.PercentualSubstractFixed)
							{
								absInnerWidth -= scaled(fixedWidth);
							}
							break;

						case ElementSizing.FitToChildren:
							absFixedX = scaled(fixedX);
							absPaddingX = scaled(fixedPaddingX);

							//buildBoundsFromChildren();
							Angelfix_BoundsFromChildren();
							break;
					}

					switch (verticalSizing)
					{
						case ElementSizing.Fixed:
							absFixedY = scaled(fixedY);
							absInnerHeight = scaled(fixedHeight);
							absPaddingY = scaled(fixedPaddingY);
							break;

						case ElementSizing.Percentual:
						case ElementSizing.PercentualSubstractFixed:
							absFixedY = percentY * ParentBounds.InnerHeight; // OuterHeight changed to InnerHeight
							absPaddingY = scaled(fixedPaddingY) + percentPaddingY * ParentBounds.InnerHeight;
							absInnerHeight = percentHeight * ParentBounds.InnerHeight - absPaddingY * 2;

							// AngelFix - Fixed, as it is in the transpiler
							if (verticalSizing == ElementSizing.PercentualSubstractFixed)
							{
								absInnerHeight -= scaled(fixedHeight);
							}

							break;

						case ElementSizing.FitToChildren:
							absFixedY = scaled(fixedY);
							absPaddingY = scaled(fixedPaddingY);

							//buildBoundsFromChildren();
							Angelfix_BoundsFromChildren();
							break;
					}
				}

				// Only if the parent element has been initialized already
				if (ParentBounds.Initialized)
				{
					//calcMarginFromAlignment(ParentBounds.InnerWidth, ParentBounds.InnerHeight);
					AccessTools.Method(typeof(ElementBounds), "calcMarginFromAlignment").Invoke(this, new object[] {ParentBounds.InnerWidth, ParentBounds.InnerHeight});
				}

				Initialized = true;

				foreach (ElementBounds child in ChildBounds)
				{
					if (!child.Initialized)
					{
						child.CalcWorldBounds();
					}
				}
			}

			public void Angelfix_BoundsFromChildren()
			{
				if (_fitToChildrenAllowEmpty) 
				{
					buildBoundsFromChildrenAllowEmpty();
				}
				else
				{
					AccessTools.Method(typeof(ElementBounds), "buildBoundsFromChildren").Invoke(this, null);
				}
			}

			// buildBoundsFromChildren currently doesn't allow no child bounds. 
			// That is massively inconvenient so here's a version of the method that just sets size to 0 if there's no children.
			// Really hoping to get at least a bool parameter somewhere to allow this in vanilla
			public void buildBoundsFromChildrenAllowEmpty()
			{
				double width = 0;
				double height = 0;

				foreach (ElementBounds bounds in ChildBounds)
				{
					if (bounds == this)
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

				if (horizontalSizing == ElementSizing.FitToChildren)
				{
					absInnerWidth = width;
				}

				if (verticalSizing == ElementSizing.FitToChildren)
				{
					absInnerHeight = height;
				}
			}
		}
	}
}