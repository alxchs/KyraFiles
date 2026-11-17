// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Enums
{
	/// <summary>
	/// Represents the geometric shape styling model applied to tabs, controls, cards, and frame corners.
	/// </summary>
	public enum ControlShapeStyleKind
	{
		/// <summary>
		/// Fluent standard symmetrical rounded corners.
		/// </summary>
		FluentStandard = 0,

		/// <summary>
		/// Ticket Wave: Left concave curve, right with 3 convex wave ripples (drawing option 1).
		/// </summary>
		TicketWave = 1,

		/// <summary>
		/// Ribbon Dock: Left concave curve, right dual rounded lobes with inner notch (drawing option 2).
		/// </summary>
		RibbonDock = 2,

		/// <summary>
		/// Cyberpunk: Left 45° chamfer inward, right slanted 45° outward.
		/// </summary>
		CyberpunkTrapezoid = 3,

		/// <summary>
		/// Directional Pill: Left full convex curve, right crisp micro-rounded edge.
		/// </summary>
		DirectionalPill = 4,

		/// <summary>
		/// Hexagonal Honeycomb: Left inward 30° notch, right pointed outward hex projection.
		/// </summary>
		HexagonalTech = 5,

		/// <summary>
		/// Sinusoidal Invert: Left curve inverts at midpoint (convex to concave), right complementary wave.
		/// </summary>
		SinusoidalWave = 6,

		/// <summary>
		/// Triangular Sawtooth: Left concave curve, right with prominent triangular zig-zag sawtooth teeth.
		/// </summary>
		SawtoothTriangle = 7,

		/// <summary>
		/// Bi-Concave Hourglass: Both left and right curved deeply inward towards the center.
		/// </summary>
		BiConcaveHourglass = 8,

		/// <summary>
		/// Hybrid Tech: Top half curved, bottom half sharp rectangular 90° corner.
		/// </summary>
		HybridRectangular = 9,
	}
}
