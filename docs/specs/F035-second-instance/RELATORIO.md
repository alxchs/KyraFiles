# Relatório de Conclusão e Aceite: SPEC-0031 (F035 — Segunda instância e abertura direta)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 5 pts  
**Evidências:** `docs/agents/evidence/F035/` (`resultado.md`, `t1_controle_negativo.csv`, `resultado-20261026-0054.csv`) e commit `465363f9e` (v4.3.31)

---

## 1. Escopo e Implementação
Resolução definitiva dos problemas de segunda instância, ativação e inicialização:
- Tratamento e proteção contra `SemaphoreFullException` no pool de instâncias.
- Remoção do semáforo redundante que encerrava silenciosamente o processo com `Environment.Exit(0)` no construtor estático antes de `Main()`.
- Validação estrita de processo ativo (`IsInstanceProcessAlive`) antes de qualquer redirecionamento, limpando chaves órfãs (`INSTANCE_ACTIVE`) e evitando mortes silenciosas de novas instâncias.
- Roteamento correto de atalhos pelo pacote WinUI 3 sem erros `REGDB_E_CLASSNOTREG`.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | 5 aberturas com menos de 1s: sem mortes ou exceções | `CONFIRMED` | 20 rodadas aprovadas sem evento 1026 |
| **AC-2** | Files fechado: 2 aberturas abrem 1 janela com as pastas | `CONFIRMED` | Comprovado no teste de ativação |
| **AC-3** | Diálogo de arquivo ativo permanece isolado | `CONFIRMED` | `IsFileDialogLaunch()` preservado |
| **AC-4** | Abertura do executável não falha por registro COM | `CONFIRMED` | Pacote registrado adequadamente |
| **AC-5** | Janela em segundo plano é ativada ou reabre com liveness check | `CONFIRMED` | `465363f9e` + deploy v4.3.31 |
