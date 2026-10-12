// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
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
		/// Sinusoidal S-Curve: Smooth continuous sine wave transitions.
		/// </summary>
		SinusoidalWave = 6,

		/// <summary>
		/// Origami Fold: Asymmetric folded corner geometry.
		/// </summary>
		OrigamiFold = 7,

		/// <summary>
		/// Boarding Pass: Symmetric side circular punch notches.
		/// </summary>
		BoardingPass = 8,

		/// <summary>
		/// Organic Leaf: Diagonal rounded pairs (top-left and bottom-right rounded, others straight).
		/// </summary>
		OrganicLeaf = 9,
	}
}

