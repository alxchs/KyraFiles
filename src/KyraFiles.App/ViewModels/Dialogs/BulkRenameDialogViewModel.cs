// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using Windows.Storage;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class BulkRenameDialogViewModel : ObservableObject
	{
		private readonly IContentPageContext _context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly List<RenameItem> _sourceItems = new();
		private CancellationTokenSource? _previewCts;

		// Preview collection
		public ObservableCollection<RenamePreviewItem> PreviewItems { get; } = new();

		// State & validation
		private bool _canApply;
		public bool CanApply
		{
			get => _canApply;
			private set => SetProperty(ref _canApply, value);
		}

		private bool _hasConflicts;
		public bool HasConflicts
		{
			get => _hasConflicts;
			private set => SetProperty(ref _hasConflicts, value);
		}

		private string _conflictMessage = string.Empty;
		public string ConflictMessage
		{
			get => _conflictMessage;
			private set => SetProperty(ref _conflictMessage, value);
		}

		private int _changedCount;
		public int ChangedCount
		{
			get => _changedCount;
			private set => SetProperty(ref _changedCount, value);
		}

		public int TotalCount => _sourceItems.Count;

		// Tab selection: 0 = Regras, 1 = Edição como Texto
		private int _selectedTabIndex;
		public int SelectedTabIndex
		{
			get => _selectedTabIndex;
			set
			{
				if (SetProperty(ref _selectedTabIndex, value))
				{
					if (value == 1)
						InitializeTextMode();
					else
						QueuePreviewUpdate();
				}
			}
		}

		// --- Regras ---

		// Localizar e Substituir
		private string _findText = string.Empty;
		public string FindText
		{
			get => _findText;
			set
			{
				if (SetProperty(ref _findText, value))
					QueuePreviewUpdate();
			}
		}

		private string _replaceText = string.Empty;
		public string ReplaceText
		{
			get => _replaceText;
			set
			{
				if (SetProperty(ref _replaceText, value))
					QueuePreviewUpdate();
			}
		}

		private bool _useRegex;
		public bool UseRegex
		{
			get => _useRegex;
			set
			{
				if (SetProperty(ref _useRegex, value))
					QueuePreviewUpdate();
			}
		}

		private bool _matchCase;
		public bool MatchCase
		{
			get => _matchCase;
			set
			{
				if (SetProperty(ref _matchCase, value))
					QueuePreviewUpdate();
			}
		}

		private bool _applyToExtension;
		public bool ApplyToExtension
		{
			get => _applyToExtension;
			set
			{
				if (SetProperty(ref _applyToExtension, value))
					QueuePreviewUpdate();
			}
		}

		// Numeração
		private bool _enableNumbering;
		public bool EnableNumbering
		{
			get => _enableNumbering;
			set
			{
				if (SetProperty(ref _enableNumbering, value))
					QueuePreviewUpdate();
			}
		}

		private int _numberingPosition = 0; // 0 = Suffix, 1 = Prefix, 2 = Replace
		public int NumberingPosition
		{
			get => _numberingPosition;
			set
			{
				if (SetProperty(ref _numberingPosition, value))
					QueuePreviewUpdate();
			}
		}

		private int _numberingStart = 1;
		public int NumberingStart
		{
			get => _numberingStart;
			set
			{
				if (SetProperty(ref _numberingStart, value))
					QueuePreviewUpdate();
			}
		}

		private int _numberingPadding = 3;
		public int NumberingPadding
		{
			get => _numberingPadding;
			set
			{
				if (SetProperty(ref _numberingPadding, value))
					QueuePreviewUpdate();
			}
		}

		private string _numberingSeparator = "_";
		public string NumberingSeparator
		{
			get => _numberingSeparator;
			set
			{
				if (SetProperty(ref _numberingSeparator, value))
					QueuePreviewUpdate();
			}
		}

		// Transformação de Caixa
		private int _selectedCaseTransform = 0; // 0=None, 1=Uppercase, 2=Lowercase, 3=TitleCase, 4=CapitalizeFirst
		public int SelectedCaseTransform
		{
			get => _selectedCaseTransform;
			set
			{
				if (SetProperty(ref _selectedCaseTransform, value))
					QueuePreviewUpdate();
			}
		}

		// Troca de Espaços
		private bool _enableReplaceSpaces;
		public bool EnableReplaceSpaces
		{
			get => _enableReplaceSpaces;
			set
			{
				if (SetProperty(ref _enableReplaceSpaces, value))
					QueuePreviewUpdate();
			}
		}

		private string _spaceReplacementChar = "_";
		public string SpaceReplacementChar
		{
			get => _spaceReplacementChar;
			set
			{
				if (SetProperty(ref _spaceReplacementChar, value))
					QueuePreviewUpdate();
			}
		}

		// Data
		private bool _enableDate;
		public bool EnableDate
		{
			get => _enableDate;
			set
			{
				if (SetProperty(ref _enableDate, value))
					QueuePreviewUpdate();
			}
		}

		private int _datePosition = 0; // 0 = Prefix, 1 = Suffix
		public int DatePosition
		{
			get => _datePosition;
			set
			{
				if (SetProperty(ref _datePosition, value))
					QueuePreviewUpdate();
			}
		}

		private string _dateFormat = "yyyy-MM-dd";
		public string DateFormat
		{
			get => _dateFormat;
			set
			{
				if (SetProperty(ref _dateFormat, value))
					QueuePreviewUpdate();
			}
		}

		// Edição como Texto (US4)
		private string _textModeContent = string.Empty;
		public string TextModeContent
		{
			get => _textModeContent;
			set
			{
				if (SetProperty(ref _textModeContent, value))
					ApplyTextModeChanges();
			}
		}

		// Commands
		public IAsyncRelayCommand CommitRenameCommand { get; }

		public BulkRenameDialogViewModel()
		{
			CommitRenameCommand = new AsyncRelayCommand(DoCommitRenameAsync);

			if (_context.SelectedItems is not null)
			{
				for (int i = 0; i < _context.SelectedItems.Count; i++)
				{
					var item = _context.SelectedItems[i];
					bool isDir = item.PrimaryItemAttribute == StorageItemTypes.Folder;
					_sourceItems.Add(RenameItem.FromPath(
						item.GetRequiredPath(),
						i,
						isDir,
						item.ItemDateCreatedReal,
						item.ItemDateModifiedReal));
				}
			}

			QueuePreviewUpdate();
		}

		public void QueuePreviewUpdate()
		{
			if (_selectedTabIndex == 1)
				return;

			_previewCts?.Cancel();
			_previewCts = new CancellationTokenSource();
			var token = _previewCts.Token;

			var rules = BuildActiveRules();
			var snapshot = _sourceItems.ToList();

			Task.Run(() =>
			{
				if (token.IsCancellationRequested)
					return;

				var preview = RenameRuleEngine.GeneratePreview(snapshot, rules, path => File.Exists(path) || Directory.Exists(path));

				if (token.IsCancellationRequested)
					return;

				MainWindow.Instance?.DispatcherQueue?.TryEnqueue(() =>
				{
					if (token.IsCancellationRequested)
						return;

					PreviewItems.Clear();
					foreach (var item in preview)
						PreviewItems.Add(item);

					ChangedCount = preview.Count(p => p.IsChanged);
					HasConflicts = preview.Any(p => p.HasConflict);
					CanApply = ChangedCount > 0 && !HasConflicts;

					var firstConflict = preview.FirstOrDefault(p => p.HasConflict);
					ConflictMessage = firstConflict?.ConflictReason ?? string.Empty;
				});
			}, token);
		}

		private List<IRenameRule> BuildActiveRules()
		{
			var rules = new List<IRenameRule>();

			if (!string.IsNullOrEmpty(FindText))
			{
				rules.Add(new FindReplaceRule
				{
					Find = FindText,
					Replace = ReplaceText,
					UseRegex = UseRegex,
					MatchCase = MatchCase,
					ApplyToExtension = ApplyToExtension
				});
			}

			if (EnableReplaceSpaces)
			{
				rules.Add(new ReplaceSpacesRule
				{
					Replacement = SpaceReplacementChar
				});
			}

			if (SelectedCaseTransform > 0)
			{
				rules.Add(new CaseTransformRule
				{
					Transform = (CaseTransformType)SelectedCaseTransform
				});
			}

			if (EnableDate)
			{
				rules.Add(new DateRule
				{
					Position = DatePosition == 1 ? InsertPosition.Suffix : InsertPosition.Prefix,
					Format = DateFormat
				});
			}

			if (EnableNumbering)
			{
				rules.Add(new NumberingRule
				{
					Position = NumberingPosition switch
					{
						1 => InsertPosition.Prefix,
						2 => InsertPosition.Replace,
						_ => InsertPosition.Suffix
					},
					StartIndex = NumberingStart,
					PaddingDigits = NumberingPadding,
					Separator = NumberingSeparator
				});
			}

			return rules;
		}

		private void InitializeTextMode()
		{
			var lines = PreviewItems.Count > 0
				? PreviewItems.Select(p => p.NewFullName)
				: _sourceItems.Select(s => s.OriginalFullName);

			_textModeContent = string.Join(Environment.NewLine, lines);
			OnPropertyChanged(nameof(TextModeContent));
		}

		private void ApplyTextModeChanges()
		{
			if (string.IsNullOrWhiteSpace(_textModeContent))
			{
				HasConflicts = true;
				CanApply = false;
				ConflictMessage = "A lista de nomes não pode ser vazia.";
				return;
			}

			var lines = _textModeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			if (lines.Length != _sourceItems.Count)
			{
				HasConflicts = true;
				CanApply = false;
				ConflictMessage = $"O número de linhas ({lines.Length}) deve coincidir exatamente com os arquivos selecionados ({_sourceItems.Count}).";
				return;
			}

			var previews = new List<RenamePreviewItem>(_sourceItems.Count);
			var collisionCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			bool hasConflict = false;
			string? conflictReason = null;

			for (int i = 0; i < _sourceItems.Count; i++)
			{
				var source = _sourceItems[i];
				string newName = lines[i].Trim();
				string newPath = Path.Combine(source.DirectoryPath, newName);

				var item = new RenamePreviewItem
				{
					Source = source,
					NewFullName = newName,
					NewPath = newPath
				};

				if (string.IsNullOrWhiteSpace(newName))
				{
					item.HasConflict = true;
					item.ConflictReason = "Nome de arquivo não pode ser vazio.";
				}
				else if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				{
					item.HasConflict = true;
					item.ConflictReason = "O nome contém caracteres proibidos.";
				}
				else if (newName.EndsWith('.') || newName.EndsWith(' '))
				{
					item.HasConflict = true;
					item.ConflictReason = "O nome não pode terminar com ponto ou espaço.";
				}
				else if (!collisionCheck.Add(newPath))
				{
					item.HasConflict = true;
					item.ConflictReason = "Conflito: múltiplos arquivos terão o mesmo nome.";
				}

				if (item.HasConflict && !hasConflict)
				{
					hasConflict = true;
					conflictReason = item.ConflictReason;
				}

				previews.Add(item);
			}

			PreviewItems.Clear();
			foreach (var p in previews)
				PreviewItems.Add(p);

			ChangedCount = previews.Count(p => p.IsChanged);
			HasConflicts = hasConflict;
			ConflictMessage = conflictReason ?? string.Empty;
			CanApply = ChangedCount > 0 && !HasConflicts;
		}

		private async Task DoCommitRenameAsync()
		{
			if (!CanApply || PreviewItems.Count == 0)
				return;

			var items = PreviewItems.ToList();
			await BulkRenameExecutionService.ExecuteAsync(items);
		}
	}
}
