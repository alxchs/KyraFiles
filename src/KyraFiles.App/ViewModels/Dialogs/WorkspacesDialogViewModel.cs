using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KyraFiles.App.Data.Contracts;
using KyraFiles.Shared.Workspaces;

namespace KyraFiles.App.ViewModels.Dialogs
{
    public sealed partial class WorkspacesDialogViewModel : ObservableObject
    {
        private readonly INamedWorkspacesService _workspacesService;

        [ObservableProperty]
        private string _newWorkspaceName = string.Empty;

        [ObservableProperty]
        private NamedWorkspace? _selectedWorkspace;

        public bool HasSelectedWorkspace => SelectedWorkspace != null;

        partial void OnSelectedWorkspaceChanged(NamedWorkspace? value)
        {
            OnPropertyChanged(nameof(HasSelectedWorkspace));
        }

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasStatusMessage;

        public ObservableCollection<NamedWorkspace> Workspaces { get; } = new();

        public WorkspacesDialogViewModel()
        {
            _workspacesService = Ioc.Default.GetRequiredService<INamedWorkspacesService>();
            LoadWorkspaces();
        }

        public WorkspacesDialogViewModel(INamedWorkspacesService workspacesService)
        {
            _workspacesService = workspacesService ?? Ioc.Default.GetRequiredService<INamedWorkspacesService>();
            LoadWorkspaces();
        }

        public void LoadWorkspaces()
        {
            Workspaces.Clear();
            foreach (var ws in _workspacesService.GetWorkspaces())
            {
                Workspaces.Add(ws);
            }
        }

        [RelayCommand]
        public async Task SaveCurrentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewWorkspaceName))
            {
                StatusMessage = "Digite um nome para o espaço de trabalho.";
                HasStatusMessage = true;
                return;
            }

            var (success, error) = await _workspacesService.SaveCurrentAsWorkspaceAsync(NewWorkspaceName.Trim());
            if (success)
            {
                StatusMessage = $"Espaço de trabalho '{NewWorkspaceName}' salvo com sucesso!";
                HasStatusMessage = true;
                NewWorkspaceName = string.Empty;
                LoadWorkspaces();
            }
            else
            {
                StatusMessage = error;
                HasStatusMessage = true;
            }
        }

        [RelayCommand]
        public async Task LoadSelectedReplaceAsync()
        {
            if (SelectedWorkspace == null) return;

            var (success, error) = await _workspacesService.LoadWorkspaceAsync(SelectedWorkspace.Id, replaceExisting: true);
            if (success)
            {
                StatusMessage = string.IsNullOrEmpty(error) ? "Espaço de trabalho carregado." : error;
                HasStatusMessage = true;
            }
            else
            {
                StatusMessage = error;
                HasStatusMessage = true;
            }
        }

        [RelayCommand]
        public async Task LoadSelectedAppendAsync()
        {
            if (SelectedWorkspace == null) return;

            var (success, error) = await _workspacesService.LoadWorkspaceAsync(SelectedWorkspace.Id, replaceExisting: false);
            if (success)
            {
                StatusMessage = string.IsNullOrEmpty(error) ? "Abas adicionadas." : error;
                HasStatusMessage = true;
            }
            else
            {
                StatusMessage = error;
                HasStatusMessage = true;
            }
        }

        [RelayCommand]
        public void DeleteSelected()
        {
            if (SelectedWorkspace == null) return;

            string id = SelectedWorkspace.Id;
            if (_workspacesService.DeleteWorkspace(id))
            {
                StatusMessage = "Espaço de trabalho excluído.";
                HasStatusMessage = true;
                LoadWorkspaces();
            }
        }
    }
}
