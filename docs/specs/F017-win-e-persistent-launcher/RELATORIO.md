# Relatório de Conclusão e Aceite: SPEC-0014 (F017 — Interceptador Global Ultraleve do Win+E e Instalador Modular)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 5 pts  
**Evidências:** `KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`, scripts `tools/perf/f017_*.ps1` e nota `2026-10-15_0205_build-KyraFilesSetup-4.2.37-baseline.md`

---

## 1. Escopo e Implementação
Criação do subsistema de interceptação global persistente do `Win + E`:
- Projeto nativo C++ `KyraFiles.HotkeyService` com hook de baixo nível (`WH_KEYBOARD_LL`) consumindo < 2 MB de RAM e < 1 ms de CPU.
- Processo de supervisão mútua `KyraFiles.Watchdog` com recuperação em ~240 ms após finalização forçada.
- Roteamento transparente: se fechado, abre o KyraFiles; se aberto, traz a janela ativa para o primeiro plano.
- Instalador modular via Inno Setup com desinstalação limpa que reverte o `Win + E` ao Windows Explorer nativo.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1 / AC-2** | Win+E abre o KyraFiles ou traz janela existente ao topo | `CONFIRMED` | Hook em `KyraFiles.HotkeyService.exe` |
| **AC-3** | Recuperação ultrarrápida do processo Hotkey pós-kill | `CONFIRMED` | Recuperado em 228/242 ms pelo Watchdog |
| **AC-4 a AC-7** | Instalador modular, isolado e desinstalação que restaura o Explorer | `CONFIRMED` | `unins000.exe` remove chaves e restaura Explorer |
| **AC-8** | Consumo de RAM < 3 MB e boot em milissegundos | `CONFIRMED` | 1,66 MB total medido em `f017_perf_audit.ps1` |
