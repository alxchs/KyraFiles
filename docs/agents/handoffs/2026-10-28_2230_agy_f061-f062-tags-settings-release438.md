# Handoff: F061 + F062 + Correções Críticas (Tags e Settings) + Integração v4.3.8

- **Data/Hora:** 28/10/2026 ~22:35
- **Autor:** AGY (Antigravity)
- **Destinatário:** Claude / Alexandre
- **Branch Integrada:** `feature/release-4.3.8-integrated`
- **Commit na main / colab / custom/main:** `cd00407d3`
- **Push Remoto:** CONFIRMED (`origin/main`, `origin/colab`, `origin/custom/main` em `cd00407d3`)

---

## 1. Resumo Executivo das Demandas Atendidas

1. **Memória do Ecossistema no Sobre (F062):**
   - **Solicitação:** "Dentro do about quero uma opcao pa ver memoria que o files está consumindo, considerando todos os módulos."
   - **SDD Completo:** Criados `spec.md`, `plan.md` e `tasks.md` em `docs/specs/F062-diagnostico-memoria-ecossistema/`.
   - **Implementação:** Classe estática `EcosystemMemoryDiagnostics` em `src/Files.Shared/Helpers/EcosystemMemoryDiagnostics.cs`. Mede `WorkingSet64` e `PrivateMemorySize64` de todos os processos do ecossistema (`Files`, `Files.App.Server`, `Files.HotkeyService`, `Files.Watchdog`, `Files.App.IndexService`).
   - **Testes Unitários:** 6 testes em `tests/Files.App.Search.Index.Tests/EcosystemMemoryDiagnosticsTests.cs` cobrindo formatação de bytes, cultura invariante, captura de snapshot e tratamento de ausência. Resultado: `6/6 passaram`.
   - **UI:** Integrado em `AboutPage.xaml` e `AboutViewModel.cs` com `SettingsCard`, Flyout de detalhamento por processo, botão de copiar relatório para o clipboard e botão de atualização em tempo real.

2. **Causa-Raiz e Correção do Bug de Perda de Configurações:**
   - **Relato:** "Está perdendo minhas configurações toda vez que fecha e/ou compila um novo exe/instala novamente"
   - **Causa Raiz CONFIRMED:** No `installer/FilesCustomSetup.iss` (linha 89), o instalador executava `Remove-AppxPackage $p.PackageFullName` sem a flag `-PreserveApplicationData`. Ao desinstalar a identidade AppX anterior, o Windows apagava fisicamente `%LOCALAPPDATA%\Packages\FilesDev_ykqwq8d6ps0ag\LocalState\settings\user_settings.json`.
   - **Correção:** Modificado para evitar desinstalação caso o caminho já seja `{app}`, e, se necessário desinstalar registro anterior de outro caminho, passar obrigatoriamente `-PreserveApplicationData`. Configurações agora são rigorosamente preservadas entre instalações e atualizações.

3. **Causa-Raiz e Correção do Bug dos Filtros por Tag:**
   - **Relato:** "Todos os filtros por tag falhando, mostra um nome estranho do lado direito da tela quando clica no filtro; 'Search results for `tag:Home`', por exemplo"
   - **Causa Raiz CONFIRMED:** O `EverythingSearchProvider` e o `IndexedSearchProvider` interceptavam a query antes do `FolderSearch`. Como o `es.exe` (Everything) e o Windows Search não indexam o banco interno SQLite de tags do Files (`FileTags.db`), a busca retornava 0 itens. Além disso, a formatação do cabeçalho de busca gerava strings genéricas de AQS (`Search results for tag:...`).
   - **Correção:**
     - `EverythingSearchProvider.cs`: Adicionado bypass imediato para queries contendo `tag:`, delegando diretamente ao `FolderSearch`.
     - `ShellViewModel.cs`: Se for busca de tag, desativa o provider externo e usa `FolderSearch.SearchTagsAsync`, exibindo cabeçalho amigável `Tag: <Nome>` / `Tags: <Nome>`.
     - `NavigationToolbarViewModel.cs`: Desativa provider externo para sugestões de tags.
     - `BaseLayoutPage.cs`: Atualizado o PathBox para exibir `Tag: <Nome>` em vez de `Search results in Home for tag:...`.

4. **Saúde e Estabilidade do Win+E (F061):**
   - `verify_win_e_health.ps1` executado: 1 HotkeyService vivo, 1 Watchdog vivo, 0 processos terminados, mutex de instância ativo há mais de 4h46m contínuas sem qualquer reinício ou flood.

5. **Gate F021 e Higiene:**
   - `verify_no_index_service.ps1` executado: `Files.App.IndexService.exe` inativo, CPU em repouso medida em 3.33% (< 5%). Gate validado com sucesso.
   - Limpeza de `%TEMP%\Compiler` concluída.

6. **Squash e Integração:**
   - Commit squash atômico `cd00407d3` gerado sobre a base `caa4c57ee`.
   - Fast-forward aplicado para `main`, `colab` e `custom/main`.
   - `git push origin main colab custom/main feature/release-4.3.8-integrated` executado com sucesso.
   - Nenhuma tag de release publicada ainda (aguardando provas de tela conforme solicitado).

---

## 2. Evidências de Validação

### Testes Unitários F062
```text
Running tests from Files.App.Search.Index.Tests.dll (net10.0|x64)
Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 3s 444ms
```

### Compilação de Files.App
```text
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64
Build succeeded.
    25 Warning(s) (Baseline mantida)
    0 Error(s)
Time Elapsed 00:04:07.15
```

### Gate F021 (Contenção de Recursos)
```text
SUCESSO: Files.App.IndexService NAO esta rodando. Bloqueio F021 confirmado!
Files PID: 94192
RAM Working Set: 352.52 MB
CPU em repouso: 3.33 %
SUCESSO: CPU em repouso calmissima (< 5%).
TESTE F021 CONCLUIDO COM SUCESSO! GATE VALIDADO.
```

### Saúde do Win+E
```text
[OK] Win+E supervisionado e saudavel: 1 servico vivo, 1 Watchdog vivo, 0 terminados, mutex ativo (idade do servico: 00d 04h 46m 16s)
```
