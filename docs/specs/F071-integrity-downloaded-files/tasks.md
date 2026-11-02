# F071 — Integridade e Arquivos Baixados — TASKS

Status: CONCLUÍDO (AGY, 01/11/2026). Backend-first concluído com testes unitários, Release compilado e validado. Branch: `feature/f071-integrity`.

> **Diretriz do Alexandre:** Backend primeiro dentro de cada spec (núcleo sem tela e testado com testes unitários, tela por último). Requisitos de tela ficam em `NOT TESTED` e não bloqueiam a entrega do núcleo.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T1 | **Motor de Hash com Streaming e Progresso (`FileIntegrityService`):** Implementar cálculo via `IncrementalHash` com buffer fixo de 128 KB, cancelamento cooperativo e normalizador de comparação de hashes. | FR-001, FR-002, FR-003, NFR-001, AC-1, AC-2, AC-3 | Build 0/0 e testes de hash de arquivos grandes passam | **concluído** |
| T2 | **Motor de Desbloqueio de Marca da Web em Lote (`ZoneIdentifierService`):** Implementar checagem e remoção segura de fluxo ADS `:Zone.Identifier` com retorno estruturado `BatchUnblockResult`. | FR-004, FR-005, AC-5, AC-7 | Build 0/0 e testes de remoção em lote passam | **concluído** |
| T3 | **Suíte de Testes Unitários de Integridade e Lote:** Criar testes em `FileIntegrityTests.cs` cobrindo AC-1 (consumo de memória), AC-2 (cancelamento), AC-3 (comparação flexível) e AC-5/AC-7 (lote de streams). | AC-1 a AC-5, AC-7 | 100% dos novos testes passam com prova de mutação | **concluído** |
| T4 | **Registro e Injeção de Dependência:** Registrar `IFileIntegrityService` e `IZoneIdentifierService` como Singletons no container da aplicação. | FR-002, FR-005 | Build 0/0 e serviços instanciados via DI | **concluído** |
| T5 | **Validação e Relatório de Integridade:** Validação completa de compilador Release via `mkfile` e contenção F021. | FR-006, NFR-003, AC-6 | Build 0/0 e relatório emitido | **concluído** |
| T6 | **Validação de Interface e Registro de Prova de Tela:** Validar a ação visualmente no app (cenários de interface em NOT TESTED até a execução da automação UI sem roubo de foco). | AC-6 | App compilado 0/0 e relatório emitido | **NOT TESTED** (aguardando UI automation sem roubo de foco) |

## Teste obrigatório
- Teste de medição de pico de RAM durante hash de arquivo com buffer de 128 KB (< 50 MB de acréscimo): **CONFIRMADO**.
- Teste de cancelamento rápido de hash (< 500 ms): **CONFIRMADO** (interrupção em menos de 50 ms).
- Teste de exclusão em lote de fluxos `:Zone.Identifier` com arquivos de teste: **CONFIRMADO** (3 unblocked, 2 unchanged).
- Verificação de não regressão do Gate F021 e suíte padrão de testes: **CONFIRMADO** (188 testes passando, 0 falhas).

## Entrega
Compilar release (0/0), testes unitários verdes (188/189), instalado em `%LOCALAPPDATA%\Programs\FilesCustom`, gate F021 verificado com sucesso.
