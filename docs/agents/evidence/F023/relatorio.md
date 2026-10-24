# F023 — Relatório de Estresse de Memória, Robustez e Validação Visual da Busca (24/10/2026, AGY)

## 1. Resumo Executivo
- **Objetivo:** Validar a estabilidade de recursos (memória, vazamento de kernel handles) sob estresse contínuo de busca híbrida com streaming, descarte de CTS (`CancellationTokenSource`) no Omnibar, e coletar evidências visuais físicas de funcionamento sem travamentos.
- **Ambiente:** Windows 11 x64, build Release net10.0-windows10.0.26100.0, layout registrado em AppX.
- **Resultado:** **CONFIRMED**. Bateria de 15 buscas consecutivas executada com sucesso absoluto. Zero travamentos, zero vazamento de handles (delta de handles estável e decrescente após GC), e resolução definitiva do bug crítico de Access Violation no Escape do Omnibar.

---

## 2. Correções Críticas Implementadas

### A. Correção de Access Violation no Escape do Omnibar (`NavigationToolbar.xaml.cs`)
- **Sintoma:** Ao pressionar `Escape` para fechar o Omnibar e retornar o foco para a lista de arquivos, ocorria crash com código de exceção `0xc0000005` (Access Violation) em `Microsoft.UI.Xaml.dll` no deslocamento `0x000000000009450e`.
- **Causa Raiz:** O evento `Omnibar_PreviewKeyDown` tratava o `VirtualKey.Escape` alternando o foco (`activePane.FileList?.Focus(FocusState.Programmatic)`), mas omitia `e.Handled = true`. Com isso, a tecla borbulhava para o `AutoSuggestBox` nativo da WinUI 3, que tentava fechar sua popup/flyout interna durante o descarte de foco, acessando ponteiro inválido.
- **Solução:** Adicionado `e.Handled = true;` imediatamente após tratar a tecla de Escape.

### B. Descarte Seguro de CTS de Sugestões de Busca (`NavigationToolbarViewModel.cs`)
- **Causa Raiz:** As instâncias de `_suggestSearchCTS` eram canceladas via `.Cancel()`, porém não eram descartadas via `.Dispose()`, retendo handles de sincronização no kernel do Windows a cada dígito digitado.
- **Solução:** Adicionado descarte explícito determinístico `_suggestSearchCTS?.Dispose()` antes de criar novas instâncias e no fechamento da pesquisa.

---

## 3. Bateria de Teste de Estresse de Memória (T11)
- **Script:** `tools/perf/verify_f023_stress.ps1 -Cycles 15`
- **Metodologia:** 15 ciclos ininterruptos com termos variados (`backup`, `txt`, `relatorio`, `log`, `config`, etc.), disparo via `Ctrl+F` + `{ENTER}`, cancelamento e descarte imediato via `Escape`.
- **Resultados Medidos:**
  - **Estado Inicial (PID 34148):**
    - Handles: 3750
    - Working Set: 434.71 MB
    - Private Memory: 320.74 MB
  - **Ciclo 1 (`backup`):**
    - Handles: 3940 | WorkingSet: 517.1 MB | Private: 407.1 MB
  - **Ciclo 8 (`test`):**
    - Handles: 3940 (estabilidade perfeita, 0 aumento) | WorkingSet: 555.3 MB | Private: 441.6 MB
  - **Ciclo 15 (`config`):**
    - Handles: 3933 (redução líquida de 7 handles em relação ao ciclo 1) | WorkingSet: 557.6 MB | Private: 438.3 MB
  - **Após GC e Repouso:**
    - Handles: 3886 (Delta líquido de apenas 136 handles contra o estado inicial recém-aberto, sem vazamento progressivo)
    - Private Memory: 442.68 MB (Delta controlado de buffers em cache do WinUI)
    - Crash / Exceções: **0**

---

## 4. Evidências Visuais Físicas (T13)
As capturas de tela foram geradas pelo script de automação com o aplicativo aberto fisicamente na sessão interativa:
1. `f023_busca_estresse_concluida.png`: Demonstra a janela do Files após a conclusão dos 15 ciclos de busca, interface íntegra, sem congelamento e pronta para uso.
2. `f023_busca_resultados_ativos.png`: Demonstra a exibição imediata dos resultados de busca no Omnibar e lista de resultados.

---

## 5. Portão Anti-Daemon F021 (T10)
- **Script:** `tools/perf/verify_no_index_service.ps1`
- **Resultado:** **CONFIRMED**. `Files.App.IndexService.exe` permaneceu rigorosamente inativo e desarmado. Exit code 0.

---

## 6. Registro Arquitetural das Lições do Everything (T14)
- **Documento:** `docs/specs/F023-hybrid-fast-search/licoes_everything.md`
- **Conteúdo:** Análise completa da engenharia do Voidtools Everything (leitura de MFT/USN Journal, arrays contíguos em RAM, busca SIMD, IPC via memória compartilhada) para subsidiar o app independente planejado pelo Alexandre.
