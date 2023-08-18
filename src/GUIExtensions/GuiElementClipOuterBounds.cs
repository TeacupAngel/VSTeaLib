using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

using HarmonyLib;

namespace TeaLib
{
	namespace GuiExtensions
	{
		class GuiElementClipOuterBounds : GuiElement
		{
			bool clip;

			/// <summary>
			/// Adds a clipped area to the GUI.
			/// </summary>
			/// <param name="capi">The Client API</param>
			/// <param name="clip">Do we clip?</param>
			/// <param name="bounds">The bounds of the element.</param>
			public GuiElementClipOuterBounds(ICoreClientAPI capi, bool clip, ElementBounds bounds) : base(capi, bounds)
			{
				this.clip = clip;
			}

			public override void ComposeElements(Context ctxStatic, ImageSurface surface)
			{
				Bounds.CalcWorldBounds();
			}

			public override void RenderInteractiveElements(float deltaTime)
			{
				if (clip)
				{
					PushScissorOuterBounds(Bounds);
				} 
				else
				{
					api.Render.PopScissor();
				}
			}

			public override int OutlineColor()
			{
				return (255 << 16) + (255 << 24);
			}


			public override void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
			{
				// Can't be interacted with
			}

			public void PushScissorOuterBounds(ElementBounds bounds, bool stacking = false)
			{
				if (bounds == null)
				{
					api.Render.GlScissorFlag(false);
				}
				else
				{
					if (stacking && api.Render.ScissorStack.Count > 0)
					{
						ElementBounds prevbounds = api.Render.ScissorStack.Peek();
						double prevx1 = prevbounds.renderX;
						double prevy1 = prevbounds.renderY;
						double prevx2 = prevx1 + prevbounds.OuterWidth;
						double prevy2 = prevy1 + prevbounds.OuterHeight;
						double x2 = bounds.renderX;
						double y2 = bounds.renderY;
						double val = x2 + bounds.OuterWidth;
						double y3 = y2 + bounds.OuterHeight;
						int x = (int)Math.Max(x2, prevx1);
						int y = (int)((double)api.Gui.WindowBounds.OuterHeight - Math.Max(y2, prevy1) - (Math.Min(y3, prevy2) - Math.Max(y2, prevy1)));
						int w = (int)(Math.Min(val, prevx2) - Math.Max(x2, prevx1));
						int h = (int)(Math.Min(y3, prevy2) - Math.Max(y2, prevy1));
						api.Render.GlScissor(x, y, Math.Max(0, w), Math.Max(0, h));
					}
					else
					{
						api.Render.GlScissor((int)bounds.renderX, (int)((double)api.Gui.WindowBounds.OuterHeight - bounds.renderY - bounds.OuterHeight), (int)bounds.OuterWidth, (int)bounds.OuterHeight);
					}

					api.Render.GlScissorFlag(true);
				}
				api.Render.ScissorStack.Push(bounds);
			}
		}

		public static class GuiElementClipOuterHelper
		{
			/// <summary>
			/// Add a clip area. Thhis select an area to be rendered, where anything outside will be invisible. Useful for scrollable content. Can be called multiple times, to reduce the render area further, but needs an equal amount of calls to EndClip()
			/// </summary>
			/// <param name="bounds">The bounds of the object.</param>
			public static GuiComposer BeginClipOuterBounds(this GuiComposer composer, ElementBounds bounds)
			{
				if (!composer.Composed)
				{
					composer.AddInteractiveElement(new GuiElementClipOuterBounds(composer.Api, true, bounds));
					//composer.InsideClipBounds = bounds;
					AccessTools.Field(typeof(GuiComposer), "InsideClipBounds").SetValue(composer, bounds);
					composer.BeginChildElements();
				}
				return composer;
			}

			/// <summary>
			/// Remove a previously added clip area.
			/// </summary>
			public static GuiComposer EndClipOuterBounds(this GuiComposer composer)
			{
				if (!composer.Composed)
				{
					composer.AddInteractiveElement(new GuiElementClipOuterBounds(composer.Api, false, ElementBounds.Empty));
					//composer.InsideClipBounds = null;
					AccessTools.Field(typeof(GuiComposer), "InsideClipBounds").SetValue(composer, null);
					composer.EndChildElements();
				}
				return composer;
			}
		}
	}
}
