# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F015 (índice de busca próprio): T3–T12 implementados (Modo Básico ponta a ponta); F017 T1–T5 concluídos; F019 T1–T5 concluídos
CURRENT TASK   Capturas visuais na sessão interativa desbloqueada (F015 T11/T12), cliques UAC do Alexandre (F015 T1, T2a), F017 T6 (medições de boot/RAM/UI) e F015 T13–T16 (Modo Completo)
ACCEPTANCE     docs/specs/F015-indice-de-busca/tasks.md, coluna "Pronto quando"
DO NOT CHANGE  Fora dos arquivos listados no plan.md da F015
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Antigravity (F017 T6, F015 T11/T12 capturas + T13–T18); Claude revisa; Alexandre para cliques UAC (T1, T2a) e inspeção física (F019 T5, F017 Win+E físico)
CURRENT BRANCH feature/f015-indice-de-busca
LAST DECISION  D-019: índice próprio em dois modos; D-020 (PENDING teste elevado); D-021 (hook WH_KEYBOARD_LL)
NEXT ACTION    Com a tela desbloqueada: capturar telas de F015 T11/T12 e rodar F017 T6; para Modo Completo (T13+), aguardar T1/T2a com UAC do Alexandre ou iniciar MftReader (T13)
BLOCKER        Sessão do Windows bloqueada durante a madrugada impede janelas WinUI/keybd_event interativas; UAC para T1 e T2a depende do Alexandre
SIDE WORK      F019 (efeito visível das 10 formas de controle/aba): T1–T5 implementadas e comitadas (68a890033), 10 PNGs gerados em %TEMP%\claude\desenv\utils\FilesApp\f019\ aguardando aprovação visual do Alexandre (T5)
F017 WIN+E     T1–T5 concluídas (7ce6b0309): instalador Inno Setup modular (64,82 MB, 1 DLL na raiz + 341 em Dependencies\), Delta Updater (2,23 MB < 20 MB), desinstalação 100% limpa e reversão do Win+E ao Explorer comprovadas (tools/perf/f017_installer_test.ps1). Falta T6 (profiling) e teste físico do Win+E pelo Alexandre
LAST UPDATE    2026-10-13 — Antigravity (F017 T3–T5 + F015 T7–T12 comitados)
```
