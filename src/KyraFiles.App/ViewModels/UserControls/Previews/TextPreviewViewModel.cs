// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.UserControls.FilePreviews;
using KyraFiles.App.ViewModels.Properties;

namespace KyraFiles.App.ViewModels.Previews
{
	public sealed partial class TextPreviewViewModel : BasePreviewModel
	{
		private string? textValue;
		public string? TextValue
		{
			get => textValue;
			private set => SetProperty(ref textValue, value);
		}

		public TextPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();

			try
			{
				var text = TextValue ?? await ReadFileAsTextAsync(PreviewFile);

				details.Add(GetFileProperty("PropertyLineCount", text.AsSpan().Count('\n') + 1));
				details.Add(GetFileProperty("PropertyWordCount", CountWords(text)));

				TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			return details;
		}

		public static async Task<TextPreview?> TryLoadAsTextAsync(ListedItem item)
		{
			string? extension = item.FileExtension?.ToLowerInvariant();
			if (ExcludedExtensions(extension) || item.FileSizeBytes is 0 or > Constants.PreviewPane.TryLoadAsTextSizeLimit)
				return null;

			try
			{
				item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath!);
				if (item.ItemFile is not { } itemFile)
					return null;

				var text = await ReadFileAsTextAsync(itemFile);
				bool isBinaryFile = text.Contains("\0\0\0\0", StringComparison.Ordinal);

				if (isBinaryFile)
					return null;

				var model = new TextPreviewViewModel(item) { TextValue = text };
				await model.LoadAsync();

				return new TextPreview(model);
			}
			catch
			{
				return null;
			}
		}

		// Same result as splitting on space and line feed without empty entries, without allocating the parts
		private static int CountWords(ReadOnlySpan<char> text)
		{
			var count = 0;
			var inWord = false;
			foreach (var c in text)
			{
				if (c is ' ' or '\n')
				{
					inWord = false;
				}
				else if (!inWord)
				{
					inWord = true;
					count++;
				}
			}

			return count;
		}

		private static bool ExcludedExtensions(string? extension)
			=> extension is ".iso";
	}
}
