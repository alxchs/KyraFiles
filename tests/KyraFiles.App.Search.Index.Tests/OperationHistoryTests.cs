// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.Shared.OperationHistory;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class OperationHistoryTests
	{
		private string _tempDir = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_tempDir = Path.Combine(
				Path.GetTempPath(),
				"agy",
				Guid.NewGuid().ToString("N"),
				"desenv",
				"utils",
				"KyraFiles",
				"KyraFiles",
				"F043Tests");
			Directory.CreateDirectory(_tempDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, recursive: true);
		}

		[TestMethod]
		public async Task T1_Store_NewestFirst_LimitDiscardsOldest_AndPersistsRoundTrip()
		{
			var jsonPath = Path.Combine(_tempDir, "operation-history.json");
			var store = new OperationHistoryStore(jsonPath, maxEntries: 3);

			var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);
			for (int i = 1; i <= 5; i++)
			{
				store.Add(new OperationHistoryEntry
				{
					Id = $"entry-{i}",
					TimestampUtc = t0.AddMinutes(i),
					Kind = OperationHistoryKind.Move,
					Result = OperationHistoryResult.Success,
					Reversible = true,
					Sources = [new OperationHistoryItemPath($@"C:\src\file{i}.txt", false)],
					Destinations = [new OperationHistoryItemPath($@"C:\dst\file{i}.txt", false)],
				});
			}

			await store.FlushAsync();

			// Controle negativo: limite 3 DEVE descartar as 2 mais antigas (entry-1 e entry-2)
			var snapshot = store.GetSnapshot();
			Assert.AreEqual(3, snapshot.Count);
			Assert.AreEqual("entry-5", snapshot[0].Id);
			Assert.AreEqual("entry-4", snapshot[1].Id);
			Assert.AreEqual("entry-3", snapshot[2].Id);

			// Recarregar de disco (AC-5)
			var reloaded = new OperationHistoryStore(jsonPath, maxEntries: 3);
			reloaded.Load();
			Assert.IsNull(reloaded.LastLoadError);
			var reloadedSnapshot = reloaded.GetSnapshot();
			Assert.AreEqual(3, reloadedSnapshot.Count);
			Assert.AreEqual("entry-5", reloadedSnapshot[0].Id);
			Assert.AreEqual("entry-3", reloadedSnapshot[2].Id);
		}

		[TestMethod]
		public void T1_Store_CorruptedJson_RecoversEmptyWithoutCrash()
		{
			var jsonPath = Path.Combine(_tempDir, "corrupted.json");
			File.WriteAllText(jsonPath, "{ this is not valid json !!!");

			var store = new OperationHistoryStore(jsonPath);
			store.Load();

			Assert.AreEqual(0, store.Count);
			Assert.IsNotNull(store.LastLoadError);
		}

		[TestMethod]
		public async Task T1_Store_PersistenceDisabled_DoesNotCreateFile()
		{
			var jsonPath = Path.Combine(_tempDir, "disabled.json");
			var store = new OperationHistoryStore(jsonPath, persistenceEnabled: false);
			store.Add(new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.Copy,
				Reversible = true,
				Sources = [new OperationHistoryItemPath(@"C:\a.txt", false)],
				Destinations = [new OperationHistoryItemPath(@"C:\b.txt", false)],
			});
			await store.FlushAsync();

			Assert.AreEqual(1, store.Count);
			Assert.IsFalse(File.Exists(jsonPath));
		}

		[TestMethod]
		public async Task T1_Store_ConcurrentWrites_8Threads_ThreadSafe()
		{
			var jsonPath = Path.Combine(_tempDir, "concurrent.json");
			var store = new OperationHistoryStore(jsonPath, maxEntries: 200);

			var tasks = Enumerable.Range(0, 8).Select(threadIndex => Task.Run(() =>
			{
				for (int i = 0; i < 25; i++)
				{
					store.Add(new OperationHistoryEntry
					{
						Kind = OperationHistoryKind.Rename,
						Reversible = true,
						Sources = [new OperationHistoryItemPath($@"C:\t{threadIndex}\old_{i}.txt", false)],
						Destinations = [new OperationHistoryItemPath($@"C:\t{threadIndex}\new_{i}.txt", false)],
					});
				}
			})).ToArray();

			await Task.WhenAll(tasks);
			await store.FlushAsync();

			Assert.AreEqual(200, store.Count);
			Assert.IsNull(store.LastSaveError);
		}

		[TestMethod]
		public void T1_Store_Filter500Entries_CompletesWithin300Ms()
		{
			var store = new OperationHistoryStore(maxEntries: 500, persistenceEnabled: false);
			for (int i = 0; i < 500; i++)
			{
				store.Add(new OperationHistoryEntry
				{
					Kind = (OperationHistoryKind)(i % 5),
					Reversible = true,
					Sources = [new OperationHistoryItemPath($@"C:\work\folder_{i % 20}\relatorio_{i}.pdf", false)],
					Destinations = [new OperationHistoryItemPath($@"D:\archive\relatorio_{i}.pdf", false)],
				});
			}

			var sw = Stopwatch.StartNew();
			var matches = store.Filter(OperationHistoryKind.Move, "relatorio_42");
			sw.Stop();

			Assert.IsTrue(matches.Count > 0);
			Assert.IsLessThan(300, sw.ElapsedMilliseconds, $"Filtro levou {sw.ElapsedMilliseconds} ms (limite 300 ms).");
		}

		[TestMethod]
		public void T2_Eligibility_DeletePermanent_IsNotReversible()
		{
			var entry = new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.DeletePermanent,
				Reversible = true, // store normalizes to false, evaluator also enforces
				Sources = [new OperationHistoryItemPath(Path.Combine(_tempDir, "gone.txt"), false)],
			};

			var result = UndoEligibility.Evaluate(entry);

			Assert.IsFalse(result.CanUndo);
			Assert.AreEqual(UndoEligibilityStatus.NotEligible, result.Status);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.Reason));
		}

		[TestMethod]
		public void T2_Eligibility_Move_DestinationDeleted_NegativeControl_NotEligibleAndDiskUntouched()
		{
			var srcPath = Path.Combine(_tempDir, "original.txt");
			var dstPath = Path.Combine(_tempDir, "moved.txt");
			// Destino foi apagado depois da operacao
			var entry = new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.Move,
				Reversible = true,
				Sources = [new OperationHistoryItemPath(srcPath, false)],
				Destinations = [new OperationHistoryItemPath(dstPath, false)],
			};

			var result = UndoEligibility.Evaluate(entry);

			Assert.IsFalse(result.CanUndo);
			Assert.AreEqual(dstPath, result.ConflictingPath);
			Assert.IsFalse(File.Exists(srcPath));
			Assert.IsFalse(File.Exists(dstPath));
		}

		[TestMethod]
		public void T2_Eligibility_Rename_OldNameOccupied_RequiresConfirmation()
		{
			var oldPath = Path.Combine(_tempDir, "antes.txt");
			var newPath = Path.Combine(_tempDir, "depois.txt");
			File.WriteAllText(oldPath, "novo arquivo ocupando o nome antigo");
			File.WriteAllText(newPath, "arquivo renomeado");

			var entry = new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.Rename,
				Reversible = true,
				Sources = [new OperationHistoryItemPath(oldPath, false)],
				Destinations = [new OperationHistoryItemPath(newPath, false)],
			};

			var result = UndoEligibility.Evaluate(entry);

			Assert.IsTrue(result.CanUndo);
			Assert.IsTrue(result.RequiresConfirmation);
			Assert.AreEqual(UndoEligibilityStatus.EligibleWithConfirmation, result.Status);
			Assert.AreEqual(oldPath, result.ConflictingPath);
		}

		[TestMethod]
		public void T2_Eligibility_MoveAndRename_CleanState_IsEligible()
		{
			var oldPath = Path.Combine(_tempDir, "livre.txt");
			var newPath = Path.Combine(_tempDir, "atual.txt");
			File.WriteAllText(newPath, "conteudo");

			var renameEntry = new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.Rename,
				Reversible = true,
				Sources = [new OperationHistoryItemPath(oldPath, false)],
				Destinations = [new OperationHistoryItemPath(newPath, false)],
			};

			var moveEntry = new OperationHistoryEntry
			{
				Kind = OperationHistoryKind.Move,
				Reversible = true,
				Sources = [new OperationHistoryItemPath(oldPath, false)],
				Destinations = [new OperationHistoryItemPath(newPath, false)],
			};

			Assert.AreEqual(UndoEligibilityStatus.Eligible, UndoEligibility.Evaluate(renameEntry).Status);
			Assert.AreEqual(UndoEligibilityStatus.Eligible, UndoEligibility.Evaluate(moveEntry).Status);
		}
	}
}
