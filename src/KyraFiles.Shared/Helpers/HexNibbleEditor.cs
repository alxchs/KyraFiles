// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Manages 2-nibble keystroke entry for hex editing (F060 M2 / M2-AC-1..3).
	/// Typing 2 hexadecimal digits (0-9, A-F) produces a completed byte replacement.
	/// Invalid characters are ignored.
	/// Backspace or Esc cancels the pending nibble in progress.
	/// </summary>
	public sealed class HexNibbleEditor
	{
		/// <summary>
		/// Gets whether a high nibble is currently pending completion.
		/// </summary>
		public bool HasPendingNibble => PendingHighNibble.HasValue;

		/// <summary>
		/// Gets the high nibble value (0..15) if pending, or null.
		/// </summary>
		public int? PendingHighNibble { get; private set; }

		/// <summary>
		/// Gets the pending high nibble character ('0'..'9', 'A'..'F') if pending, or null.
		/// </summary>
		public char? PendingChar { get; private set; }

		/// <summary>
		/// Checks whether a character is a valid hexadecimal digit (0-9, a-f, A-F).
		/// </summary>
		public static bool IsHexDigit(char c)
		{
			return (c >= '0' && c <= '9') ||
				   (c >= 'a' && c <= 'f') ||
				   (c >= 'A' && c <= 'F');
		}

		/// <summary>
		/// Converts a valid hex digit character to its numeric value (0..15).
		/// Throws ArgumentException if the character is not a hex digit.
		/// </summary>
		public static int HexToNibble(char c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;
			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;

			throw new ArgumentException($"'{c}' is not a valid hexadecimal character.", nameof(c));
		}

		/// <summary>
		/// Processes a keystroke.
		/// - Backspace ('\b') or Escape ('\x1b'): cancels in-progress editing; returns true if an edit was cancelled.
		/// - Non-hex characters: ignored; returns false.
		/// - First hex character: stored as pending high nibble; completedByte is null; returns true.
		/// - Second hex character: combines with high nibble; completedByte is set; state is reset; returns true.
		/// </summary>
		/// <param name="keyChar">The input character.</param>
		/// <param name="completedByte">Receives the completed byte when the 2nd nibble is entered; otherwise null.</param>
		/// <returns>True if the key was handled (valid hex or cancellation); false if ignored.</returns>
		public bool ProcessKey(char keyChar, out byte? completedByte)
		{
			completedByte = null;

			// Backspace ('\b') or Esc ('\x1b') cancels in-progress edit
			if (keyChar == '\b' || keyChar == '\x1b')
			{
				if (HasPendingNibble)
				{
					Cancel();
					return true;
				}

				return false;
			}

			// Invalid hex digit: ignore
			if (!IsHexDigit(keyChar))
			{
				return false;
			}

			int nibble = HexToNibble(keyChar);

			if (PendingHighNibble is not int high)
			{
				// First nibble (high)
				PendingHighNibble = nibble;
				PendingChar = char.ToUpperInvariant(keyChar);
				return true;
			}
			else
			{
				// Second nibble (low) completes the byte
				byte result = (byte)((high << 4) | nibble);
				Reset();
				completedByte = result;
				return true;
			}
		}

		/// <summary>
		/// Cancels any pending in-progress nibble entry.
		/// </summary>
		public void Cancel()
		{
			PendingHighNibble = null;
			PendingChar = null;
		}

		/// <summary>
		/// Resets the editor state.
		/// </summary>
		public void Reset()
		{
			Cancel();
		}
	}
}
