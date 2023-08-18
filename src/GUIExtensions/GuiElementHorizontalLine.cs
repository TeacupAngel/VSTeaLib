using Cairo;
using Vintagestory.API.Client;

namespace TeaLib
{
	namespace GuiExtensions
	{
		class GuiElementHorizontalLine : GuiElement
		{
			private int width;
			private float brightness;

			/// <summary>
			/// Creates a new inset for the GUI.
			/// </summary>
			/// <param name="capi">The Client API</param>
			/// <param name="bounds">The bounds of the Element.</param>
			/// <param name="width">The width of the line.</param>
			/// <param name="brightness">The brightness of the line.</param>
			public GuiElementHorizontalLine(ICoreClientAPI capi, ElementBounds bounds, int width = 1, float brightness = 0.3f) : base(capi, bounds)
			{
				this.width = width;
				this.brightness = brightness;
			}

			public override void ComposeElements(Context ctx, ImageSurface surface)
			{
				Bounds.CalcWorldBounds();

				ctx.Rectangle(Bounds.drawX, Bounds.drawY + Bounds.InnerHeight / 2, Bounds.InnerWidth, width);
				ctx.SetSourceRGBA(0, 0, 0, brightness);
				ctx.LineWidth = width;
				ctx.Stroke();
			}
		}

		public static class GuiElementInsetHelper
		{
			/// <summary>
			/// Adds a horizontal line to the current GUI.
			/// </summary>
			/// <param name="bounds">The bounds of the inset.</param>
			/// <param name="width">The width of the line.</param>
			/// <param name="brightness">The brightness of the line.</param>
			public static GuiComposer AddHorizontalLine(this GuiComposer composer, ElementBounds bounds, int width = 1, float brightness = 0.3f)
			{
				if (!composer.Composed)
				{
					composer.AddStaticElement(new GuiElementHorizontalLine(composer.Api, bounds, width, brightness));
				}
				return composer;
			}
		}
	}
}
