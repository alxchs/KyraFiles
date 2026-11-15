// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Recovery;

namespace KyraFiles.App.Services
{
	public sealed partial class SideloadUpdateService : ObservableObject, IUpdateService, IDisposable
	{
		private const string SIDELOAD_STABLE = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
		private const string SIDELOAD_PREVIEW = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

		private readonly HttpClient _client = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) });

		private readonly Dictionary<string, string> _sideloadVersion = new()
		{
			{ "Files", SIDELOAD_STABLE },
			{ "FilesPreview", SIDELOAD_PREVIEW }
		};

		private ILogger? Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private string PackageName { get; } = Package.Current.Id.Name;

		private Version PackageVersion { get; } = new(
			Package.Current.Id.Version.Major,
			Package.Current.Id.Version.Minor,
			Package.Current.Id.Version.Build,
			Package.Current.Id.Version.Revision);

		private bool _isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			private set => SetProperty(ref _isUpdateAvailable, value);
		}

		private bool _isUpdating;
		public bool IsUpdating
		{
			get => _isUpdating;
			private set => SetProperty(ref _isUpdating, value);
		}

		private int _updateProgress;
		public int UpdateProgress
		{
			get => _updateProgress;
			private set => SetProperty(ref _updateProgress, value);
		}

		public bool IsAppUpdated
		{
			get => AppLifecycleHelper.IsAppUpdated;
		}

		private bool _areReleaseNotesAvailable = false;
		public bool AreReleaseNotesAvailable
		{
			get => _areReleaseNotesAvailable;
			private set => SetProperty(ref _areReleaseNotesAvailable, value);
		}

		private ReleaseCheckResult? _latestResult;

		public async Task DownloadUpdatesAsync()
		{
			if (_latestResult?.PageUrl is not null)
			{
				try
				{
					await Windows.System.Launcher.LaunchUriAsync(new Uri(_latestResult.PageUrl));
					IsUpdateAvailable = false;
					return;
				}
				catch (Exception ex)
				{
					Logger?.LogWarning(ex, "Failed to launch update URL: {Url}", _latestResult.PageUrl);
				}
			}

			await ApplyPackageUpdateAsync();
		}

		public Task DownloadMandatoryUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public async Task CheckForUpdatesAsync()
		{
			IsUpdateAvailable = false;
			try
			{
				Logger?.LogInformation("SIDELOAD: Checking for updates via Custom Releases...");

				var result = await ReleaseCheckService.CheckAsync();
				_latestResult = result;
				if (result.Status == ReleaseCheckStatus.Available)
				{
					Logger?.LogInformation($"SIDELOAD: Update found: {result.Latest} (current: {result.Current})");
					MainWindow.Instance?.DispatcherQueue.TryEnqueue(() =>
					{
						IsUpdateAvailable = true;
					});
				}
				else
				{
					Logger?.LogInformation($"SIDELOAD: Update check status: {result.Status} (current: {result.Current})");
				}
			}
			catch (HttpRequestException ex)
			{
				Logger?.LogDebug(ex, ex.Message);
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, ex.Message);
			}
		}

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			try
			{
				var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "KyraFiles");
				var destExeFilePath = Path.Combine(destFolderPath, "KyraFiles.App.Launcher.exe");
				var branchFilePath = Path.Combine(destFolderPath, "Branch.txt");

				// If KyraFiles.App.Launcher.exe doesn't exist, no need to update it.
				if (!File.Exists(destExeFilePath))
					return;

				// Check if the launcher file is associated with the current branch of the app.
				if (File.Exists(branchFilePath))
				{
					try
					{
						var branch = await File.ReadAllTextAsync(branchFilePath, Encoding.UTF8);
						if (!string.Equals(branch.Trim(), "files-dev", StringComparison.OrdinalIgnoreCase))
							return;
					}
					catch { }
				}
				else
				{
					try
					{
						// Create branch file for users updating from versions earlier than v4.0.20.
						await File.WriteAllTextAsync(branchFilePath, "files-dev", Encoding.UTF8);
					}
					catch { }
				}

				var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/KyraFiles.App.Launcher.exe"));
				var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

				await srcExeFile.CopyAsync(destFolder, "KyraFiles.App.Launcher.exe", NameCollisionOption.ReplaceExisting);

				App.Logger.LogInformation("KyraFiles.App.Launcher updated.");
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, ex.Message);
				return;
			}
		}

		public async Task CheckForReleaseNotesAsync()
		{
			using var client = new HttpClient();

			try
			{
				var response = await client.GetAsync(Constants.ExternalUrl.ReleaseNotesUrl);
				AreReleaseNotesAvailable = response.IsSuccessStatusCode;
			}
			catch
			{
				AreReleaseNotesAvailable = false;
			}
		}

		private async Task ApplyPackageUpdateAsync()
		{
			if (!IsUpdateAvailable)
				return;

			IsUpdating = true;

			DeploymentResult? result = null;

			try
			{
				PackageManager packageManager = new PackageManager();

				var restartStatus = PInvoke.RegisterApplicationRestart(null, (REGISTER_APPLICATION_RESTART_FLAGS)0);
				App.AppModel.ForceProcessTermination = true;

				Logger?.LogInformation($"Register for restart: {restartStatus}");

				await Task.Run(async () =>
				{
					if (!_sideloadVersion.TryGetValue(PackageName, out var appInstallerUrl))
					{
						if (_latestResult?.PageUrl is not null)
						{
							await Windows.System.Launcher.LaunchUriAsync(new Uri(_latestResult.PageUrl));
						}
						return;
					}

					var appInstallerUri = new Uri(appInstallerUrl);

					var deployment = packageManager.AddPackageByAppInstallerFileAsync(
						appInstallerUri,
						AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
						packageManager.GetDefaultPackageVolume());

					deployment.Progress = (op, progress) =>
					{
						MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
						{
							UpdateProgress = (int)progress.percentage;
						});
					};

					result = await deployment;
				});
			}
			catch (Exception e)
			{
				if (result?.ExtendedErrorCode is not null)
					Logger?.LogInformation(result.ErrorText);

				Logger?.LogError(e, e.Message);
			}
			finally
			{
				// Reset fields
				IsUpdating = false;
				IsUpdateAvailable = false;
				UpdateProgress = 0;
			}
		}

		public void Dispose()
		{
			_client?.Dispose();
		}
	}
}
