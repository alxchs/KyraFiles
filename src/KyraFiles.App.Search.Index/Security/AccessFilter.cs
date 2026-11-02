// Copyright (c) Files Community. Licensed under the MIT License.

using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using Files.App.Search.Index.Interop;
using Files.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace Files.App.Search.Index.Security;

/// <summary>
/// Filtro de segurança por permissão de diretório e utilitários de ACL para o modo completo (F015 T15 — FR-025, FR-043, NFR-006, AC-16).
/// Garante que o serviço rodando como <c>SYSTEM</c> nunca exponha arquivos de pastas às quais o usuário
/// conectado ao Named Pipe não tenha permissão de leitura/travessia, usando cache por pasta (<c>O(pastas)</c>).
/// </summary>
public sealed class AccessFilter
{
	private readonly VolumeIndex _index;
	private readonly Func<string, bool> _canAccessDirectoryPath;
	// Cache por índice de diretório no VolumeIndex: 1 = permitido, -1 = negado, 0 = ainda não avaliado
	private readonly Dictionary<int, sbyte> _directoryAccessCache = new();
	private bool _rootAccessibleChecked;
	private bool _rootAccessible;

	public int NativeChecksCount { get; private set; }

	public AccessFilter(VolumeIndex index, Func<string, bool>? customDirectoryAccessChecker = null)
	{
		ArgumentNullException.ThrowIfNull(index);
		_index = index;
		_canAccessDirectoryPath = customDirectoryAccessChecker ?? ProbeDirectoryReadAccess;
	}

	/// <summary>
	/// Verifica se o item na posição <paramref name="entryIndex"/> do <see cref="VolumeIndex"/>
	/// pode ser visualizado pelo usuário atual (ou pelo delegado de personificação).
	/// Avalia e memoriza em cache toda a cadeia de pastas ancestrais até a raiz do volume.
	/// </summary>
	public bool CanAccessEntry(int entryIndex)
	{
		if (_index.IsDeleted(entryIndex))
			return false;

		if (_index.IsDirectory(entryIndex))
			return CanAccessDirectory(entryIndex, depth: 0);

		int parentIndex = _index.GetParentIndex(entryIndex);
		return CanAccessDirectory(parentIndex, depth: 0);
	}

	/// <summary>
	/// Verifica recursivamente (com cache por índice de pasta) se o diretório <paramref name="dirIndex"/>
	/// e todos os seus ancestrais são acessíveis ao cliente.
	/// </summary>
	public bool CanAccessDirectory(int dirIndex, int depth = 0)
	{
		if (depth > 256)
			return false;

		if (dirIndex == VolumeIndex.RootParentIndex)
		{
			if (!_rootAccessibleChecked)
			{
				_rootAccessibleChecked = true;
				NativeChecksCount++;
				_rootAccessible = _canAccessDirectoryPath(_index.VolumeRoot);
			}
			return _rootAccessible;
		}

		if (dirIndex < 0 || _index.IsDeleted(dirIndex))
			return false;

		if (_directoryAccessCache.TryGetValue(dirIndex, out sbyte cached))
			return cached == 1;

		// Primeiro garante que o diretório pai também seja acessível (travessia hierárquica)
		int parentDir = _index.GetParentIndex(dirIndex);
		if (!CanAccessDirectory(parentDir, depth + 1))
		{
			_directoryAccessCache[dirIndex] = -1;
			return false;
		}

		string fullDirPath = _index.GetFullPath(dirIndex);
		NativeChecksCount++;
		bool allowed = _canAccessDirectoryPath(fullDirPath);
		_directoryAccessCache[dirIndex] = allowed ? (sbyte)1 : (sbyte)-1;
		return allowed;
	}

	/// <summary>
	/// Executa a verificação de acesso a um diretório personificando o cliente conectado ao <see cref="NamedPipeServerStream"/>.
	/// </summary>
	public static bool ProbeDirectoryReadAccessAsPipeClient(NamedPipeServerStream pipeServer, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(pipeServer);
		bool allowed = false;
		try
		{
			pipeServer.RunAsClient(() =>
			{
				allowed = ProbeDirectoryReadAccess(directoryPath);
			});
		}
		catch
		{
			allowed = false;
		}
		return allowed;
	}

	/// <summary>
	/// Testa se a identidade da thread atual tem permissão <c>FILE_LIST_DIRECTORY</c> na pasta informada.
	/// </summary>
	public static bool ProbeDirectoryReadAccess(string directoryPath)
	{
		if (string.IsNullOrWhiteSpace(directoryPath))
			return false;

		using SafeFileHandle handle = NativeMethods.CreateFileW(
			directoryPath,
			NativeMethods.FILE_LIST_DIRECTORY,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			IntPtr.Zero);

		return !handle.IsInvalid;
	}

	/// <summary>
	/// Retorna o diretório padrão de snapshots para o modo completo (<c>%ProgramData%\FilesCustom\Index</c> — NFR-006).
	/// </summary>
	public static string GetDefaultFullModeCacheDirectory()
	{
		string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
		return Path.Combine(commonAppData, "FilesCustom", "Index");
	}

	/// <summary>
	/// Cria e aplica a ACL restrita (<c>SYSTEM</c> + <c>Administradores</c>, sem herança de usuários comuns)
	/// na pasta de snapshots do modo completo (NFR-006).
	/// </summary>
	public static void EnsureSecureCacheDirectoryAcl(string directoryPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
		var dirInfo = Directory.CreateDirectory(directoryPath);

		var security = new DirectorySecurity();
		security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

		var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
		var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

		const FileSystemRights fullRights = FileSystemRights.FullControl;
		const InheritanceFlags inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

		security.AddAccessRule(new FileSystemAccessRule(systemSid, fullRights, inheritance, PropagationFlags.None, AccessControlType.Allow));
		security.AddAccessRule(new FileSystemAccessRule(adminsSid, fullRights, inheritance, PropagationFlags.None, AccessControlType.Allow));

		// Quando executado sem elevação em testes, garante que o próprio usuário criador também mantenha acesso ao diretório temporário
		using var currentIdentity = WindowsIdentity.GetCurrent();
		if (currentIdentity.User is not null &&
			!currentIdentity.User.Equals(systemSid) &&
			!new WindowsPrincipal(currentIdentity).IsInRole(WindowsBuiltInRole.Administrator))
		{
			security.AddAccessRule(new FileSystemAccessRule(currentIdentity.User, fullRights, inheritance, PropagationFlags.None, AccessControlType.Allow));
		}

		dirInfo.SetAccessControl(security);
	}

	/// <summary>
	/// Constrói a <see cref="PipeSecurity"/> para o Named Pipe do modo completo:
	/// permite apenas usuários autenticados locais (<c>AuthenticatedUserSid</c>) e nega acesso de rede (<c>NetworkSid</c>) (FR-043).
	/// </summary>
	public static PipeSecurity CreateLocalAuthenticatedPipeSecurity()
	{
		var pipeSecurity = new PipeSecurity();

		var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
		var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
		var authUsersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
		var networkSid = new SecurityIdentifier(WellKnownSidType.NetworkSid, null);
		var anonymousSid = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null);

		pipeSecurity.AddAccessRule(new PipeAccessRule(systemSid, PipeAccessRights.FullControl, AccessControlType.Allow));
		pipeSecurity.AddAccessRule(new PipeAccessRule(adminsSid, PipeAccessRights.FullControl, AccessControlType.Allow));
		pipeSecurity.AddAccessRule(new PipeAccessRule(authUsersSid, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
		pipeSecurity.AddAccessRule(new PipeAccessRule(networkSid, PipeAccessRights.FullControl, AccessControlType.Deny));
		pipeSecurity.AddAccessRule(new PipeAccessRule(anonymousSid, PipeAccessRights.FullControl, AccessControlType.Deny));

		return pipeSecurity;
	}
}
