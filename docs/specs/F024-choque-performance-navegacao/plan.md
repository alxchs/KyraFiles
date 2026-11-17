# F024 — Plano de Arquitetura e Implementação (Choque de Performance)

## 1. Diagnóstico Técnico dos Gargalos

### 1.1 O gargalo de carga de 1.600 arquivos (`ApplyFilesAndFoldersChangesAsync`)
- **Problema:** Durante a enumeração de pastas com muitos arquivos, `ShellViewModel` dispara múltiplos lotes intermediários (`intermediateAction`). A cada lote, é chamado `FilesAndFolders.Clear()` seguido de `FilesAndFolders.AddRange(...)` e `FilesAndFolders.EndBulkOperation()`.
- **Efeito catastrófico no WinUI 3:** O WinUI 3 interpreta o evento `Reset` da coleção destruindo **todos os contêineres visuais já criados na tela** e reconstruindo a lista inteira do zero repetidas vezes. O usuário percebe travamento, tela congelada e teclas que não respondem.
- **Solução:** 
  1. Primeiro lote ultra-rápido (primeiros 64 itens) renderizado imediatamente para resposta visual instantânea.
  2. Carregamento dos itens restantes em segundo plano em lote consolidado único, sem thrashear a UI com múltiplos `Reset`s.

### 1.2 O gargalo de navegação por teclado (`Home`, `End`, `PgUp`, `PgDn`, `Ctrl`)
- **Problema:** As teclas `Home`, `End`, `PgUp`, `PgDn` não possuem tratamento direto em `FileList_PreviewKeyDown` e caem no pipeline genérico do `ListView` do WinUI 3. Com 1.600 itens, o WinUI 3 tenta realizar medições e passes de layout de contêineres virtuais em saltos longos.
- **Solução:** Tratar explicitamente `Home`, `End`, `PageUp`, `PageDown` (e suas combinações com `Ctrl` e `Shift`) em `FileList_PreviewKeyDown`:
  - `Ctrl + Home`: Seleciona e dá scroll direto para o índice 0.
  - `Ctrl + End`: Seleciona e dá scroll direto para o último índice (`Count - 1`).
  - `PageUp` / `PageDown`: Salta por página baseado no número de itens visíveis calculados pela altura da viewport.
  - Execução imediata via `ScrollIntoView` nativo com `ScrollIntoViewAlignment.Leading` / `Default`.

### 1.3 O gargalo do FileSystemWatcher (Sincronismo Imediato sem Recarregar Tudo)
- **Problema:** Em `DirectoryWatcher_Changed`, qualquer evento de arquivo disparava `RefreshItems(null)`, forçando uma releitura completa do disco e destruindo a lista em exibição.
- **Solução:**
  - `Created`: Localiza a posição ordenada correta e insere cirurgicamente o novo item via `Insert` na coleção (ou na fila do `ProcessOperationQueueAsync`), sem recarregar a pasta.
  - `Deleted`: Remove cirurgicamente o `ListedItem` correspondente pelo caminho.
  - `Renamed`: Atualiza o nome, extensão e caminho do item existente in-place.
  - `Changed`: Atualiza propriedades de tamanho/data do item existente.
  - Atualização com prioridade adequada no `DispatcherQueue` para resposta imediata ao usuário (< 100 ms).

### 1.4 Redução da CPU Ociosa de 15% para ≤ 2%
- Forçar suspensão do compositor para abas que não estão em exibição (`Visibility = Collapsed`).
- Garantir que animações e storyboards da barra de status ou timers de widgets não fiquem pulsando no WinUI 3.

---

## 2. Estrutura dos Testes Automatizados e Higiene
1. Criar `C:\FilesUXLab\perf\1600` via script automatizado `tools\perf\setup_1600_lab.ps1` contendo exatamente 1.600 arquivos reais com metadados variados.
2. Criar `tools\perf\f024_keyboard_nav_test.ps1`:
   - Envia `Ctrl+End`, mede o tempo até o foco e seleção atingirem o último item.
   - Envia `Ctrl+Home`, mede o tempo de retorno.
   - Envia sequências de `PgDn` e `PgUp`.
3. Criar `tools\perf\f024_watcher_sync_test.ps1`:
   - Cria 5 arquivos em paralelo na pasta enquanto o KyraFiles está aberto.
   - Verifica se os 5 arquivos aparecem na lista sem congelar a janela.
   - Remove os 5 arquivos e verifica a desaparição imediata.
4. Script de encerramento e higiene estrita `tools\perf\cleanup_f024_lab.ps1` que remove pasta e scripts, comprovando exclusão via `Test-Path`.

