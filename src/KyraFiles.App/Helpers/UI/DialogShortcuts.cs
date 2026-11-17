// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.CompilerServices;
using WinRT;
using Windows.System;
using Windows.UI.Core;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// KyraFiles (F057): underlined accelerator letter on the buttons and options of a <see cref="ContentDialog"/>.
	/// Pressing the letter does what a click does; it never fires while a text box, combo box or similar is focused.
	/// </summary>
	public static class DialogShortcuts
	{
		private sealed class State
		{
			public readonly List<Entry> Entries = [];
		}

		private sealed record Entry(FrameworkElement Control, char Letter);

		private static readonly ConditionalWeakTable<ContentDialog, State> attached = new();
		private static readonly ConditionalWeakTable<ContentControl, string> originalTexts = new();

		/// <summary>Idempotent: calling it twice on the same dialog does nothing the second time.</summary>
		public static void Attach(ContentDialog dialog)
		{
			if (attached.TryGetValue(dialog, out _))
				return;

			attached.Add(dialog, new State());

			dialog.Opened += (_, _) => Apply(dialog);
			dialog.PreviewKeyDown += Dialog_PreviewKeyDown;
			dialog.RegisterPropertyChangedCallback(ContentDialog.PrimaryButtonTextProperty, (_, _) => Apply(dialog));
			dialog.RegisterPropertyChangedCallback(ContentDialog.SecondaryButtonTextProperty, (_, _) => Apply(dialog));
			dialog.RegisterPropertyChangedCallback(ContentDialog.CloseButtonTextProperty, (_, _) => Apply(dialog));
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		[DynamicWindowsRuntimeCast(typeof(DependencyObject))]
		private static void Apply(ContentDialog dialog)
		{
			if (!attached.TryGetValue(dialog, out var state))
				return;

			try
			{
				state.Entries.Clear();

				var controls = new List<(ContentControl Control, string Text)>();

				void AddButton(string name, string? text)
				{
					if (!string.IsNullOrEmpty(text) && FindByName(dialog, name) is Button button && button.Visibility == Visibility.Visible)
						controls.Add((button, text));
				}

				AddButton("PrimaryButton", dialog.PrimaryButtonText);
				AddButton("SecondaryButton", dialog.SecondaryButtonText);
				AddButton("CloseButton", dialog.CloseButtonText);

				if (dialog.Content is DependencyObject content)
					CollectOptions(content, controls);

				var indexes = DialogAccessKeys.Assign(controls.Select(c => c.Text).ToList());

				for (int i = 0; i < controls.Count; i++)
				{
					if (indexes[i] < 0)
						continue;

					var (control, text) = controls[i];
					var letter = DialogAccessKeys.BaseLetter(text[indexes[i]]);
					SetUnderlinedContent(control, text, indexes[i]);
					state.Entries.Add(new Entry(control, letter));
				}
			}
			catch (Exception ex)
			{
				// The accelerators are a convenience: a failure here must never break the dialog
				App.Logger?.LogWarning(ex, "Dialog accelerators could not be applied.");
			}
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		[DynamicWindowsRuntimeCast(typeof(RadioButton))]
		[DynamicWindowsRuntimeCast(typeof(ToggleButton))]
		private static void CollectOptions(DependencyObject root, List<(ContentControl Control, string Text)> controls)
		{
			int count = VisualTreeHelper.GetChildrenCount(root);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);

				if (child is CheckBox or RadioButton)
				{
					var option = (ToggleButton)child;
					var text = originalTexts.TryGetValue(option, out var saved) ? saved : option.Content as string;
					if (!string.IsNullOrEmpty(text) && option.Visibility == Visibility.Visible)
						controls.Add((option, text));
				}
				else
				{
					CollectOptions(child, controls);
				}
			}
		}

		[DynamicWindowsRuntimeCast(typeof(ToggleButton))]
		private static void SetUnderlinedContent(ContentControl control, string text, int index)
		{
			if (control is ToggleButton && !originalTexts.TryGetValue(control, out _))
				originalTexts.Add(control, text);

			var block = new TextBlock();
			if (index > 0)
				block.Inlines.Add(new Run { Text = text[..index] });

			var underline = new Underline();
			underline.Inlines.Add(new Run { Text = text[index].ToString() });
			block.Inlines.Add(underline);

			if (index < text.Length - 1)
				block.Inlines.Add(new Run { Text = text[(index + 1)..] });

			// The accessible name stays the plain text, without markers
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(control, text);
			control.Content = block;
		}

		[DynamicWindowsRuntimeCast(typeof(ContentDialog))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		[DynamicWindowsRuntimeCast(typeof(PasswordBox))]
		[DynamicWindowsRuntimeCast(typeof(RichEditBox))]
		[DynamicWindowsRuntimeCast(typeof(ComboBox))]
		[DynamicWindowsRuntimeCast(typeof(AutoSuggestBox))]
		[DynamicWindowsRuntimeCast(typeof(NumberBox))]
		[DynamicWindowsRuntimeCast(typeof(ContentControl))]
		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		[DynamicWindowsRuntimeCast(typeof(RadioButton))]
		[DynamicWindowsRuntimeCast(typeof(Button))]
		private static void Dialog_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not ContentDialog dialog || !attached.TryGetValue(dialog, out var state))
				return;

			if (e.Key is < VirtualKey.A or > VirtualKey.Z)
				return;

			if (IsDown(VirtualKey.Control) || IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
				return;

			var focused = FocusManager.GetFocusedElement(dialog.XamlRoot);
			if (focused is TextBox or PasswordBox or RichEditBox or ComboBox or AutoSuggestBox or NumberBox)
				return;

			var letter = (char)('A' + (e.Key - VirtualKey.A));
			var entry = state.Entries.FirstOrDefault(x => x.Letter == letter);
			if (entry is null)
				return;

			var control = (ContentControl)entry.Control;
			if (!control.IsEnabled || control.Visibility != Visibility.Visible)
				return;

			e.Handled = true;

			switch (control)
			{
				case CheckBox checkBox:
					checkBox.IsChecked = checkBox.IsChecked != true;
					break;
				case RadioButton radio:
					radio.IsChecked = true;
					break;
				case Button button:
					(new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke) as IInvokeProvider)?.Invoke();
					break;
			}
		}

		private static bool IsDown(VirtualKey key)
			=> InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private static FrameworkElement? FindByName(DependencyObject root, string name)
		{
			int count = VisualTreeHelper.GetChildrenCount(root);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is FrameworkElement element && element.Name == name)
					return element;

				var found = FindByName(child, name);
				if (found is not null)
					return found;
			}

			return null;
		}
	}
}
