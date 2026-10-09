// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Utils.Shell
{
	/// <summary>Provides the AOT-safe vtable calls needed for the IDispatch-based IShellWindows interface.</summary>
	internal sealed unsafe partial class ShellWindowsAutomation : IDisposable
	{
		private const int QueryInterfaceSlot = 0;
		private const int ReleaseSlot = 2;
		private const int GetCountSlot = 7;
		private const int GetItemSlot = 8;
		private const int QueryServiceSlot = 3;
		private const int BrowseObjectSlot = 11;
		private const int QueryActiveShellViewSlot = 15;
		private const int GetFolderSlot = 5;
		private const int GetCurrentFolderSlot = 5;
		private const int WebBrowserGetHwndSlot = 37;
		private static readonly Guid WebBrowser2InterfaceId = new("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
		private const int GetSelectionSlot = 32;
		private const int ItemArrayGetCountSlot = 7;
		private const int ItemArrayGetItemAtSlot = 8;
		private const int ShellItemGetDisplayNameSlot = 5;
		private const uint DesktopAbsoluteParsing = 0x80028000;
		private static readonly Guid FolderView2InterfaceId = new("1AF3A467-214F-4298-908E-06B03E0B39F9");

		private static readonly Guid ShellWindowsClassId = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
		private static readonly Guid ShellWindowsInterfaceId = new("85CB6900-4D95-11CF-960C-0080C7F4EE85");
		private static readonly Guid ServiceProviderInterfaceId = new("6D5140C1-7436-11CE-8034-00AA006009FA");
		private static readonly Guid TopLevelBrowserServiceId = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
		private static readonly Guid ShellBrowserInterfaceId = new("000214E2-0000-0000-C000-000000000046");
		private static readonly Guid FolderViewInterfaceId = new("CDE725B0-CCC9-4519-917E-325D72FAB4CE");
		private static readonly Guid PersistFolder2InterfaceId = new("1AC3D9F0-175C-11D1-95BE-00609797EA4F");

		private void* shellWindows;

		public ShellWindowsAutomation()
		{
			Guid classId = ShellWindowsClassId;
			Guid interfaceId = ShellWindowsInterfaceId;
			void* instance = null;
			new HRESULT(CoCreateInstance(&classId, null, CLSCTX.CLSCTX_LOCAL_SERVER, &interfaceId, &instance)).ThrowOnFailure();
			if (instance is null)
				throw new InvalidOperationException("The shell did not return an IShellWindows instance.");
			shellWindows = instance;
		}

		public int Count
		{
			get
			{
				ObjectDisposedException.ThrowIf(shellWindows is null, this);
				var getCount = (delegate* unmanaged[MemberFunction]<void*, int*, int>)GetVtable(shellWindows)[GetCountSlot];
				int count = 0;
				new HRESULT(getCount(shellWindows, &count)).ThrowOnFailure();
				return count;
			}
		}

		public ShellWindow? GetWindow(int index)
		{
			ObjectDisposedException.ThrowIf(shellWindows is null, this);
			var getItem = (delegate* unmanaged[MemberFunction]<void*, ComVariant, void**, int>)GetVtable(shellWindows)[GetItemSlot];
			ComVariant itemIndex = ComVariant.Create(index);
			void* window = null;
			HRESULT result = new(getItem(shellWindows, itemIndex, &window));
			if (result.Failed || window is null)
			{
				Release(ref window);
				return null;
			}

			return new(window);
		}

		public void Dispose()
		{
			Release(ref shellWindows);
			GC.SuppressFinalize(this);
		}

		[LibraryImport("ole32.dll", EntryPoint = "CoCreateInstance")]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static partial int CoCreateInstance(Guid* classId, void* outer, CLSCTX context, Guid* interfaceId, void** instance);

		private static void** GetVtable(void* instance)
			=> *(void***)instance;

		private static HRESULT QueryInterface(void* instance, Guid interfaceId, out void* result)
		{
			var queryInterface = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)GetVtable(instance)[QueryInterfaceSlot];
			Guid requestedInterface = interfaceId;
			void* queriedInterface = null;
			HRESULT queryResult = new(queryInterface(instance, &requestedInterface, &queriedInterface));
			result = queriedInterface;
			return queryResult;
		}

		private static void Release(ref void* instance)
		{
			if (instance is null)
				return;

			var release = (delegate* unmanaged[MemberFunction]<void*, uint>)GetVtable(instance)[ReleaseSlot];
			release(instance);
			instance = null;
		}

		internal sealed unsafe partial class ShellWindow : IDisposable
		{
			private void* window;

			public ShellWindow(void* window)
			{
				this.window = window;
			}

			public bool TryNavigate(ShellPidl targetPidl, ShellPidl controlPanelPidl)
			{
				ObjectDisposedException.ThrowIf(window is null, this);
				void* serviceProvider = null;
				void* shellBrowser = null;
				void* shellView = null;
				void* folderView = null;
				void* persistFolder = null;
				ITEMIDLIST* currentFolderPidl = null;
				byte[] targetBytes = targetPidl.GetBytes();
				byte[] controlPanelBytes = controlPanelPidl.GetBytes();

				fixed (byte* target = targetBytes)
				fixed (byte* controlPanel = controlPanelBytes)
				{
					try
					{
						if (QueryInterface(window, ServiceProviderInterfaceId, out serviceProvider).Failed ||
							serviceProvider is null)
							return false;
						if (QueryService(serviceProvider, TopLevelBrowserServiceId, ShellBrowserInterfaceId, out shellBrowser).Failed ||
							shellBrowser is null)
							return false;
						if (QueryActiveShellView(shellBrowser, out shellView).Failed || shellView is null)
							return false;
						if (QueryInterface(shellView, FolderViewInterfaceId, out folderView).Failed || folderView is null)
							return false;
						if (GetFolder(folderView, PersistFolder2InterfaceId, out persistFolder).Failed ||
							persistFolder is null)
							return false;
						if (GetCurrentFolder(persistFolder, out currentFolderPidl).Failed || currentFolderPidl is null)
							return false;

						bool canReuse = PInvoke.ILIsParent(currentFolderPidl, (ITEMIDLIST*)target, true) ||
							PInvoke.ILIsEqual(currentFolderPidl, (ITEMIDLIST*)controlPanel);
						return canReuse && BrowseObject(shellBrowser, (ITEMIDLIST*)target, PInvoke.SBSP_SAMEBROWSER | PInvoke.SBSP_ABSOLUTE).Succeeded;
					}
					finally
					{
						PInvoke.CoTaskMemFree(currentFolderPidl);
						Release(ref persistFolder);
						Release(ref folderView);
						Release(ref shellView);
						Release(ref shellBrowser);
						Release(ref serviceProvider);
					}
				}
			}

			/// <summary>
			/// Files Custom: reads the window handle, the parsing name of the current folder and the selected items (F012 T9).
			/// </summary>
			public bool TryGetLocation(out nint windowHandle, out string? parsingName, out List<string> selection)
			{
				ObjectDisposedException.ThrowIf(window is null, this);
				windowHandle = 0;
				parsingName = null;
				selection = [];
				void* serviceProvider = null;
				void* shellBrowser = null;
				void* shellView = null;
				void* folderView = null;
				void* persistFolder = null;
				void* itemArray = null;
				ITEMIDLIST* currentFolderPidl = null;
				void* webBrowser = null;
				try
				{
					// IShellBrowser::GetWindow is input-synchronous and fails across processes; IWebBrowser2::get_HWND does not
					if (QueryInterface(window, WebBrowser2InterfaceId, out webBrowser).Failed || webBrowser is null)
						return false;
					var getHwnd = (delegate* unmanaged[MemberFunction]<void*, nint*, int>)GetVtable(webBrowser)[WebBrowserGetHwndSlot];
					nint handle = 0;
					if (new HRESULT(getHwnd(webBrowser, &handle)).Failed || handle == 0)
						return false;
					windowHandle = handle;

					if (QueryInterface(window, ServiceProviderInterfaceId, out serviceProvider).Failed || serviceProvider is null)
						return false;
					if (QueryService(serviceProvider, TopLevelBrowserServiceId, ShellBrowserInterfaceId, out shellBrowser).Failed || shellBrowser is null)
						return false;

					if (QueryActiveShellView(shellBrowser, out shellView).Failed || shellView is null)
						return false;
					if (QueryInterface(shellView, FolderView2InterfaceId, out folderView).Failed || folderView is null)
						return false;
					if (GetFolder(folderView, PersistFolder2InterfaceId, out persistFolder).Failed || persistFolder is null)
						return false;
					if (GetCurrentFolder(persistFolder, out currentFolderPidl).Failed || currentFolderPidl is null)
						return false;

					parsingName = GetParsingName(currentFolderPidl);
					if (parsingName is null)
						return false;

					var getSelection = (delegate* unmanaged[MemberFunction]<void*, int, void**, int>)GetVtable(folderView)[GetSelectionSlot];
					void* items = null;
					if (new HRESULT(getSelection(folderView, 0, &items)).Succeeded && items is not null)
					{
						itemArray = items;
						var getCount = (delegate* unmanaged[MemberFunction]<void*, uint*, int>)GetVtable(itemArray)[ItemArrayGetCountSlot];
						var getItemAt = (delegate* unmanaged[MemberFunction]<void*, uint, void**, int>)GetVtable(itemArray)[ItemArrayGetItemAtSlot];
						uint count = 0;
						if (new HRESULT(getCount(itemArray, &count)).Succeeded)
						{
							for (uint index = 0; index < count; index++)
							{
								void* item = null;
								if (new HRESULT(getItemAt(itemArray, index, &item)).Failed || item is null)
									continue;

								var getDisplayName = (delegate* unmanaged[MemberFunction]<void*, uint, char**, int>)GetVtable(item)[ShellItemGetDisplayNameSlot];
								char* name = null;
								if (new HRESULT(getDisplayName(item, DesktopAbsoluteParsing, &name)).Succeeded && name is not null)
								{
									selection.Add(new string(name));
									PInvoke.CoTaskMemFree(name);
								}

								Release(ref item);
							}
						}
					}

					return true;
				}
				finally
				{
					PInvoke.CoTaskMemFree(currentFolderPidl);
					Release(ref itemArray);
					Release(ref persistFolder);
					Release(ref folderView);
					Release(ref shellView);
					Release(ref shellBrowser);
					Release(ref serviceProvider);
					Release(ref webBrowser);
				}
			}

			private static string? GetParsingName(ITEMIDLIST* pidl)
			{
				char* name = null;
				if (SHGetNameFromIDList(pidl, DesktopAbsoluteParsing, &name) < 0 || name is null)
					return null;

				try
				{
					return new string(name);
				}
				finally
				{
					PInvoke.CoTaskMemFree(name);
				}
			}

			[LibraryImport("shell32.dll", EntryPoint = "SHGetNameFromIDList")]
			[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
			private static partial int SHGetNameFromIDList(ITEMIDLIST* pidl, uint sigdnName, char** name);

			public void Dispose()
			{
				Release(ref window);
				GC.SuppressFinalize(this);
			}

			private static HRESULT QueryService(void* serviceProvider, Guid serviceId, Guid interfaceId, out void* result)
			{
				var queryService = (delegate* unmanaged[MemberFunction]<void*, Guid*, Guid*, void**, int>)GetVtable(serviceProvider)[QueryServiceSlot];
				Guid requestedService = serviceId;
				Guid requestedInterface = interfaceId;
				void* service = null;
				HRESULT queryResult = new(queryService(serviceProvider, &requestedService, &requestedInterface, &service));
				result = service;
				return queryResult;
			}

			private static HRESULT QueryActiveShellView(void* shellBrowser, out void* shellView)
			{
				var queryActiveShellView = (delegate* unmanaged[MemberFunction]<void*, void**, int>)GetVtable(shellBrowser)[QueryActiveShellViewSlot];
				void* activeView = null;
				HRESULT queryResult = new(queryActiveShellView(shellBrowser, &activeView));
				shellView = activeView;
				return queryResult;
			}

			private static HRESULT GetFolder(void* folderView, Guid interfaceId, out void* folder)
			{
				var getFolder = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)GetVtable(folderView)[GetFolderSlot];
				Guid requestedInterface = interfaceId;
				void* shellFolder = null;
				HRESULT getFolderResult = new(getFolder(folderView, &requestedInterface, &shellFolder));
				folder = shellFolder;
				return getFolderResult;
			}

			private static HRESULT GetCurrentFolder(void* persistFolder, out ITEMIDLIST* folderPidl)
			{
				var getCurrentFolder = (delegate* unmanaged[MemberFunction]<void*, ITEMIDLIST**, int>)GetVtable(persistFolder)[GetCurrentFolderSlot];
				ITEMIDLIST* currentFolder = null;
				HRESULT getFolderResult = new(getCurrentFolder(persistFolder, &currentFolder));
				folderPidl = currentFolder;
				return getFolderResult;
			}

			private static HRESULT BrowseObject(void* shellBrowser, ITEMIDLIST* pidl, uint flags)
			{
				var browseObject = (delegate* unmanaged[MemberFunction]<void*, ITEMIDLIST*, uint, int>)GetVtable(shellBrowser)[BrowseObjectSlot];
				return new(browseObject(shellBrowser, pidl, flags));
			}
		}
	}
}
