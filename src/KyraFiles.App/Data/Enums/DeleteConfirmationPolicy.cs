// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Data.Enums
{
	public enum DeleteConfirmationPolicies : byte
	{
		/// <summary>
		/// Always ask to confirm.
		/// </summary>
		Always,

		/// <summary>
		/// Permanent deletion only.
		/// </summary>
		PermanentOnly,

		/// <summary>
		/// Never ask to confirm.
		/// </summary>
		Never,
	}
}
