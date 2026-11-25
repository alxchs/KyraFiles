# F039 — O que está ocupando meu disco — TASKS

Status: RASCUNHO (aguardando aprovação para iniciar implementação).

Regras: seguir `plan.md`; build Release x64 sem erros; manter gate F021 (`verify_no_index_service.ps1`) e `check-legacy-names.ps1` verdes; nunca baixar arquivos OneDrive "só na nuvem".

- [ ] **T1 — Motor de análise `DiskUsageScanner`.** Implementar varredura incremental (reaproveitando `VolumeIndex` quando disponível ou `FileIdBothDirectoryInfo` via `DirectoryScanner`), ignorando junctions/symlinks e arquivos offline/OneDrive, contabilizando pastas sem permissão como "Inacessível" e cancelando em ≤ 1 s. Atende: FR-001, FR-002, AC-1, AC-2, AC-6, AC-7.
- [ ] **T2 — ViewModel hierárquico e filtros.** Implementar `DiskUsageViewModel` com agregação compacta em memória, navegação por nível (Entrar/Voltar), filtros por tipo (vídeos, imagens, instaladores, `node_modules`) e idade, e reconciliação com espaço usado da unidade. Atende: US1, FR-003, NFR-001.
- [ ] **T3 — Tela P1 (Lista proporcional e ações).** Criar `DiskUsagePage.xaml(.cs)` e ação de entrada no app, com barras proporcionais, "Mostrar no local" e "Excluir" para a Lixeira com atualização incremental do total sem reescanear. Atende: US1, US3, AC-3, AC-4, AC-5.
- [ ] **T4 — Visualização P2 (Mapa de blocos / Treemap).** Adicionar controle de mapa de blocos proporcional sobre o mesmo `DiskUsageViewModel` com clique para navegar. Atende: US2, AC-3.
- [ ] **T5 — Medição NFR-001, validação de ACs e fechamento.** Medir tempo até top 20 (≤ 5 s), cancelamento (≤ 1 s) e memória em unidade grande; validar ACs 1–7, rodar gate F021 e `check-legacy-names.ps1`, atualizar `STATUS.md` e `AGENT_CONTEXT.md`. Atende: NFR-001.
