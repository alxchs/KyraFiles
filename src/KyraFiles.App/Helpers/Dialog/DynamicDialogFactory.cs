// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Files.App.Helpers
{
	public static class DynamicDialogFactory
	{
		public static readonly SolidColorBrush _transparentBrush = new SolidColorBrush(Colors.Transparent);

		public static DynamicDialog GetFor_PropertySaveErrorDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.PropertySaveErrorDialogTitle.GetLocalizedResource(),
				SubtitleText = Strings.PropertySaveErrorMessageText.GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = Strings.Retry.GetLocalizedResource(),
				SecondaryButtonText = Strings.PropertySaveErrorDialog_SecondaryButtonText.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ConsentDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.WelcomeDialogTitle.GetLocalizedResource(),
				SubtitleText = Strings.WelcomeDialogTextBlockText.GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = Strings.WelcomeDialog_PrimaryButtonText.GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ShortcutNotFound(string? targetPath)
		{
			DynamicDialog dialog = new(new DynamicDialogViewModel
			{
				TitleText = Strings.ShortcutCannotBeOpened.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.DeleteShortcutDescription.GetLocalizedResource(), targetPath),
				PrimaryButtonText = Strings.Delete.GetLocalizedResource(),
				SecondaryButtonText = Strings.No.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CreateItemDialog(string itemType, string? itemName)
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = Strings.EnterAnItemName.GetLocalizedResource()
			};

			Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(inputText, "CreateItemDialogNameTextBox");

			TeachingTip warning = new()
			{
				Title = Strings.InvalidFilename_Text.GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			void UpdateDialogState()
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);

				// An unparented TeachingTip can only open once it has a XamlRoot, so keep the warning closed until then
				warning.XamlRoot ??= inputText.XamlRoot;
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = warning.XamlRoot is not null && !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			}

			inputText.TextChanged += (textBox, args) => UpdateDialogState();

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format(Strings.CreateNewItemTitle.GetLocalizedResource(), itemType),
				SubtitleText = Strings.EnterAnItemName.GetLocalizedResource(),
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				DisplayControlOnLoaded = async (vm, e) =>
				{
					UpdateDialogState();

					// The dialog asynchronously moves initial focus to its default button on an
					// unpredictable schedule, so keep refocusing until focus verifiably sticks
					for (int i = 0; i < 20; i++)
					{
						if (inputText.XamlRoot is not null &&
							Equals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(inputText.XamlRoot), inputText))
							return;

						inputText.Focus(FocusState.Programmatic);
						inputText.SelectAll();
						await Task.Delay(50);
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.Hide(); // Rename successful
				},
				PrimaryButtonText = Strings.Create.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			// Prefill text box with default name #17845
			if (itemType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
				inputText.Text = Strings.NewFolder.GetLocalizedResource();
			else if (itemName is not null)
				inputText.Text = FilesystemHelpers.FilterRestrictedCharacters(string.Format(Strings.CreateNewFile.GetLocalizedResource(), itemName));

			// TextChanged is not raised for text set while the box is not loaded yet,
			// so apply the initial validation state directly
			UpdateDialogState();

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}

		public static DynamicDialog GetFor_CannotRunFileDialog()
		{
			return new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.CannotRunFileDialogTitle.GetLocalizedResource(),
				SubtitleText = Strings.CannotRunFileDialogText.GetLocalizedResource(),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
		}

		public static DynamicDialog GetFor_FileInUseDialog(List<Win32Process>? lockingProcess = null)
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.FileInUseDialogTitle.GetLocalizedResource(),
				SubtitleText = lockingProcess.IsEmpty() ? Strings.FileInUseDialogText.GetLocalizedResource() :
					string.Format(Strings.FileInUseByDialogText.GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
				PrimaryButtonText = Strings.Retry.GetLocalizedResource(),
				SecondaryButtonText = Strings.Skip.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CredentialEntryDialog(string path)
		{
			string[] userAndPass = new string[3];
			DynamicDialog? dialog = null;

			TextBox inputUsername = new()
			{
				PlaceholderText = Strings.CredentialDialogUserName_PlaceholderText.GetLocalizedResource()
			};

			PasswordBox inputPassword = new()
			{
				PlaceholderText = Strings.Password.GetLocalizedResource()
			};

			CheckBox saveCreds = new()
			{
				Content = Strings.NetworkAuthenticationSaveCheckbox.GetLocalizedResource()
			};

			inputUsername.TextChanged += (textBox, args) =>
			{
				userAndPass[0] = inputUsername.Text;
				dialog!.ViewModel.AdditionalData = userAndPass;
			};

			inputPassword.PasswordChanged += (textBox, args) =>
			{
				userAndPass[1] = inputPassword.Password;
				dialog!.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Checked += (textBox, args) =>
			{
				userAndPass[2] = "y";
				dialog!.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Unchecked += (textBox, args) =>
			{
				userAndPass[2] = "n";
				dialog!.ViewModel.AdditionalData = userAndPass;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.NetworkAuthenticationDialogTitle.GetLocalizedResource(),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.NetworkAuthenticationDialogMessage.GetLocalizedResource(), path.Substring(2)),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						new StackPanel()
						{
							Spacing = 10d,
							Children =
							{
								inputUsername,
								inputPassword,
								saveCreds
							}
						}
					}
				},
				CloseButtonAction = (vm, e) =>
				{
					vm.AdditionalData = null;
					vm.Hide();
				}

			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitCheckoutConflicts(string checkoutBranchName, string headBranchName)
		{
			DynamicDialog dialog = null!;

			var optionsListView = new ListView()
			{
				ItemsSource = new string[]
				{
					string.Format(Strings.BringChanges.GetLocalizedResource(), checkoutBranchName),
					string.Format(Strings.StashChanges.GetLocalizedResource(), headBranchName),
					Strings.DiscardChanges.GetLocalizedResource()
				},
				SelectionMode = ListViewSelectionMode.Single
			};
			optionsListView.SelectedIndex = 0;

			optionsListView.SelectionChanged += (listView, args) =>
			{
				dialog.ViewModel.AdditionalData = (GitCheckoutOptions)optionsListView.SelectedIndex;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.SwitchBranch.GetLocalizedResource(),
				PrimaryButtonText = Strings.Switch.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				SubtitleText = Strings.UncommittedChanges.GetLocalizedResource(),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						optionsListView
					}
				},
				AdditionalData = GitCheckoutOptions.BringChanges,
				CloseButtonAction = (vm, e) =>
				{
					vm.AdditionalData = GitCheckoutOptions.None;
					vm.Hide();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitMergeConflicts(string checkoutBranchName, string headBranchName)
		{
			DynamicDialog dialog = null!;

			var optionsListView = new ListView()
			{
				ItemsSource = new string[]
				{
					string.Format(Strings.AbortMergeAndSwitch.GetLocalizedResource(), checkoutBranchName),
					string.Format(Strings.StayAndResolveConflicts.GetLocalizedResource(), headBranchName)
				},
				SelectionMode = ListViewSelectionMode.Single
			};
			optionsListView.SelectedIndex = 0;

			optionsListView.SelectionChanged += (listView, args) =>
			{
				dialog.ViewModel.AdditionalData = optionsListView.SelectedIndex == 0
					? GitCheckoutOptions.AbortMerge
					: GitCheckoutOptions.None;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.SwitchBranch.GetLocalizedResource(),
				PrimaryButtonText = Strings.Confirm.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				SubtitleText = Strings.MergeInProgress.GetLocalizedResource(),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						optionsListView
					}
				},
				AdditionalData = GitCheckoutOptions.AbortMerge,
				CloseButtonAction = (vm, e) =>
				{
					vm.AdditionalData = GitCheckoutOptions.None;
					vm.Hide();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitHubConnectionError()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = Strings.CannotReachGitHubError.GetLocalizedResource(),
				PrimaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}
		public static DynamicDialog GetFor_GitHubOAuthClientIdNotConfigured()
		{
			return new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "GitHubOAuthClientIdNotConfigured_Title".GetLocalizedResource(),
				SubtitleText = "GitHubOAuthClientIdNotConfigured_Subtitle".GetLocalizedResource(),
				PrimaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
		}

		public static DynamicDialog GetFor_GitCannotInitializeqRepositoryHere()
		{
			return new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = Strings.CannotInitializeGitRepo.GetLocalizedResource(),
				PrimaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
		}

		public static DynamicDialog GetFor_DeleteGitBranchConfirmation(string branchName)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.GitDeleteBranch.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.GitDeleteBranchSubtitle.GetLocalizedResource(), branchName),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				AdditionalData = true,
				CloseButtonAction = (vm, e) =>
				{
					vm.AdditionalData = false;
					vm.Hide();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_RenameRequiresHigherPermissions(string path)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.ItemRenameFailed.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.HigherPermissionsRequired.GetLocalizedResource(), path),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				SecondaryButtonText = Strings.EditPermissions.GetLocalizedResource(),
				SecondaryButtonAction = (vm, e) =>
				{
					var context = Ioc.Default.GetRequiredService<IContentPageContext>();
					var item = context.ShellPage?.ShellViewModel?.FilesAndFolders.FirstOrDefault(li => li.ItemPath!.Equals(path));

					if (context.ShellPage is not null && item is not null)
						FilePropertiesHelpers.OpenPropertiesWindow(item, context.ShellPage, PropertiesNavigationViewItemType.Security);
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_CreateAlternateDataStreamDialog()
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = Strings.EnterDataStreamName.GetLocalizedResource()
			};

			TeachingTip warning = new()
			{
				Title = Strings.InvalidFilename_Text.GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format(Strings.CreateAlternateDataStream.GetLocalizedResource()),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.Hide();
				},
				PrimaryButtonText = Strings.Create.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}

		public static async Task ShowFor_IDEErrorDialog(string friendlyName)
		{
			var commands = Ioc.Default.GetRequiredService<ICommandManager>();
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.IDENotLocatedTitle.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.IDENotLocatedContent.GetLocalizedResource(), friendlyName),
				PrimaryButtonText = Strings.OpenSettings.GetLocalizedResource(),
				SecondaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary,
			});

			await dialog.TryShowAsync();

			if (dialog.DynamicResult is DynamicDialogResult.Primary)
				await commands.OpenSettings.ExecuteAsync(
					new SettingsNavigationParams() { PageKind = SettingsPageKind.DevToolsPage }
				);
		}

		public static async Task ShowFor_CannotCloneRepo(string exception)
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.CannotCloneRepoTitle.GetLocalizedResource(),
				SubtitleText = exception,
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});

			await dialog.TryShowAsync();
		}

		public static DynamicDialog GetFor_SplitFileDialog(string sourceFilePath, long totalSize)
		{
			DynamicDialog? dialog = null;

			var partSizeHeader = new TextBlock
			{
				Text = Strings.PartSize.GetLocalizedResource(),
				Margin = new Thickness(0, 0, 0, 4)
			};

			var presetCombo = new ComboBox
			{
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			var item100Mb = new ComboBoxItem { Content = Strings.PartSizePreset100MB.GetLocalizedResource(), Tag = 100L * 1024 * 1024 };
			var item650Mb = new ComboBoxItem { Content = Strings.PartSizePreset650MB.GetLocalizedResource(), Tag = 650L * 1024 * 1024 };
			var item4Gb = new ComboBoxItem { Content = Strings.PartSizePreset4GB.GetLocalizedResource(), Tag = 4L * 1024 * 1024 * 1024 };
			var itemCustom = new ComboBoxItem { Content = Strings.PartSizeCustom.GetLocalizedResource(), Tag = 0L };

			presetCombo.Items.Add(item100Mb);
			presetCombo.Items.Add(item650Mb);
			presetCombo.Items.Add(item4Gb);
			presetCombo.Items.Add(itemCustom);
			presetCombo.SelectedIndex = 0;

			var customSizeBox = new TextBox
			{
				Text = "100 MB",
				PlaceholderText = Strings.PartSizeCustomPlaceholder.GetLocalizedResource(),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(0, 4, 0, 0)
			};

			var destHeader = new TextBlock
			{
				Text = Strings.DestinationFolder.GetLocalizedResource(),
				Margin = new Thickness(0, 8, 0, 4)
			};

			var destPathBox = new TextBox
			{
				Text = System.IO.Path.GetDirectoryName(sourceFilePath) ?? string.Empty,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			var browseButton = new Button
			{
				Content = Strings.Browse.GetLocalizedResource(),
				VerticalAlignment = VerticalAlignment.Center
			};

			browseButton.Click += (s, e) =>
			{
				var commonDialogService = Ioc.Default.GetRequiredService<ICommonDialogService>();
				if (commonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var folder) && !string.IsNullOrWhiteSpace(folder))
				{
					destPathBox.Text = folder;
				}
			};

			var destGrid = new Grid
			{
				ColumnSpacing = 8
			};
			destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Grid.SetColumn(destPathBox, 0);
			Grid.SetColumn(browseButton, 1);
			destGrid.Children.Add(destPathBox);
			destGrid.Children.Add(browseButton);

			var sha256CheckBox = new CheckBox
			{
				Content = Strings.CalculateSha256Checksum.GetLocalizedResource(),
				IsChecked = false,
				Margin = new Thickness(0, 4, 0, 0)
			};

			bool isUpdatingInternally = false;

			void UpdateValidation()
			{
				bool isValidSize = TryParsePartSize(customSizeBox.Text, out var parsedSize) && parsedSize > 0;
				bool isValidDest = !string.IsNullOrWhiteSpace(destPathBox.Text);
				bool isValid = isValidSize && isValidDest;

				if (dialog != null)
				{
					dialog.ViewModel.DynamicButtonsEnabled = isValid
						? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
						: DynamicDialogButtons.Cancel;

					if (isValid)
					{
						dialog.ViewModel.AdditionalData = new SplitDialogResult
						{
							PartSize = parsedSize,
							DestinationDirectory = destPathBox.Text.Trim(),
							CalculateSha256 = sha256CheckBox.IsChecked == true
						};
					}
				}
			}

			presetCombo.SelectionChanged += (s, e) =>
			{
				if (isUpdatingInternally)
					return;

				isUpdatingInternally = true;
				switch (presetCombo.SelectedIndex)
				{
					case 0:
						customSizeBox.Text = "100 MB";
						break;
					case 1:
						customSizeBox.Text = "650 MB";
						break;
					case 2:
						customSizeBox.Text = "4 GB";
						break;
				}
				isUpdatingInternally = false;

				UpdateValidation();
			};

			customSizeBox.TextChanged += (s, e) =>
			{
				if (isUpdatingInternally)
					return;

				isUpdatingInternally = true;
				if (TryParsePartSize(customSizeBox.Text, out var parsedBytes))
				{
					if (parsedBytes == 100L * 1024 * 1024)
						presetCombo.SelectedIndex = 0;
					else if (parsedBytes == 650L * 1024 * 1024)
						presetCombo.SelectedIndex = 1;
					else if (parsedBytes == 4L * 1024 * 1024 * 1024)
						presetCombo.SelectedIndex = 2;
					else
						presetCombo.SelectedIndex = 3;
				}
				else
				{
					presetCombo.SelectedIndex = 3;
				}
				isUpdatingInternally = false;

				UpdateValidation();
			};

			destPathBox.TextChanged += (s, e) => UpdateValidation();
			sha256CheckBox.Checked += (s, e) => UpdateValidation();
			sha256CheckBox.Unchecked += (s, e) => UpdateValidation();

			var contentStack = new StackPanel
			{
				Spacing = 8,
				MinWidth = 400
			};
			contentStack.Children.Add(partSizeHeader);
			contentStack.Children.Add(presetCombo);
			contentStack.Children.Add(customSizeBox);
			contentStack.Children.Add(destHeader);
			contentStack.Children.Add(destGrid);
			contentStack.Children.Add(sha256CheckBox);

			dialog = new DynamicDialog(new DynamicDialogViewModel
			{
				TitleText = Strings.SplitFileDialogTitle.GetLocalizedResource(),
				SubtitleText = string.Format(
					Strings.SplitFileDialogSubtitle.GetLocalizedResource(),
					System.IO.Path.GetFileName(sourceFilePath),
					totalSize.ToSizeString()),
				DisplayControl = contentStack,
				PrimaryButtonText = Strings.SplitFile.GetLocalizedResource().Replace("...", string.Empty).Trim(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel,
				DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel,
				PrimaryButtonAction = (vm, e) => vm.Hide()
			});

			UpdateValidation();
			return dialog;
		}

		public static DynamicDialog GetFor_CombineFilesDialog(string manifestFilePath, Files.Shared.Helpers.SplitManifest manifest)
		{
			DynamicDialog? dialog = null;

			var policyHeader = new TextBlock
			{
				Text = Strings.ExistingFileCollisionPolicy.GetLocalizedResource(),
				Margin = new Thickness(0, 0, 0, 4)
			};

			var policyCombo = new ComboBox
			{
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			var itemReject = new ComboBoxItem
			{
				Content = Strings.CollisionPolicyReject.GetLocalizedResource(),
				Tag = Files.Shared.Helpers.CombineCollisionPolicy.Recusar
			};
			var itemOverwrite = new ComboBoxItem
			{
				Content = Strings.CollisionPolicyOverwrite.GetLocalizedResource(),
				Tag = Files.Shared.Helpers.CombineCollisionPolicy.Sobrescrever
			};
			var itemRename = new ComboBoxItem
			{
				Content = Strings.CollisionPolicyRename.GetLocalizedResource(),
				Tag = Files.Shared.Helpers.CombineCollisionPolicy.Renomear
			};

			policyCombo.Items.Add(itemReject);
			policyCombo.Items.Add(itemOverwrite);
			policyCombo.Items.Add(itemRename);
			policyCombo.SelectedIndex = 0;

			var destHeader = new TextBlock
			{
				Text = Strings.DestinationFolder.GetLocalizedResource(),
				Margin = new Thickness(0, 8, 0, 4)
			};

			var destPathBox = new TextBox
			{
				Text = System.IO.Path.GetDirectoryName(manifestFilePath) ?? string.Empty,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			var browseButton = new Button
			{
				Content = Strings.Browse.GetLocalizedResource(),
				VerticalAlignment = VerticalAlignment.Center
			};

			browseButton.Click += (s, e) =>
			{
				var commonDialogService = Ioc.Default.GetRequiredService<ICommonDialogService>();
				if (commonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var folder) && !string.IsNullOrWhiteSpace(folder))
				{
					destPathBox.Text = folder;
				}
			};

			var destGrid = new Grid
			{
				ColumnSpacing = 8
			};
			destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Grid.SetColumn(destPathBox, 0);
			Grid.SetColumn(browseButton, 1);
			destGrid.Children.Add(destPathBox);
			destGrid.Children.Add(browseButton);

			void UpdateValidation()
			{
				bool isValid = !string.IsNullOrWhiteSpace(destPathBox.Text);
				var selectedPolicy = policyCombo.SelectedIndex switch
				{
					1 => Files.Shared.Helpers.CombineCollisionPolicy.Sobrescrever,
					2 => Files.Shared.Helpers.CombineCollisionPolicy.Renomear,
					_ => Files.Shared.Helpers.CombineCollisionPolicy.Recusar
				};

				if (dialog != null)
				{
					dialog.ViewModel.DynamicButtonsEnabled = isValid
						? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
						: DynamicDialogButtons.Cancel;

					if (isValid)
					{
						dialog.ViewModel.AdditionalData = new CombineDialogResult
						{
							CollisionPolicy = selectedPolicy,
							DestinationDirectory = destPathBox.Text.Trim()
						};
					}
				}
			}

			policyCombo.SelectionChanged += (s, e) => UpdateValidation();
			destPathBox.TextChanged += (s, e) => UpdateValidation();

			var contentStack = new StackPanel
			{
				Spacing = 8,
				MinWidth = 400
			};
			contentStack.Children.Add(policyHeader);
			contentStack.Children.Add(policyCombo);
			contentStack.Children.Add(destHeader);
			contentStack.Children.Add(destGrid);

			dialog = new DynamicDialog(new DynamicDialogViewModel
			{
				TitleText = Strings.CombineFilesDialogTitle.GetLocalizedResource(),
				SubtitleText = string.Format(
					Strings.CombineFilesDialogSubtitle.GetLocalizedResource(),
					manifest.OriginalFileName,
					manifest.Parts.Count,
					manifest.TotalSize.ToSizeString()),
				DisplayControl = contentStack,
				PrimaryButtonText = Strings.CombineFiles.GetLocalizedResource().Replace("...", string.Empty).Trim(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel,
				DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel,
				PrimaryButtonAction = (vm, e) => vm.Hide()
			});

			UpdateValidation();
			return dialog;
		}

		private static bool TryParsePartSize(string? text, out long bytes)
		{
			bytes = 0;
			if (string.IsNullOrWhiteSpace(text))
				return false;

			text = text.Trim();
			long multiplier = 1024L * 1024L;
			string numPart = text;

			if (text.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L * 1024L * 1024L;
				numPart = text[..^2].Trim();
			}
			else if (text.EndsWith("G", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L * 1024L * 1024L;
				numPart = text[..^1].Trim();
			}
			else if (text.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L * 1024L;
				numPart = text[..^2].Trim();
			}
			else if (text.EndsWith("M", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L * 1024L;
				numPart = text[..^1].Trim();
			}
			else if (text.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L;
				numPart = text[..^2].Trim();
			}
			else if (text.EndsWith("K", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1024L;
				numPart = text[..^1].Trim();
			}
			else if (text.EndsWith("B", StringComparison.OrdinalIgnoreCase))
			{
				multiplier = 1L;
				numPart = text[..^1].Trim();
			}

			if (double.TryParse(numPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedVal) ||
				double.TryParse(numPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out parsedVal))
			{
				if (parsedVal > 0)
				{
					try
					{
						bytes = checked((long)(parsedVal * multiplier));
						return bytes > 0;
					}
					catch (OverflowException)
					{
						return false;
					}
				}
			}

			return false;
		}
	}

	public sealed class SplitDialogResult
	{
		public long PartSize { get; set; } = 100L * 1024 * 1024;
		public string DestinationDirectory { get; set; } = string.Empty;
		public bool CalculateSha256 { get; set; }
	}

	public sealed class CombineDialogResult
	{
		public Files.Shared.Helpers.CombineCollisionPolicy CollisionPolicy { get; set; } = Files.Shared.Helpers.CombineCollisionPolicy.Recusar;
		public string DestinationDirectory { get; set; } = string.Empty;
	}
}
