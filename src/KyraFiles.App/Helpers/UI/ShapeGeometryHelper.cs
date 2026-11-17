// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// Generates the outline of the control shape models and applies it as a composition clip.
	/// </summary>
	/// <remarks>
	/// <see cref="UIElement.Clip"/> only accepts a rectangle in WinUI 3, so free-form outlines are
	/// applied on the element's composition visual with a geometric clip.
	/// </remarks>
	public static class ShapeGeometryHelper
	{
		/// <summary>
		/// Clips <paramref name="element"/> to the outline of <paramref name="style"/> sized to its current
		/// actual size and scaled by <paramref name="scale"/>. <see cref="ControlShapeStyleKind.FluentStandard"/> (or an element not laid out yet) removes the clip.
		/// </summary>
		public static void ApplyClip(FrameworkElement element, ControlShapeStyleKind style, float scale = 1f)
		{
			var visual = ElementCompositionPreview.GetElementVisual(element);

			if (style == ControlShapeStyleKind.FluentStandard || element.ActualWidth <= 0 || element.ActualHeight <= 0)
			{
				visual.Clip = null;
				return;
			}

			var compositor = visual.Compositor;
			var geometry = compositor.CreatePathGeometry(BuildPath(style, (float)element.ActualWidth, (float)element.ActualHeight, scale));
			visual.Clip = compositor.CreateGeometricClip(geometry);
		}

		/// <summary>
		/// Builds the closed outline of the requested shape style for the given width and height, scaled by <paramref name="scale"/>.
		/// Elements with height &lt; 80px use a full lateral profile; larger elements only profile the 4 corners.
		/// </summary>
		public static CompositionPath BuildPath(ControlShapeStyleKind style, float width, float height, float scale = 1f)
		{
			using var builder = new CanvasPathBuilder(null);
			bool isSmall = height < 80f;

			switch (style)
			{
				case ControlShapeStyleKind.TicketWave:
					BuildTicketWave(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.RibbonDock:
					BuildRibbonDock(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.SawtoothTriangle:
					BuildSawtoothTriangle(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.SinusoidalWave:
					BuildSinusoidalInvert(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.BiConcaveHourglass:
					BuildBiConcave(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.HybridRectangular:
					BuildHybridRectangular(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.CyberpunkTrapezoid:
					BuildCyberpunk(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.DirectionalPill:
					BuildDirectionalPill(builder, width, height, scale, isSmall);
					break;
				case ControlShapeStyleKind.HexagonalTech:
					BuildHexagonal(builder, width, height, scale, isSmall);
					break;
				default:
					BuildStandardRounded(builder, width, height, 8f * scale);
					break;
			}

			return new CompositionPath(CanvasGeometry.CreatePath(builder));
		}

		/// <summary>Ticket Wave: Left concave; right wave ripples.</summary>
		private static void BuildTicketWave(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float concaveDepth = Math.Min(12f * s, w * 0.15f);
				float waveLen = 16f * s;
				int count = Math.Max(1, (int)Math.Floor(h / waveLen));
				float segH = h / count;
				float waveAmp = Math.Min(8f * s, w * 0.1f);
				float rightBase = w - waveAmp;

				b.BeginFigure(concaveDepth, 0);
				b.AddLine(rightBase, 0);
				for (int i = 0; i < count; i++)
				{
					float y0 = i * segH;
					b.AddCubicBezier(
						new Vector2(rightBase + waveAmp, y0 + segH * 0.25f),
						new Vector2(rightBase + waveAmp, y0 + segH * 0.75f),
						new Vector2(rightBase, y0 + segH));
				}
				b.AddLine(concaveDepth, h);
				b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right: wave ripple
				b.AddCubicBezier(new Vector2(w - cd * 0.3f, 0), new Vector2(w, cd * 0.3f), new Vector2(w - 3f * s, cd * 0.6f));
				b.AddCubicBezier(new Vector2(w - 2f * s, cd * 0.8f), new Vector2(w, cd), new Vector2(w, cd));

				b.AddLine(w, h - cd);

				// Bottom-Right: wave ripple
				b.AddCubicBezier(new Vector2(w, h - cd * 0.7f), new Vector2(w - 3f * s, h - cd * 0.4f), new Vector2(w - cd * 0.5f, h - cd * 0.2f));
				b.AddCubicBezier(new Vector2(w - cd * 0.8f, h), new Vector2(w - cd, h), new Vector2(w - cd, h));

				b.AddLine(cd, h);

				// Bottom-Left: concave arc
				b.AddArc(new Vector2(0, h - cd), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(0, cd);

				// Top-Left: concave arc
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Ribbon Dock: Left concave; right dual rounded lobes with inner notch.</summary>
		private static void BuildRibbonDock(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float concaveDepth = Math.Min(12f * s, w * 0.15f);
				float lobeDepth = Math.Min(10f * s, w * 0.1f);
				float midNotch = Math.Min(12f * s, w * 0.12f);
				float rightBase = w - lobeDepth;

				b.BeginFigure(concaveDepth, 0);
				b.AddLine(rightBase, 0);
				b.AddCubicBezier(new Vector2(w, h * 0.15f), new Vector2(w, h * 0.35f), new Vector2(w - midNotch, h * 0.5f));
				b.AddCubicBezier(new Vector2(w, h * 0.65f), new Vector2(w, h * 0.85f), new Vector2(rightBase, h));
				b.AddLine(concaveDepth, h);
				b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right: rounded lobe
				b.AddCubicBezier(new Vector2(w - cd * 0.2f, 0), new Vector2(w, cd * 0.3f), new Vector2(w - 3f * s, cd * 0.5f));
				b.AddCubicBezier(new Vector2(w - 4f * s, cd * 0.7f), new Vector2(w, cd * 0.8f), new Vector2(w, cd));

				b.AddLine(w, h - cd);

				// Bottom-Right: rounded lobe
				b.AddCubicBezier(new Vector2(w, h - cd * 0.7f), new Vector2(w - 4f * s, h - cd * 0.5f), new Vector2(w - 3f * s, h - cd * 0.3f));
				b.AddCubicBezier(new Vector2(w, h - cd * 0.2f), new Vector2(w - cd * 0.5f, h), new Vector2(w - cd, h));

				b.AddLine(cd, h);

				// Bottom-Left: concave arc
				b.AddArc(new Vector2(0, h - cd), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(0, cd);

				// Top-Left: concave arc
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Sawtooth Triangle: Left concave; right sharp triangular teeth.</summary>
		private static void BuildSawtoothTriangle(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float concaveDepth = Math.Min(12f * s, w * 0.15f);
				float toothBase = 16f * s;
				int count = Math.Max(1, (int)Math.Floor(h / toothBase));
				float segH = h / count;
				float toothAmp = Math.Min(10f * s, w * 0.1f);
				float rightBase = w - toothAmp;

				b.BeginFigure(concaveDepth, 0);
				b.AddLine(rightBase, 0);
				for (int i = 0; i < count; i++)
				{
					float yStart = i * segH;
					b.AddLine(w, yStart + segH * 0.5f);
					b.AddLine(rightBase, yStart + segH);
				}
				b.AddLine(concaveDepth, h);
				b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				float toothAmp = Math.Min(6f * s, cd * 0.4f);

				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right: 2 small teeth in corner
				b.AddLine(w - toothAmp, cd * 0.2f);
				b.AddLine(w, cd * 0.5f);
				b.AddLine(w - toothAmp, cd * 0.8f);
				b.AddLine(w, cd);

				// Straight right border
				b.AddLine(w, h - cd);

				// Bottom-Right: 2 small teeth in corner
				b.AddLine(w - toothAmp, h - cd * 0.8f);
				b.AddLine(w, h - cd * 0.5f);
				b.AddLine(w - toothAmp, h - cd * 0.2f);
				b.AddLine(w - cd, h);

				b.AddLine(cd, h);

				// Bottom-Left: concave arc
				b.AddArc(new Vector2(0, h - cd), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(0, cd);

				// Top-Left: concave arc
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Sinusoidal Invert: S-curves that invert at the midpoint.</summary>
		private static void BuildSinusoidalInvert(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float amp = Math.Min(10f * s, w * 0.1f);
				float leftBase = amp;
				float rightBase = w - amp;

				b.BeginFigure(leftBase, 0);
				b.AddLine(rightBase, 0);
				b.AddCubicBezier(new Vector2(rightBase - amp, h * 0.25f), new Vector2(rightBase + amp, h * 0.75f), new Vector2(rightBase, h));
				b.AddLine(leftBase, h);
				b.AddCubicBezier(new Vector2(leftBase - amp, h * 0.75f), new Vector2(leftBase + amp, h * 0.25f), new Vector2(leftBase, 0));
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right S-curve
				b.AddCubicBezier(new Vector2(w - cd * 0.5f, 0), new Vector2(w, cd * 0.5f), new Vector2(w, cd));

				b.AddLine(w, h - cd);

				// Bottom-Right S-curve
				b.AddCubicBezier(new Vector2(w, h - cd * 0.5f), new Vector2(w - cd * 0.5f, h), new Vector2(w - cd, h));

				b.AddLine(cd, h);

				// Bottom-Left S-curve
				b.AddCubicBezier(new Vector2(cd * 0.5f, h), new Vector2(0, h - cd * 0.5f), new Vector2(0, h - cd));

				b.AddLine(0, cd);

				// Top-Left S-curve
				b.AddCubicBezier(new Vector2(0, cd * 0.5f), new Vector2(cd * 0.5f, 0), new Vector2(cd, 0));

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Bi-Concave Hourglass: Both sides curved deeply inward.</summary>
		private static void BuildBiConcave(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float depth = Math.Min(12f * s, w * 0.12f);
				b.BeginFigure(0, 0);
				b.AddLine(w, 0);
				b.AddCubicBezier(new Vector2(w - depth, h * 0.25f), new Vector2(w - depth, h * 0.75f), new Vector2(w, h));
				b.AddLine(0, h);
				b.AddCubicBezier(new Vector2(depth, h * 0.75f), new Vector2(depth, h * 0.25f), new Vector2(0, 0));
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right concave
				b.AddArc(new Vector2(w, cd), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(w, h - cd);

				// Bottom-Right concave
				b.AddArc(new Vector2(w - cd, h), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(cd, h);

				// Bottom-Left concave
				b.AddArc(new Vector2(0, h - cd), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.AddLine(0, cd);

				// Top-Left concave
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Hybrid Rectangular: Curved top, sharp 90° bottom corners.</summary>
		private static void BuildHybridRectangular(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float r = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.5f));
				b.BeginFigure(0, r);
				b.AddArc(new Vector2(r, 0), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				b.AddLine(w - r, 0);
				b.AddArc(new Vector2(w, r), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				b.AddLine(w, h);
				b.AddLine(0, h);
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right rounded
				b.AddArc(new Vector2(w, cd), cd, cd, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				// Right edge to bottom corner (sharp 90°)
				b.AddLine(w, h);
				b.AddLine(0, h);

				// Left edge to top corner
				b.AddLine(0, cd);

				// Top-Left rounded
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Cyberpunk: 45° slanted / chamfered edges.</summary>
		private static void BuildCyberpunk(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float cut = Math.Min(12f * s, Math.Min(w * 0.2f, h * 0.4f));
				b.BeginFigure(cut, 0);
				b.AddLine(w - cut, 0);
				b.AddLine(w, cut);
				b.AddLine(w, h - cut);
				b.AddLine(w - cut, h);
				b.AddLine(cut, h);
				b.AddLine(0, h - cut);
				b.AddLine(0, cut);
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				b.BeginFigure(cd, 0);
				b.AddLine(w - cd, 0);

				// Top-Right chamfer
				b.AddLine(w, cd);

				b.AddLine(w, h - cd);

				// Bottom-Right chamfer
				b.AddLine(w - cd, h);

				b.AddLine(cd, h);

				// Bottom-Left chamfer
				b.AddLine(0, h - cd);

				b.AddLine(0, cd);

				// Top-Left chamfer
				b.AddLine(cd, 0);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Directional Pill: Left side rounded pill, right side crisp edge.</summary>
		private static void BuildDirectionalPill(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float r = Math.Min(h * 0.5f, w);
				b.BeginFigure(r, 0);
				b.AddLine(w, 0);
				b.AddLine(w, h);
				b.AddLine(r, h);
				b.AddArc(new Vector2(r, 0), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				float microR = Math.Min(4f * s, cd * 0.25f);

				b.BeginFigure(cd, 0);
				b.AddLine(w - microR, 0);

				// Top-Right: micro-rounded
				b.AddArc(new Vector2(w, microR), microR, microR, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				b.AddLine(w, h - microR);

				// Bottom-Right: micro-rounded
				b.AddArc(new Vector2(w - microR, h), microR, microR, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				b.AddLine(cd, h);

				// Bottom-Left: full rounded arc
				b.AddArc(new Vector2(0, h - cd), cd, cd, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				b.AddLine(0, cd);

				// Top-Left: full rounded arc
				b.AddArc(new Vector2(cd, 0), cd, cd, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Hexagonal Tech: Left notch inward, right hex projection outward.</summary>
		private static void BuildHexagonal(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			if (isSmall)
			{
				float notch = Math.Min(10f * s, w * 0.1f);
				b.BeginFigure(notch, 0);
				b.AddLine(w - notch, 0);
				b.AddLine(w, h * 0.5f);
				b.AddLine(w - notch, h);
				b.AddLine(notch, h);
				b.AddLine(0, h * 0.5f);
				b.EndFigure(CanvasFigureLoop.Closed);
			}
			else
			{
				float cd = Math.Min(16f * s, Math.Min(w * 0.25f, h * 0.25f));
				float hexCut = cd * 0.6f;

				b.BeginFigure(cd, 0);
				b.AddLine(w - hexCut, 0);

				// Top-Right hex projection chamfer
				b.AddLine(w, hexCut);

				b.AddLine(w, h - hexCut);

				// Bottom-Right hex projection chamfer
				b.AddLine(w - hexCut, h);

				b.AddLine(cd, h);

				// Bottom-Left inward notch
				b.AddLine(cd * 0.5f, h - cd * 0.5f);
				b.AddLine(0, h - cd);

				b.AddLine(0, cd);

				// Top-Left inward notch
				b.AddLine(cd * 0.5f, cd * 0.5f);
				b.AddLine(cd, 0);

				b.EndFigure(CanvasFigureLoop.Closed);
			}
		}

		/// <summary>Rounded rectangle.</summary>
		private static void BuildStandardRounded(CanvasPathBuilder b, float w, float h, float r)
		{
			r = Math.Min(r, Math.Min(w * 0.5f, h * 0.5f));

			b.BeginFigure(r, 0);
			b.AddLine(w - r, 0);
			b.AddArc(new Vector2(w, r), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.AddLine(w, h - r);
			b.AddArc(new Vector2(w - r, h), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.AddLine(r, h);
			b.AddArc(new Vector2(0, h - r), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.AddLine(0, r);
			b.AddArc(new Vector2(r, 0), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.EndFigure(CanvasFigureLoop.Closed);
		}
	}
}
