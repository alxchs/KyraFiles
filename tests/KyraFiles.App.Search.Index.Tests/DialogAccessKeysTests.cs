// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	// F057 T1: chooses the accelerator letter of each dialog button/option (AC-5, FR-003)
	[TestClass]
	public sealed class DialogAccessKeysTests
	{
		private static string Letters(string?[] texts)
		{
			var idx = DialogAccessKeys.Assign(texts);
			return string.Concat(idx.Select((ix, i) => ix < 0 ? '-' : DialogAccessKeys.BaseLetter(texts[i]![ix])));
		}

		private static bool HasDuplicate(string letters)
		{
			var seen = letters.Where(c => c != '-').ToList();
			return seen.Count != seen.Distinct().Count();
		}

		[TestMethod]
		public void DeleteDialog_UsesDCP()
			=> Assert.AreEqual("DCP", Letters(["Delete", "Cancel", "Permanently delete"])); // AC-1

		[TestMethod]
		public void SamePrefix_GetsDifferentLetters()
		{
			var letters = Letters(["Create", "Cancel", "Copy"]);
			Assert.AreEqual('C', letters[0]);
			Assert.IsFalse(HasDuplicate(letters), letters);
			Assert.IsFalse(letters.Contains('-'), letters);
		}

		[TestMethod]
		public void AccentedLetters_UseTheBaseLetter()
			=> Assert.AreEqual("EC", Letters(["Éxito", "Cancelar"]));

		[TestMethod]
		public void NonLatinTexts_HaveNoShortcut()
			=> Assert.AreEqual("--", Letters(["削除", "キャンセル"])); // edge: no Latin letter

		[TestMethod]
		public void EmptyAndNullTexts_AreSkipped()
			=> Assert.AreEqual("-D-", Letters([null, "Delete", ""]));

		[TestMethod]
		public void WordStart_IsPreferredToInnerLetter()
		{
			// "Open" takes O; "Open folder" cannot take O, so it takes the word start F before inner letters
			Assert.AreEqual("OF", Letters(["Open", "Open folder"]));
		}

		[TestMethod]
		public void RealisticDialogs_NeverRepeatALetter_FR003()
		{
			string[][] dialogs =
			[
				["Delete", "Cancel", "Permanently delete"],
				["Yes", "No", "Cancel"],
				["Excluir", "Cancelar", "Excluir permanentemente"],
				["Eliminar", "Cancelar", "Eliminar permanentemente"],
				["Supprimer", "Annuler", "Supprimer définitivement"],
				["Löschen", "Abbrechen", "Endgültig löschen"],
				["Save", "Don't save", "Cancel"],
				["Replace", "Skip", "Rename", "Cancel", "Apply to all"],
				["Create", "Cancel", "Compress", "Copy", "Convert", "Clear"],
			];

			foreach (var texts in dialogs)
			{
				var letters = Letters(texts);
				Assert.IsFalse(HasDuplicate(letters), $"{string.Join('|', texts)} -> {letters}");
			}
		}

		[TestMethod]
		public void NegativeControl_TheDuplicateCheckDetectsADuplicate()
			=> Assert.IsTrue(HasDuplicate("DCD")); // the check itself must fail where it should
	}
}
