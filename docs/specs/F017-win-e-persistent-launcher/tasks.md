# F017 — Interceptador Global Ultraleve, Persistente do Win+E e Instalador Modular — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| **T1** | Criar projeto nativo C++ `Files.HotkeyService` com registro de `RegisterHotKey(MOD_WIN, 'E')` e ativação pelo protocolo `files-dev:` | FR-001, FR-002, FR-003, AC-1, AC-2 | `Win + E` com o Files fechado abre o Files; com o Files aberto ganha foco; binário < 150 KB | implementada em 12/10/2026 (hook WH_KEYBOARD_LL, D-021); AC-1/AC-2 OBSERVED por simulação, físico NOT TESTED |
| **T2** | Implementar `Files.Watchdog` com supervisão mútua em < 1 segundo e evento de desligamento cooperativo | FR-004, AC-3 | `taskkill /F /IM Files.HotkeyService.exe` faz o processo reaparecer em ~200 ms | implementada; AC-3 OBSERVED: reaparece em 228/242 ms (`tools/perf/f017_win_e_test.ps1`) |
| **T3** | Estrutura de empacotamento modular e script Inno Setup (`FilesCustomSetup.iss`) instalando em `%LocalAppData%\Programs\FilesCustom\` com subpasta isolada de dependências | FR-005, US5 | Instalador instala pasta isolada sem poluir o Windows; desinstalador remove tudo | pendente |
| **T4** | Gerador de manifesto SHA256 (`package_manifest.json`) e lógica de Atualizador Inteligente (Delta Update vs Pacote Completo + fechamento seguro de processos) | FR-006, AC-6 | Atualizador baixa pacote reduzido se as dependências já existirem e substitui binários sem travar | pendente |
| **T5** | Desinstalação completa e reversão do Win+E ao Windows Explorer | FR-007, AC-7 | Desinstalação remove 100% dos arquivos e registros; `Win + E` volta imediatamente ao Explorer nativo | pendente |
| **T6** | Medição de desempenho, profiling de boot e auditoria das regressões de velocidade da UI | NFR-001 a NFR-005, AC-8 | Métricas de cold-start, RAM e tempos de navegação aferidos e registrados com scripts de `tools/perf/` | pendente |
