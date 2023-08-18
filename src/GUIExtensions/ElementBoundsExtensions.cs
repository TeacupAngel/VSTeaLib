using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace GuiExtensions
	{
		public static class ElementBoundsExtensions
		{
			/// <summary>
			/// Set the percentual width and height values
			/// </summary>
			/// <param name="width"></param>
			/// <param name="height"></param>
			/// <returns></returns>
			public static ElementBounds WithPercentSize(this ElementBounds bounds, double width, double height)
			{
				bounds.percentWidth = width;
				bounds.percentHeight = height;
				return bounds;
			}

			/// <summary>
			/// Set the percentual width property
			/// </summary>
			/// <param name="width"></param>
			/// <returns></returns>
			public static ElementBounds WithPercentWidth(this ElementBounds bounds, double width)
			{
				bounds.percentWidth = width;
				return bounds;
			}


			/// <summary>
			/// Set the percentual height property
			/// </summary>
			/// <param name="height"></param>
			/// <returns></returns>
			public static ElementBounds WithPercentHeight(this ElementBounds bounds, double height)
			{
				bounds.percentHeight = height;
				return bounds;
			}

			/// <summary>
			/// Set the horizontal sizing property. See also <seealso cref="ElementSizing"/>.
			/// </summary>
			/// <param name="horizontalSizing"></param>
			/// <param name="verticalSizing"></param>
			/// <returns></returns>
			public static ElementBounds WithHorizontalSizing(this ElementBounds bounds, ElementSizing horizontalSizing)
			{
				bounds.horizontalSizing = horizontalSizing;
				return bounds;
			}

			/// <summary>
			/// Set the vertical sizing property. See also <seealso cref="ElementSizing"/>.
			/// </summary>
			/// <param name="horizontalSizing"></param>
			/// <param name="verticalSizing"></param>
			/// <returns></returns>
			public static ElementBounds WithVerticalSizing(this ElementBounds bounds, ElementSizing verticalSizing)
			{
				bounds.verticalSizing = verticalSizing;
				return bounds;
			}

			/// <summary>
			/// Sets the parent of the bounds; unlike the WithParent method, this also assigns these bounds as a child to the parent 
			/// </summary>
			/// <param name="bounds"></param>
			/// <returns></returns>
			public static ElementBounds WithParentMutual(this ElementBounds bounds, ElementBounds newParentBounds)
			{
				newParentBounds.WithChild(bounds);
				return bounds;
			}

			/// <summary>
			/// Creates a border-box clone of the bounds without child elements
			/// </summary>
			/// <returns></returns>
			public static ElementBoundsBorderBox AsBorderBoxBounds(this ElementBounds bounds)
			{
				return new ElementBoundsBorderBox()
				{
					Alignment = bounds.Alignment,
					verticalSizing = bounds.verticalSizing,
					horizontalSizing = bounds.horizontalSizing,
					percentHeight = bounds.percentHeight,
					percentWidth = bounds.percentHeight,
					fixedOffsetX = bounds.fixedOffsetX,
					fixedOffsetY = bounds.fixedOffsetY,
					fixedX = bounds.fixedX,
					fixedY = bounds.fixedY,
					fixedWidth = bounds.fixedWidth,
					fixedHeight = bounds.fixedHeight,
					fixedPaddingX = bounds.fixedPaddingX,
					fixedPaddingY = bounds.fixedPaddingY,
					fixedMarginX = bounds.fixedMarginX,
					fixedMarginY = bounds.fixedMarginY,
					percentPaddingX = bounds.percentPaddingX,
					percentPaddingY = bounds.percentPaddingY,
					ParentBounds = bounds.ParentBounds
				};
			}
		}
	}
}
