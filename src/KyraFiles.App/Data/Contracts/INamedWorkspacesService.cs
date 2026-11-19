using System.Collections.Generic;
using System.Threading.Tasks;
using KyraFiles.Shared.Workspaces;

namespace KyraFiles.App.Data.Contracts
{
    public interface INamedWorkspacesService
    {
        IReadOnlyList<NamedWorkspace> GetWorkspaces();

        NamedWorkspace? GetWorkspace(string id);

        Task<(bool Success, string ErrorMessage)> SaveCurrentAsWorkspaceAsync(string name);

        Task<(bool Success, string ErrorMessage)> LoadWorkspaceAsync(string workspaceId, bool replaceExisting);

        bool DeleteWorkspace(string id);
    }
}
