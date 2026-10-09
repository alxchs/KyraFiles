// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Files Custom: takes over Explorer folder windows while Files is the default file manager (F012 T9, D-017).
	/// </summary>
	/// <remarks>
	/// Win+E and explorer.exe &lt;path&gt; are handled inside Explorer and ignore the registry associations.
	/// A new Explorer window is made transparent, read through ShellWindows, closed and reopened in Files.
	/// Locations Files does not show are made visible again.
	/// </remarks>
	internal static partial class ExplorerWindowRedirector
	{
		private const uint EventObjectShow = 0x8002;
		private const uint WinEventOutOfContext = 0x0000;
		private const uint WinEventSkipOwnProcess = 0x0002;
		private const int ExtendedStyleIndex = -20;
		private const int LayeredStyle = 0x00080000;
		private const uint LayeredAlpha = 0x00000002;
		private const uint CloseMessage = 0x0010;
		private const uint MultithreadedApartment = 0x0;
		private const string HomeLocation = "::{F874310E-B6B7-47DC-BC84-B9E6B38F5903}";

		private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(8);
		private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

		// Window handle -> extended style before it was made transparent
		private static readonly ConcurrentDictionary<nint, int> pendingWindows = new();

		private static nint hook;
		private static Mutex? ownerMutex;
		private static long suppressUntil;

		/// <summary>
		/// Starts watching on the calling thread, which must pump messages (the UI thread).
		/// </summary>
		public static unsafe void Start()
		{
			// A file dialog instance never takes over Explorer windows
			if (hook != 0 || App.OutputPath is not null)
				return;

			// Only one Files process watches
			var mutex = new Mutex(true, $"Files-{AppLifecycleHelper.AppEnvironment}-ExplorerWindowRedirector", out var owned);
			if (!owned)
			{
				mutex.Dispose();
				return;
			}

			ownerMutex = mutex;
			hook = SetWinEventHook(EventObjectShow, EventObjectShow, 0, &OnWinEvent, 0, 0, WinEventOutOfContext | WinEventSkipOwnProcess);
			if (hook == 0)
				App.Logger.LogWarning("Explorer window redirection could not start (SetWinEventHook failed)");
		}

		/// <summary>
		/// Stops watching and makes any window still being read visible again.
		/// </summary>
		public static void Stop()
		{
			if (hook != 0)
			{
				UnhookWinEvent(hook);
				hook = 0;
			}

			foreach (var window in pendingWindows.Keys)
				if (pendingWindows.TryRemove(window, out var extendedStyle))
					Restore(window, extendedStyle);

			if (ownerMutex is not null)
			{
				SafetyExtensions.IgnoreExceptions(ownerMutex.ReleaseMutex);
				ownerMutex.Dispose();
				ownerMutex = null;
			}
		}

		/// <summary>
		/// Leaves the Explorer windows that open in the next moments alone; Files is opening Explorer on purpose.
		/// </summary>
		public static void SuppressFor(TimeSpan duration)
			=> Interlocked.Exchange(ref suppressUntil, Environment.TickCount64 + (long)duration.TotalMilliseconds);

		[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
		private static void OnWinEvent(nint eventHook, uint eventType, nint window, int objectId, int childId, uint eventThread, uint eventTime)
		{
			try
			{
				if (objectId != 0 || window == 0 || pendingWindows.ContainsKey(window))
					return;
				if (Environment.TickCount64 < Interlocked.Read(ref suppressUntil))
					return;
				if (!IsExplorerFolderWindow(window) || !IsDefaultFileManager())
					return;

				var extendedStyle = GetWindowLongW(window, ExtendedStyleIndex);
				if (!pendingWindows.TryAdd(window, extendedStyle))
					return;

				// Hide it before it paints; it stays where Explorer put it in case it has to come back
				SetWindowLongW(window, ExtendedStyleIndex, extendedStyle | LayeredStyle);
				SetLayeredWindowAttributes(window, 0, 0, LayeredAlpha);

				var worker = new Thread(() => TakeOver(window)) { IsBackground = true, Name = "Explorer window redirection" };
				worker.Start();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Explorer window redirection failed on a new window");
			}
		}

		private static void TakeOver(nint window)
		{
			var handedOver = false;
			var comInitialized = InitializeCom();
			try
			{
				string? location = null;
				List<string> selection = [];
				var deadline = Environment.TickCount64 + (long)LookupTimeout.TotalMilliseconds;
				while (Environment.TickCount64 < deadline && IsWindow(window))
				{
					if (TryRead(window, out location, out selection))
						break;
					Thread.Sleep(50);
				}

				var target = location is null ? null : ToFilesLocation(location);
				if (target is null)
				{
					App.Logger.LogInformation("Explorer window left alone: {Location}", location ?? "(not read)");
					return;
				}

				// "Show in folder" selects the item right after the view is ready
				for (var attempt = 0; attempt < 3 && selection.Count == 0; attempt++)
				{
					Thread.Sleep(60);
					TryRead(window, out _, out selection);
				}

				PostMessageW(window, CloseMessage, 0, 0);
				var closeDeadline = Environment.TickCount64 + (long)CloseTimeout.TotalMilliseconds;
				while (Environment.TickCount64 < closeDeadline && IsWindow(window))
					Thread.Sleep(20);
				if (IsWindow(window))
				{
					App.Logger.LogWarning("Explorer window did not close; showing it again: {Location}", location);
					return;
				}

				handedOver = true;
				var selectItem = selection.Count == 1 && System.IO.Path.IsPathRooted(selection[0]) ? selection[0] : null;
				App.Logger.LogInformation("Explorer window taken over: {Location} (selected: {Selected})", target, selectItem ?? "-");
				MainWindow.Instance.DispatcherQueue.TryEnqueue(async () =>
				{
					try
					{
						await MainWindow.Instance.OpenFromExplorerAsync(target, selectItem);
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Failed to open {Location} taken over from Explorer", target);
					}
				});
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Explorer window redirection failed");
			}
			finally
			{
				if (pendingWindows.TryRemove(window, out var extendedStyle) && !handedOver)
					Restore(window, extendedStyle);
				if (comInitialized)
					CoUninitialize();
			}
		}

		private static unsafe bool InitializeCom()
			=> CoInitializeEx(null, MultithreadedApartment) >= 0;

		private static bool TryRead(nint window, out string? location, out List<string> selection)
		{
			location = null;
			selection = [];
			try
			{
				using var shellWindows = new ShellWindowsAutomation();
				var count = shellWindows.Count;
				for (var index = 0; index < count; index++)
				{
					using var shellWindow = shellWindows.GetWindow(index);
					if (shellWindow is not null &&
						shellWindow.TryGetLocation(out var handle, out var parsingName, out var selected) &&
						handle == window)
					{
						location = parsingName;
						selection = selected;
						return true;
					}
				}
			}
			catch (COMException)
			{
				// The window is still being created
			}

			return false;
		}

		private static string? ToFilesLocation(string location)
		{
			if (location.Equals(HomeLocation, StringComparison.OrdinalIgnoreCase))
				return "Home";
			if (ShellHelpers.IsSupportedShellPath(location))
				return location;

			// Disk and network folders; other shell namespaces (Control Panel, libraries) stay in Explorer
			return !location.StartsWith("::", StringComparison.Ordinal) && System.IO.Path.IsPathRooted(location) ? location : null;
		}

		private static unsafe bool IsExplorerFolderWindow(nint window)
		{
			var className = stackalloc char[32];
			var length = GetClassNameW(window, className, 32);
			return new ReadOnlySpan<char>(className, length).SequenceEqual("CabinetWClass");
		}

		private static bool IsDefaultFileManager()
		{
			using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
			var command = subkey?.GetValue(string.Empty) as string;
			return command?.Contains("Files.App.Launcher.exe", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		private static void Restore(nint window, int extendedStyle)
		{
			if (!IsWindow(window))
				return;

			SetLayeredWindowAttributes(window, 0, 255, LayeredAlpha);
			if ((extendedStyle & LayeredStyle) == 0)
				SetWindowLongW(window, ExtendedStyleIndex, extendedStyle);
		}

		[LibraryImport("user32.dll")]
		private static unsafe partial nint SetWinEventHook(uint eventMin, uint eventMax, nint module, delegate* unmanaged[Stdcall]<nint, uint, nint, int, int, uint, uint, void> callback, uint processId, uint threadId, uint flags);

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool UnhookWinEvent(nint eventHook);

		[LibraryImport("user32.dll")]
		private static unsafe partial int GetClassNameW(nint window, char* className, int maxCount);

		[LibraryImport("user32.dll")]
		private static partial int GetWindowLongW(nint window, int index);

		[LibraryImport("user32.dll")]
		private static partial int SetWindowLongW(nint window, int index, int value);

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool SetLayeredWindowAttributes(nint window, uint colorKey, byte alpha, uint flags);

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool PostMessageW(nint window, uint message, nint wParam, nint lParam);

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool IsWindow(nint window);

		[LibraryImport("ole32.dll")]
		private static unsafe partial int CoInitializeEx(void* reserved, uint coInit);

		[LibraryImport("ole32.dll")]
		private static partial void CoUninitialize();
	}
}
