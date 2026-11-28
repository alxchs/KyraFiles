using ColorCode;
using KyraFiles.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace KyraFiles.App.UserControls.FilePreviews
{
	public sealed partial class CodePreview : UserControl
	{
		private CodePreviewViewModel ViewModel { get; set; }

		public CodePreview(CodePreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
		}

		private void RenderDocument()
		{
			if (codeView is not null)
			{
				codeView.Blocks?.Clear();
				lineNumbers.Text = BuildLineNumbers(ViewModel.LineCount);

				if (ViewModel.Item.FileExtension is ".css" or ".scss")
				{
					// Some complex CSS files can make ColorCode parsing very slow and freeze the preview pane.
					// Use our lightweight renderer for CSS/SCSS to keep preview responsive while preserving highlighting.
					CssPreviewRenderer.Render(codeView, ViewModel.TextValue ?? string.Empty, ActualTheme == ElementTheme.Dark);
					return;
				}

				var formatter = new RichTextBlockFormatter(ActualTheme == ElementTheme.Dark ? PreviewCodeStyles.Dark : PreviewCodeStyles.Light);
				formatter.FormatRichTextBlock(ViewModel.TextValue!, ViewModel.CodeLanguage!, codeView);
			}
		}

		private static string BuildLineNumbers(int count)
		{
			if (count <= 0)
				return string.Empty;

			var builder = new StringBuilder(count * 6);
			for (var i = 1; i <= count; i++)
			{
				if (i > 1)
					builder.Append('\n');
				builder.Append(i);
			}

			return builder.ToString();
		}

		private void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			RenderDocument();
		}

		private void UserControl_ActualThemeChanged(Microsoft.UI.Xaml.FrameworkElement sender, object args)
		{
			try
			{
				RenderDocument();
			}
			catch (Exception)
			{
			}
		}
	}
}
