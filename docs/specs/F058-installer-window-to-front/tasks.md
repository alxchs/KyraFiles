# F058 — TASKS

Status: rascunho, **bloqueadas** até a spec e o plan serem aprovados. Branch `feature/f058-installer-window-to-front`.

- [ ] **T0 — Descobrir como a atualização inicia o instalador (somente leitura).** Do clique em "Atualizar" até o `FilesCustomSetup-*.exe` aparecer: processo pai, parâmetros, se o pai já saiu; listar tudo em `docs/agents/evidence/F058/t0.md` com comando e saída (CONFIRMED/INFERRED).
- [ ] **T1 — Controle: medir o instalador atual (AC-1, AC-2).** Depois das 23:59: iniciar o instalador atual com outra janela em foco e registrar, só lendo `GetForegroundWindow`, onde ele aparece. `resultado.csv`. Esperado: reprova (é o defeito que o Alexandre relatou).
- [ ] **T2 — Primeiro plano no instalador (FR-001, FR-004).** Mudança no `.iss`; build do instalador (`ISCC`) sem aviso.
- [ ] **T3 — Quem inicia (FR-002; AC-2).** `AllowSetForegroundWindow` no ponto definido na T0.
- [ ] **T4 — Trava de 1 s e segunda instância (FR-003; AC-5, AC-8).**
- [ ] **T5 — Prova (AC-1 a AC-8).** Depois das 23:59, instalação em pasta de teste, sem tocar no app instalado; controle negativo (versão antiga reprova); relatório gerado do CSV.
- [ ] **T6 — Modelo.** Se algo mudar na spec, atualizar `~/.dev-method/templates/spec-instalador-janela-frente.md`. Fechamento: `STATUS.md`, `CHANGELOG.md`, handoff por AC; Claude revisa. Texto para o usuário sem jargão: "Corrigido: a janela de atualização agora aparece sempre na frente das outras."
