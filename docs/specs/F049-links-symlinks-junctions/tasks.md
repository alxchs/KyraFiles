# F049 — Links simbólicos, junctions e hardlinks — TASKS

Status: CONCLUÍDO. Branch: `feature/f049-links`.

- [x] **T0 — Confirmar o plan no código (somente leitura).** O que `IsSymLink` marca de fato; onde ficam as colunas do layout de detalhes; como a enumeração devolve atributos (reparse point); onde a exclusão trata links; se `FolderSizeProvider` segue links; como junction será criada. Entregue `docs/agents/evidence/F049/t0.md`.
- [x] **T1 — Controle negativo de segurança (FR-003; AC-3, AC-6).** Escrita do teste de segurança comprovando que exclusão de reparse point e cálculo de tamanho não afetam nem duplicam destino.
- [x] **T2 — Criar links (FR-001; AC-1, AC-5).** Motor `LinkHelper.cs`, ação `CreateLinkAction` e diálogo com recuo explicado quando falta permissão.
- [x] **T3 — Mostrar e ler o destino (FR-002; AC-2, AC-4, NFR-001).** Destino resolvido via `.ResolveLinkTarget`, coluna "Destino do link" em `ExplorerPropertyService.cs`, link quebrado sem exceção.
- [x] **T4 — Excluir sem seguir e tamanho sem duplicar (FR-003; AC-3, AC-6).** `SafeDeleteLink` e `CachedSizeProvider` pulando reparse points.
- [x] **T5 — Suíte de Testes Formais:** `tests/test-f049-all-criteria.ps1` com validação de AC-1 a AC-6 e NFR-001.
- [x] **T6 — Gate F021 e Relatório:** Verificação mandatória F021 e emissão de `docs/agents/evidence/F049/RELATORIO.md` e `resultado.csv`.
- [x] **T7 — Fechamento.** Atualização de documentação, `production-control.json`, `FILA.md` e integração via fast-forward na `main`.
