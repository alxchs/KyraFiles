# F054 — Prévia técnica — TASKS

Status: APROVADO (Claude, 25/11/2026). Seguir também os "Ajustes da revisão" do `plan.md`.

Regras: seguir `plan.md`; build Release x64 sem erros; medir NFR-001 (≤ 300 ms para 1 MB); manter gate F021 (`verify_no_index_service.ps1`) e `check-legacy-names.ps1` verdes; temporários em `%TEMP%\agy\<GUID>\desenv\utils\KyraFiles\Files\`.

- [ ] **T0 — Medição baseline da prévia atual.** Medir tempo de abertura e memória de `CodePreview` e `TextPreview` para arquivos `.cs`, `.json`, `.csv` e `.log` (1 MB e 200 MB) e registrar em `docs/agents/evidence/F054/baseline.md`. Atende: ponto em aberto §8, NFR-001.
  - _(Claude, 27/11/2026: script `tools/perf/f054_preview_baseline.ps1` pronto (UIA, janela mostrada sem ativar, sem teclas). **Impedida:** o KyraFiles do Alexandre estava aberto (10:09), e medir exige uma instância própria; rodar com o app fechado. A 4.3.33 instalada não muda com o trabalho na branch, então a ordem com T2 não altera o baseline.)_
- [~] **T1 — Realce de sintaxe estendido e numeração de linha.** _(Claude, 27/11/2026: código pronto e 5 testes unitários verdes; tela NOT TESTED, prova visual fica na T5. Inclui `.json/.jsonc`, `.yaml/.yml`, `.ini/.toml/.cfg/.conf`, `.sh/.bash/.zsh`, `.sql`, `.py`; paleta sem vermelho; e a leitura da prévia passa a parar no limite em vez de ler o arquivo inteiro, adiantando parte da T4/FR-002.)_ Atualizar `CodePreviewViewModel.cs` (`.json`, `.yaml`/`.yml`, `.toml`, `.sql`, `.sh`) e `CodePreview.xaml(.cs)` com coluna de número de linha e alternância claro/escuro. Atende: US1, AC-1, FR-003.
- [ ] **T2 — Prévia em árvore para JSON/XML/YAML.** Criar `StructuredTreePreviewViewModel` e `StructuredTreePreview` com nós recolhíveis e exibição de linha/coluna em erro de sintaxe. Atende: US2, AC-2, AC-3.
  - [FEITO] (commit desta tarefa) T2a · claude · 2026-11-27 · branch feature/F054-technical-preview · pronto = montador de árvore JSON/XML com limite de nós, cancelamento e linha/coluna do erro, com testes · NÃO inclui = tela, YAML · `StructuredTreeBuilder` + 10 testes verdes; build Release 0 erros, sem aviso nos arquivos tocados
  - [LIVRE] T2b · depende de T2a · pronto = `StructuredTreePreview` (TreeView) ligado no `InfoPaneViewModel` com alternância Texto ↔ Árvore
  - [LIVRE] T2c · depende de decisão (biblioteca YamlDotNet compatível com AOT ou leitor próprio) · pronto = árvore de YAML
- [ ] **T3 — Prévia tabular virtualizada para CSV.** Criar `CsvPreviewViewModel` e `CsvPreview` com leitura sob demanda para 100 mil linhas sem carregar tudo em RAM. Atende: US3, AC-4.
- [ ] **T4 — Modo acompanhar (`tail -f`), limite de arquivo e cancelamento rápido.** Atualizar `TextPreviewViewModel.cs` e `InfoPaneViewModel.cs` para `FileShare.ReadWrite | FileShare.Delete`, leitura limitada em arquivos > limite, botão "Acompanhar" e cancelamento de prévias intermediárias. Atende: US4, AC-5, AC-6, AC-7, FR-001, FR-002.
- [ ] **T5 — Validação de performance (NFR-001) e fechamento.** Medir tempo de exibição (≤ 300 ms em 1 MB) e uso de RAM; validar ACs 1–7, rodar gate F021 e `check-legacy-names.ps1`, atualizar `STATUS.md` e `AGENT_CONTEXT.md`. Atende: NFR-001.
