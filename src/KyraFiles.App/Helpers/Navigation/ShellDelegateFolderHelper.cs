// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Files.App.Data.Contracts;
using Files.App.Data.Items;
using Files.App.Storage;
using Files.App.Utils.Shell;

namespace Files.App.Helpers
{
	/// <summary>
	/// Helper para enumerar itens delegados de "Este Computador" (servidores de mídia DLNA/UPnP, dispositivos portáteis)
	/// em thread STA própria com timeout estrito de 3 segundos (NFR-001/002 e F092 T4a).
	/// </summary>
	public static class ShellDelegateFolderHelper
	{
		public static async Task<IReadOnlyList<INavigationControlItem>> GetThisPCDelegateItemsAsync(TimeSpan? timeout = null)
		{
			var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);

			try
			{
				using var cts = new CancellationTokenSource(effectiveTimeout);
				var task = STATask.Run<IReadOnlyList<INavigationControlItem>>(() =>
				{
					var items = new List<INavigationControlItem>();

					try
					{
						using var shellFolder = ShellFolderExtensions.GetShellItemFromPathOrPIDL(Constants.UserEnvironmentPaths.MyComputerPath) as ShellFolder;
						if (shellFolder is null)
							return items;

						foreach (var folderItem in shellFolder)
						{
							if (cts.IsCancellationRequested)
								break;

							try
							{
								var fileItem = folderItem is Files.App.Utils.Shell.ShellLink link
									? ShellFolderExtensions.GetShellLinkItem(link)
									: ShellFolderExtensions.GetShellFileItem(folderItem);

								if (fileItem is null || string.IsNullOrEmpty(fileItem.FilePath))
									continue;

								// Ignora volumes convencionais com letra de unidade (ex: C:\, D:\), que pertencem à seção Drives
								if (Path.IsPathRooted(fileItem.FilePath) && fileItem.FilePath.Length >= 2 && fileItem.FilePath[1] == ':' && fileItem.FilePath.Length <= 3)
									continue;

								var item = new LocationItem
								{
									Text = fileItem.FileName ?? Path.GetFileName(fileItem.FilePath),
									Path = fileItem.FilePath,
									Section = SectionType.ThisPC,
									MenuOptions = new ContextMenuOptions
									{
										IsLocationItem = true,
										ShowProperties = true,
										ShowShellItems = true
									},
									SelectsOnInvoked = true,
									ChildItems = null
								};

								items.Add(item);
							}
							catch
							{
								// Ignora item com falha isolada
							}
						}
					}
					catch
					{
						// Retorna os itens obtidos até o momento
					}

					return items;
				}, App.Logger);

				var completed = await Task.WhenAny(task, Task.Delay(effectiveTimeout, cts.Token));
				if (completed == task)
				{
					return await task;
				}
				else
				{
					cts.Cancel();
					return Array.Empty<INavigationControlItem>();
				}
			}
			catch
			{
				return Array.Empty<INavigationControlItem>();
			}
		}
	}
}
