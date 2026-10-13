// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;

namespace Files.App.Helpers
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

		/// <summary>Left concave; right with 3 convex wave ripples.</summary>
		private static void BuildTicketWave(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float concaveDepth = Math.Min(16f * s, w * 0.15f);
			float waveAmp = Math.Min(10f * s, w * 0.1f);
			float rightBase = w - waveAmp;

			b.BeginFigure(concaveDepth, 0);
			b.AddLine(rightBase, 0);

			float hSeg = h / 3f;
			for (int i = 0; i < 3; i++)
			{
				float y0 = i * hSeg;
				b.AddCubicBezier(
					new Vector2(rightBase + waveAmp, y0 + hSeg * 0.25f),
					new Vector2(rightBase + waveAmp, y0 + hSeg * 0.5f),
					new Vector2(rightBase, y0 + hSeg));
			}

			b.AddLine(concaveDepth, h);
			b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Left concave; right with 2 lobes and a central notch.</summary>
		private static void BuildRibbonDock(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float concaveDepth = Math.Min(16f * s, w * 0.15f);
			float lobeDepth = Math.Min(12f * s, w * 0.1f);
			float midNotch = Math.Min(14f * s, w * 0.12f);
			float rightBase = w - lobeDepth;

			b.BeginFigure(concaveDepth, 0);
			b.AddLine(rightBase, 0);
			b.AddCubicBezier(new Vector2(w, h * 0.15f), new Vector2(w, h * 0.35f), new Vector2(w - midNotch, h * 0.5f));
			b.AddCubicBezier(new Vector2(w, h * 0.65f), new Vector2(w, h * 0.85f), new Vector2(rightBase, h));
			b.AddLine(concaveDepth, h);
			b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Left concave; right with sharp triangular zig-zag teeth.</summary>
		private static void BuildSawtoothTriangle(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float concaveDepth = Math.Min(16f * s, w * 0.15f);
			float toothAmp = Math.Min(12f * s, w * 0.1f);
			float rightBase = w - toothAmp;

			b.BeginFigure(concaveDepth, 0);
			b.AddLine(rightBase, 0);

			const int toothCount = 4;
			float segH = h / toothCount;
			for (int i = 0; i < toothCount; i++)
			{
				float yStart = i * segH;
				b.AddLine(w, yStart + segH * 0.5f);
				b.AddLine(rightBase, yStart + segH);
			}

			b.AddLine(concaveDepth, h);
			b.AddCubicBezier(new Vector2(0, h * 0.75f), new Vector2(0, h * 0.25f), new Vector2(concaveDepth, 0));
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Both sides are S-curves that invert at the midpoint.</summary>
		private static void BuildSinusoidalInvert(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float amp = Math.Min(12f * s, w * 0.1f);
			float leftBase = amp;
			float rightBase = w - amp;

			b.BeginFigure(leftBase, 0);
			b.AddLine(rightBase, 0);
			b.AddCubicBezier(new Vector2(rightBase - amp, h * 0.25f), new Vector2(rightBase + amp, h * 0.75f), new Vector2(rightBase, h));
			b.AddLine(leftBase, h);
			b.AddCubicBezier(new Vector2(leftBase - amp, h * 0.75f), new Vector2(leftBase + amp, h * 0.25f), new Vector2(leftBase, 0));
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Both sides curve inward (hourglass).</summary>
		private static void BuildBiConcave(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float depth = Math.Min(14f * s, w * 0.12f);

			b.BeginFigure(0, 0);
			b.AddLine(w, 0);
			b.AddCubicBezier(new Vector2(w - depth, h * 0.25f), new Vector2(w - depth, h * 0.75f), new Vector2(w, h));
			b.AddLine(0, h);
			b.AddCubicBezier(new Vector2(depth, h * 0.75f), new Vector2(depth, h * 0.25f), new Vector2(0, 0));
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Curved top, sharp 90° bottom corners.</summary>
		private static void BuildHybridRectangular(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float r = Math.Min(18f * s, Math.Min(w * 0.25f, h * 0.5f));

			b.BeginFigure(0, r);
			b.AddCubicBezier(new Vector2(r * 0.5f, r * 0.5f), new Vector2(r * 0.5f, 0), new Vector2(r, 0));
			b.AddLine(w - r, 0);
			b.AddArc(new Vector2(w, r), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.AddLine(w, h);
			b.AddLine(0, h);
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>45° slanted sides.</summary>
		private static void BuildCyberpunk(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float cut = Math.Min(14f * s, Math.Min(w * 0.2f, h * 0.4f));

			b.BeginFigure(cut, 0);
			b.AddLine(w, 0);
			b.AddLine(w - cut, h);
			b.AddLine(0, h);
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Left side a full half-circle, right side straight.</summary>
		private static void BuildDirectionalPill(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float r = Math.Min(h * 0.5f, w);

			b.BeginFigure(r, 0);
			b.AddLine(w, 0);
			b.AddLine(w, h);
			b.AddLine(r, h);
			b.AddArc(new Vector2(r, 0), r, r, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			b.EndFigure(CanvasFigureLoop.Closed);
		}

		/// <summary>Hexagon with pointed left and right edges.</summary>
		private static void BuildHexagonal(CanvasPathBuilder b, float w, float h, float s, bool isSmall)
		{
			float notch = Math.Min(12f * s, w * 0.1f);

			b.BeginFigure(notch, 0);
			b.AddLine(w - notch, 0);
			b.AddLine(w, h * 0.5f);
			b.AddLine(w - notch, h);
			b.AddLine(notch, h);
			b.AddLine(0, h * 0.5f);
			b.EndFigure(CanvasFigureLoop.Closed);
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
