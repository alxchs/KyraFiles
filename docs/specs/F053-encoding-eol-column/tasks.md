# F053 — Codificação e Fim de Linha — TASKS

Status: CONCLUÍDO. Branch: `feature/f053-encoding-eol`.

| # | Tarefa | Atende | Estado |
|---|---|---|---|
| T1 | **Motor de Análise (`TextEncodingHelper`):** Detecção de encoding (UTF-8, UTF-8 BOM, UTF-16, ANSI) e quebras de linha (CRLF, LF, Misto) em streaming com buffer de 4 KB e cache. | FR-001, NFR-001, AC-1, AC-2 | **concluída** |
| T2 | **Motor de Conversão e Desfazer (`BatchTextConverter`):** Conversão atômica via temp file, preservação de conteúdo textual, suporte a desfazer e tratamento de arquivos bloqueados/não-texto. | FR-002, AC-3, AC-4, AC-5, AC-7 | **concluída** |
| T3 | **Integração nas Colunas (`ExplorerPropertyService`):** Registro de `FilesCustom.Text.Encoding` e `FilesCustom.Text.LineEnding`. | US1, AC-1, AC-6 | **concluída** |
| T4 | **Ações de Menu de Contexto:** `ConvertToCrlfAction` e `ConvertToLfAction` integradas em `ContentPageContextFlyoutFactory.cs`. | US2, AC-3 | **concluída** |
| T5 | **Suíte de Testes Formais:** `tests/test-f053-all-criteria.ps1` cobrindo AC-1 a AC-7 + NFR-001 (8/8 PASS). | Todos os ACs | **concluída** |
| T6 | **Gate F021 e Relatório:** Verificação mandatória F021 e emissão de `docs/agents/evidence/F053/RELATORIO.md` e `resultado.csv`. | Gate F021 | **concluída** |
