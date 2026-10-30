# Handoff AGY → Claude: Conclusão da F063, Build Parcial 4.3.9 e Roteiro de Memória Ativa (P0)

**Data:** 29/10/2026 21:00  
**Autor:** Antigravity (AGY)  
**Destinatário:** Claude  
**Branch Base:** `colab` / `main` / `custom/main` (sincronizadas no commit `bda243fd9`)  
**Status Atual:** F063 100% concluída, testada e integrada; Pacote e instalador 4.3.9 gerados.

---

## 1. O que foi feito e entregue (Spec F063)

### 1.1 Objetivo e Implementação
A spec **F063** atacou o vazamento/retenção de Working Set físico em repouso e segundo plano (Prioridade Zero negociada com o Alexandre):
* **`src/Files.App/Helpers/MemoryTrimHelper.cs`:**
  * Implementa `ScheduleTrim(delayMs: 2000)` com debounce via `CancellationTokenSource`.
  * Executa `PInvoke.K32EmptyWorkingSet` de forma segura em background (`Task.Run`).
  * Inclui `CancelPendingTrim()` chamado imediatamente ao restaurar ou focar a janela, evitando disputa de recursos.
  * Inclui trava de segurança: `FileOperationsHelpers.WaitForCompletion()` para **nunca** rodar trim durante cópias, movimentações ou compressões ativas de arquivos.
* **`src/Files.App/MainWindow.xaml.cs`:**
  * Evento `AppWindow.Changed`: detecta `OverlappedPresenterState.Minimized` e agenda o trim de 2s.
  * Evento `Activated`: cancela trims pendentes ao receber foco e agenda trim de repouso longo (60s) se for desativada sem ser minimizada.
* **`src/Files.App/App.xaml.cs`:**
  * Atualizado `TryEmptyWorkingSetWhenIdle` para consumir o `MemoryTrimHelper`.

### 1.2 Evidências e Medições Físicas (CONFIRMED)
Ferramenta criada: [`tools/perf/measure_memory_and_latency.ps1`](file:///c:/desenv/utils/FilesApp/tools/perf/measure_memory_and_latency.ps1) (enumera janelas desktop `WinUIDesktopWin32WindowClass` via API nativa de Desktop):
* **Baseline anterior (4.3.8 sem trim):**
  * Working Set ativo: 245.12 MB
  * Working Set minimizado: 246.25 MB (0% de redução, retenção total de memória pelo DWM/DirectX).
* **F063 Medido (3 rodadas físicas comprovadas):**
  * **Minimização:** Working Set despenca de 311 MB / 246 MB para **3.61 MB – 4.47 MB** (**~98.5% de redução**).
  * **Latência de Restauração:** Medida entre **38 ms e 46 ms** (estritamente abaixo do teto de 50 ms). Como as páginas ficam na Standby List do Windows, a recuperação ocorre por *soft page faults* em RAM, com **zero-jank** e sem acesso a disco.
  * **Gate F021:** 0 instâncias de `IndexService`, CPU em repouso de 3.33% (< 5%).
  * **Testes Automatizados:** 73/73 testes passando em `Files.App.Search.Index.Tests`.

---

## 2. Build Parcial 4.3.9 e Instalador

A pedido do Alexandre ("Quero o parcial da final 39"):
* **Versão Bump:** `4.3.9.0` em `src/Files.App/Package.appxmanifest` e `installer/FilesCustomSetup.iss`.
* **Compilação Release:** Realizada sem erros e sem novos warnings (`dotnet build src\Files.App\Files.App.csproj -c Release -p:Platform=x64`).
* **Layout AppX:** Montado e registrado via `.\Open-FilesDev.ps1 -Configuration Release -NoLaunch`.
* **Manifesto SHA256:** `installer/package_manifest.json` atualizado com 1.333 arquivos (212,08 MB).
* **Instalador Inno Setup:**
  * Arquivo: [`artifacts\Setup\FilesCustomSetup-4.3.9.exe`](file:///c:/desenv/utils/FilesApp/artifacts/Setup/FilesCustomSetup-4.3.9.exe)
  * Tamanho: **52.686.633 bytes** (50,24 MB — 1,5 KB menor que a versão 4.3.8).
  * SHA256: `E31293B803FBDCFCC8120995635443D59E526A06BD42F5735BCA3DA59514AFBF`.
* **Notas de Liberação:**
  * Markdown: [`docs/releases/2026-10-29_build-FilesCustomSetup-4.3.9.md`](file:///c:/desenv/utils/FilesApp/docs/releases/2026-10-29_build-FilesCustomSetup-4.3.9.md)
  * HTML: [`docs/releases/2026-10-29_build-FilesCustomSetup-4.3.9.html`](file:///c:/desenv/utils/FilesApp/docs/releases/2026-10-29_build-FilesCustomSetup-4.3.9.html)

---

## 3. Diagnóstico do Alexandre e Próximo Desafio (Memória Ativa)

O Alexandre enviou uma captura de tela do diálogo **"Ecosystem Memory Usage"** (criado na F062 em Configurações → Sobre):
* `Files` (PID 96340): **540.7 MB** (Privada: 417.9 MB)
* `Files.App.Server`: 0 MB
* `Files.HotkeyService`: 6.4 MB (Privada: 1.0 MB)
* `Files.Watchdog`: 5.2 MB (Privada: 856 KB)
* `Files.App.IndexService`: 0 MB

### Análise Técnica para a Claude
1. **O que gerou os 540 MB:**
   * A sessão do print era da versão 4.3.8 instalada (antes da F063).
   * No WinUI 3, ao abrir **Configurações (`Settings/AboutPage`)**, a árvore visual da página de navegação anterior (pastas, itens de lista, miniaturas) e da nova página de configurações permanecem vivas simultaneamente no heap do GC e no heap nativo da Microsoft Composition / DirectX.
2. **Oportunidade da F064 (Memória Ativa / Tab & Page Unloading):**
   * A F063 resolveu o repouso e a minimização (4 MB).
   * A **F064** deve focar na memória enquanto o app está **com a janela aberta e em uso**:
     * **Tab Suspender / Unloading de Abas Inativas:** Se houver mais de uma aba aberta, descarregar a árvore visual XAML da aba secundária, preservando seu ViewModel/estado leve em memória. Ao clicar na aba, o XAML é reconstituído instantaneamente.
     * **Page Cache Clean no Frame:** Quando o usuário fecha ou sai da tela de Configurações, garantir que o `Frame.BackStack` ou o cache da página descarte referências pesadas e chame `GC.Collect(1)` debouncado.
     * **Redução esperada:** Trazer a memória ativa com abas/configurações de ~500 MB para o intervalo de **150 MB a 250 MB**.

---

## 4. Regras Soberanas do Alexandre (METODO.md §18 — 29/10/2026)

Relembrando o ciclo mandatório de desenvolvimento que deve ser seguido rigidamente pela AGY e pela Claude:
1. **Uma branch por spec:** Toda branch deve sair exclusivamente da `main` / `colab` atualizada (`feature/<spec-id>`).
2. **`colab` atualizada antes de iniciar:** Nunca comece uma spec sem a `colab` alinhada com a última entrega.
3. **Roteiro de testes obrigatório:** Executar testes reais e medições comprovadas com comandos e saídas.
4. **Performance Zero Regressão:** Em hipótese alguma devolver na `colab` com desempenho pior que o baseline (sem perda de latência, sem novos travamentos).
5. **Devolução na `colab` via Fast-Forward:** Branch finalizada com commits agrupados (squash/rebase) e integrada na `colab` antes de iniciar a próxima.
6. **Relatório de Conclusão Mandatório:** Documento com evidências, métricas e advertências técnicas ao término de cada spec.
7. **NÃO ROUBAR O FOCO:** Nunca rodar scripts que abram janelas interativas ou roubem o foco do Alexandre enquanto ele trabalha. Ao usar `Open-FilesDev.ps1`, passe sempre `-NoLaunch`.
