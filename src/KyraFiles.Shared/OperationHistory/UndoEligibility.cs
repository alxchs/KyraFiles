// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.IO;

namespace KyraFiles.Shared.OperationHistory
{
	public enum UndoEligibilityStatus
	{
		Eligible,
		EligibleWithConfirmation,
		NotEligible,
	}

	public sealed record UndoEligibilityResult(
		UndoEligibilityStatus Status,
		string? Reason = null,
		string? ConflictingPath = null)
	{
		public bool CanUndo => Status != UndoEligibilityStatus.NotEligible;

		public bool RequiresConfirmation => Status == UndoEligibilityStatus.EligibleWithConfirmation;
	}

	public static class UndoEligibility
	{
		public static UndoEligibilityResult Evaluate(
			OperationHistoryEntry entry,
			Func<string, bool>? pathExistsOverride = null)
		{
			ArgumentNullException.ThrowIfNull(entry);

			var exists = pathExistsOverride ?? DefaultPathExists;

			if (entry.IsUndone)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Esta operacao ja foi desfeita.");

			if (entry.Result == OperationHistoryResult.Failed)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Operacao falhou e nao ha alteracoes para desfazer.");

			if (entry.Kind == OperationHistoryKind.DeletePermanent || !entry.Reversible)
				return new UndoEligibilityResult(
					UndoEligibilityStatus.NotEligible,
					entry.IrreversibleReason ?? "Exclusao definitiva nao pode ser desfeita.");

			return entry.Kind switch
			{
				OperationHistoryKind.Move => EvaluateMove(entry, exists),
				OperationHistoryKind.Rename => EvaluateRename(entry, exists),
				OperationHistoryKind.Recycle => EvaluateRecycle(entry, exists),
				OperationHistoryKind.CreateNew or OperationHistoryKind.CreateLink => EvaluateCreate(entry, exists),
				OperationHistoryKind.Copy => EvaluateCopy(entry, exists),
				OperationHistoryKind.Restore => EvaluateRestore(entry, exists),
				_ => new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Tipo de operacao nao suportado para desfazer."),
			};
		}

		private static UndoEligibilityResult EvaluateMove(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Destinations.Count == 0 || entry.Sources.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Caminhos de origem ou destino indisponiveis.");

			foreach (var dest in entry.Destinations)
			{
				if (string.IsNullOrWhiteSpace(dest.Path) || !exists(dest.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Destino nao encontrado no disco: {dest.Path}",
						dest.Path);
			}

			foreach (var src in entry.Sources)
			{
				if (!string.IsNullOrWhiteSpace(src.Path) && exists(src.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.EligibleWithConfirmation,
						$"Ja existe um item no caminho original: {src.Path}",
						src.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static UndoEligibilityResult EvaluateRename(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Destinations.Count == 0 || entry.Sources.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Caminhos do renomeio indisponiveis.");

			foreach (var dest in entry.Destinations)
			{
				if (string.IsNullOrWhiteSpace(dest.Path) || !exists(dest.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Item renomeado nao encontrado no disco: {dest.Path}",
						dest.Path);
			}

			foreach (var src in entry.Sources)
			{
				if (!string.IsNullOrWhiteSpace(src.Path) && exists(src.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.EligibleWithConfirmation,
						$"O nome anterior ja esta ocupado: {src.Path}",
						src.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static UndoEligibilityResult EvaluateRecycle(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Destinations.Count == 0 || entry.Sources.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Referencia do item na Lixeira indisponivel.");

			foreach (var recycled in entry.Destinations)
			{
				if (string.IsNullOrWhiteSpace(recycled.Path) || !exists(recycled.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Item nao esta mais disponivel na Lixeira: {recycled.Path}",
						recycled.Path);
			}

			foreach (var original in entry.Sources)
			{
				if (!string.IsNullOrWhiteSpace(original.Path) && exists(original.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.EligibleWithConfirmation,
						$"Ja existe um item no local original: {original.Path}",
						original.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static UndoEligibilityResult EvaluateCreate(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Sources.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Caminho do item criado indisponivel.");

			foreach (var created in entry.Sources)
			{
				if (string.IsNullOrWhiteSpace(created.Path) || !exists(created.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Item criado nao existe mais no disco: {created.Path}",
						created.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static UndoEligibilityResult EvaluateCopy(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Destinations.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Caminho da copia indisponivel.");

			foreach (var copied in entry.Destinations)
			{
				if (string.IsNullOrWhiteSpace(copied.Path) || !exists(copied.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Copia nao encontrada no destino: {copied.Path}",
						copied.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static UndoEligibilityResult EvaluateRestore(OperationHistoryEntry entry, Func<string, bool> exists)
		{
			if (entry.Destinations.Count == 0)
				return new UndoEligibilityResult(UndoEligibilityStatus.NotEligible, "Caminho restaurado indisponivel.");

			foreach (var restored in entry.Destinations)
			{
				if (string.IsNullOrWhiteSpace(restored.Path) || !exists(restored.Path))
					return new UndoEligibilityResult(
						UndoEligibilityStatus.NotEligible,
						$"Item restaurado nao encontrado: {restored.Path}",
						restored.Path);
			}

			return new UndoEligibilityResult(UndoEligibilityStatus.Eligible);
		}

		private static bool DefaultPathExists(string path)
			=> File.Exists(path) || Directory.Exists(path);
	}
}
