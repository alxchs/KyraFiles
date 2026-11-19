using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Data.Parameters;
using KyraFiles.App.Helpers;
using KyraFiles.App.UserControls.TabBar;
using KyraFiles.App.ViewModels;
using KyraFiles.App.Views;
using KyraFiles.Shared.Workspaces;

namespace KyraFiles.App.Services
{
    public sealed class NamedWorkspacesService : INamedWorkspacesService
    {
        private readonly NamedWorkspaceStore _store;

        public NamedWorkspacesService()
        {
            _store = new NamedWorkspaceStore();
        }

        public NamedWorkspacesService(NamedWorkspaceStore store)
        {
            _store = store ?? new NamedWorkspaceStore();
        }

        public IReadOnlyList<NamedWorkspace> GetWorkspaces()
            => _store.GetAll();

        public NamedWorkspace? GetWorkspace(string id)
            => _store.GetById(id);

        public Task<(bool Success, string ErrorMessage)> SaveCurrentAsWorkspaceAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult((false, "O nome do espaço de trabalho não pode ser vazio."));

            var mainPageVm = Ioc.Default.GetService<MainPageViewModel>();
            if (MainPageViewModel.AppInstances.Count == 0)
                return Task.FromResult((false, "Nenhuma aba aberta para salvar como espaço de trabalho."));

            var tabs = new List<WorkspaceTabSnapshot>();
            for (int i = 0; i < MainPageViewModel.AppInstances.Count; i++)
            {
                var tab = MainPageViewModel.AppInstances[i];
                string leftPath = string.Empty;
                string? rightPath = null;
                bool isDual = false;

                if (tab.TabItemContent is ShellPanesPage shellPanes)
                {
                    leftPath = shellPanes.NavParamsLeft?.NavPath ?? string.Empty;
                    if (shellPanes.IsMultiPaneActive && shellPanes.NavParamsRight != null)
                    {
                        isDual = true;
                        rightPath = shellPanes.NavParamsRight.NavPath;
                    }
                }
                else if (tab.NavigationParameter is TabBarItemParameter param)
                {
                    leftPath = NavigationHelpers.ExtractNavigationPath(param.NavigationParameter) ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(leftPath))
                    leftPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                tabs.Add(new WorkspaceTabSnapshot(leftPath, rightPath, isDual, tab.IsPinned));
            }

            int selectedIndex = (mainPageVm?.SelectedTabItem != null)
                ? MainPageViewModel.AppInstances.IndexOf(mainPageVm.SelectedTabItem)
                : 0;
            if (selectedIndex < 0) selectedIndex = 0;

            var ws = new NamedWorkspace(
                Guid.NewGuid().ToString("N"),
                name.Trim(),
                tabs,
                selectedIndex
            );

            var res = _store.SaveOrUpdate(ws);
            return Task.FromResult(res);
        }

        public async Task<(bool Success, string ErrorMessage)> LoadWorkspaceAsync(string workspaceId, bool replaceExisting)
        {
            var ws = _store.GetById(workspaceId);
            if (ws == null)
                return (false, "Espaço de trabalho não encontrado.");

            if (ws.Tabs == null || ws.Tabs.Count == 0)
                return (false, "O espaço de trabalho selecionado não contém abas salvas.");

            var (valid, missing) = _store.ValidatePaths(ws);

            var mainPageVm = Ioc.Default.GetService<MainPageViewModel>();
            if (mainPageVm == null)
                return (false, "MainPageViewModel não disponível.");

            var oldTabs = replaceExisting ? MainPageViewModel.AppInstances.ToList() : new List<TabBarItem>();

            // 1. Abre a primeira aba do novo workspace para garantir que a janela nunca fique com 0 abas
            var firstTab = ws.Tabs[0];
            string safeFirstPath = Directory.Exists(firstTab.LeftPath)
                ? firstTab.LeftPath
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (firstTab.IsDualPane && !string.IsNullOrWhiteSpace(firstTab.RightPath) && Directory.Exists(firstTab.RightPath))
            {
                var paneArgs = new PaneNavigationArguments
                {
                    LeftPaneNavPathParam = safeFirstPath,
                    RightPaneNavPathParam = firstTab.RightPath
                };
                await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), paneArgs, switchToNewTab: true, isPinned: firstTab.IsPinned);
            }
            else
            {
                await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), safeFirstPath, switchToNewTab: true, isPinned: firstTab.IsPinned);
            }

            // 2. Se for modo "Substituir", agora pode fechar com segurança as abas antigas
            if (replaceExisting && oldTabs.Count > 0)
            {
                var mainPage = (MainWindow.Instance?.Content as Microsoft.UI.Xaml.Controls.Frame)?.Content as MainPage;
                foreach (var oldTab in oldTabs)
                {
                    try
                    {
                        oldTab.IsPinned = false; // Despina para permitir fechamento
                        if (mainPage != null)
                        {
                            mainPage.DetachTabContent(oldTab);
                        }
                        MainPageViewModel.AppInstances.Remove(oldTab);
                        oldTab.Unload();
                    }
                    catch { }
                }
            }

            // 3. Abre as abas restantes do workspace
            for (int i = 1; i < ws.Tabs.Count; i++)
            {
                var t = ws.Tabs[i];
                string safePath = Directory.Exists(t.LeftPath)
                    ? t.LeftPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (t.IsDualPane && !string.IsNullOrWhiteSpace(t.RightPath) && Directory.Exists(t.RightPath))
                {
                    var paneArgs = new PaneNavigationArguments
                    {
                        LeftPaneNavPathParam = safePath,
                        RightPaneNavPathParam = t.RightPath
                    };
                    await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), paneArgs, switchToNewTab: false, isPinned: t.IsPinned);
                }
                else
                {
                    await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), safePath, switchToNewTab: false, isPinned: t.IsPinned);
                }
            }

            string warning = string.Empty;
            if (missing.Count > 0)
            {
                warning = $"Aviso: {missing.Count} pasta(s) salva(s) não existem mais e foram abertas na pasta do usuário.";
            }

            return (true, warning);
        }

        public bool DeleteWorkspace(string id)
            => _store.Delete(id);
    }
}
