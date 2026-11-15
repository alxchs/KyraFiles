# F093 — Compilação e Deploy Otimizados — TASKS

Status: **EM ANDAMENTO** em 14/11/2026.
Branch: `feature/f093-fast-compile-and-deploy`, saindo da `main` atualizada.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| **T0** | Ajustar `installer/KyraFilesSetup.iss` com suporte a `#ifdef FAST_COMPRESSION` | AC-1 | Inno Setup compila em modo rápido e ultra | concluído |
| **T1** | Implementar `tools/Quick-Ship.ps1` com modos `-Fast` e `-Full` | AC-1, AC-2, AC-3 | script executa compilação, instalador e deploy | concluído |
| **T2** | Testar e validar execução do modo `-Fast` com medição de tempo | AC-1 | tempo total inferior a 2 minutos | concluído |
| **T3** | Validar gate F021 e produzir `RELATORIO.md` | AC-4 | `verify_no_index_service.ps1` exit 0 e relatório gerado | concluído |
