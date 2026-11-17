# F060 — Matriz de situação da SPEC "Diferenciais" (v1.2) contra o projeto atual

Data: 26/10/2026. Fonte: `origem/SPEC_original_v1.2.md`. Pedido do Alexandre: "adicione essa spec e descarte o que já estiver feito".
Estados da própria SPEC original (§1.2). Cada linha cita o comando/arquivo que a sustenta. Não é a TASK-000 completa (não leu cada requisito até o fim do código): onde só há indício, vale `NEEDS_REVIEW`.

## 1. Descartado (já feito ou já coberto por spec aprovada)

| Requisito da SPEC | Situação | Evidência / destino |
|---|---|---|
| SYNC-003 (MD5, SHA-1, SHA-256) | **EXISTS** (cálculo). SHA-384 e SHA-512 também | `src/KyraFiles.Shared/Helpers/ChecksumHelpers.cs` (`CreateMD5/SHA1/SHA256/SHA384/SHA512`); `HashesViewModel.cs` (aba Hashes das propriedades) |
| SYNC-001, 002, 004 a 011, 014 (inventário, diff por hash, lado a lado, espelho, dry-run, filtros) | **Coberto por spec aprovada** | `F041-folder-compare-sync` (AC-1 a AC-6; prévia, "conteúdo", espelho com Lixeira, ignorar padrões) e `F040-duplicate-finder` (tamanho e depois conteúdo) |
| REN-001, 002, 005, 006, 007, 009 (regex, máscaras, prévia, colisão, desfazer, execução parcial) | **Coberto por spec aprovada** | `F037-bulk-rename-preview` (US1 a US3, AC-5 regex inválida, AC-6 parcial, FR-003 desfazer atômico). O diálogo atual é só `BulkRenameDialog` simples (sem regex: sem ocorrência de `Regex` em `BulkRenameDialogViewModel.cs`), por isso a F037 continua valendo |
| QUEUE-001 a 006, 009, 010 (fila central, background, pausar, cancelar, reordenar, progresso) | **Coberto por spec aprovada** | `F036-copy-queue` (fases A e B aprovadas; T7/T8 aguardam D-F036-1). Progresso e cancelamento básicos já existem no Status Center (`StatusCenterViewModel.cs`, `StatusCenterItem.cs`) |
| REM-001 (FTP: conectar, listar, baixar, enviar) | **EXISTS** | `src/KyraFiles.App.Storage/Ftp/*` e `Utils/Storage/Helpers/FtpHelpers.cs`; pacote FluentFTP 55.0.0 em `Directory.Packages.props` |
| ADR de runtime/UI/DI/persistência (ADR-001 a 004) | **Descartado** | Stack real já conhecida: WinUI 3, .NET (SDK 10.0.301 no build), DI por `Ioc.Default`, `GeneralSettingsService`. A SPEC manda não trocar nada; não há decisão a tomar |
| §14 e §15 (regras Claude/AGY, prompt persistente) | **Descartado** | Já cobertos por `AGENTS.md`, `METODO.md` e `INSTRUCTIONS_OPERARY.md` |
| §13 Fase 0 (auditoria) | **Feita em parte** | Esta matriz + `docs/agents/SITUACAO.md`. Licença do upstream (REM/ADR-015) segue em aberto, ver F060 T0 |

## 2. Resta fazer (vira a spec F060)

| Módulo | Requisitos que sobram | Situação |
|---|---|---|
| MOD-REMOTE | REM-003 (SFTP, senha e chave); REM-002 (FTPS: confirmar o que já há e validar certificado); REM-005/006/007/008 (credenciais, sessão, keep-alive, reconexão); REM-009 (retomada); REM-011/012 (integridade, erros específicos) | SFTP **MISSING** (nenhuma ocorrência de SSH.NET/Sftp no código nem no `Directory.Packages.props`). FTPS: `NEEDS_REVIEW` (nenhuma referência a `EncryptionMode` em `src`; pode estar no padrão do FluentFTP) |
| MOD-SCRIPT | SCRIPT-001 a 012 | **MISSING** (nenhuma ocorrência de MoonSharp/Jint) |
| MOD-HEX | HEX-001 a 010 | **MISSING** (nenhum visualizador hexadecimal) |
| MOD-SPLIT | SPLIT-001 a 009 | **MISSING**. CRC32 existe só como utilitário em `ChecksumHelpers.cs` (linha 27) |
| MOD-NLSEARCH (M5) | NLS-001 a 010 | **MISSING**. Reaproveita `FolderSearch` e o índice da F023; ver ideia em conversa de 26/10/2026 |
| Resíduos da fila | QUEUE-007 (limite de banda), QUEUE-008 (persistir e recuperar após reiniciar), QUEUE-006 (prioridade), QUEUE-012 (verificação final por hash) | A F036 deixa "retomar após reiniciar" e "limite de velocidade" **fora de escopo** (spec F036, §fora de escopo). Vira complemento posterior à F036 |
| Resíduos do renomeio | REN-003/004 (EXIF e ID3), REN-007 persistido entre reinícios, REN-008 (histórico) | F037 cita data "da foto", mas não EXIF/ID3 completos nem rollback após reiniciar. Complemento posterior à F037 |
| Resíduos do sync | SYNC-008 (bidirecional com conflitos), SYNC-012/013 (cache de hash, retomar análise) | F041 só cobre uma direção, dois sentidos "revendo antes" e espelho. Complemento posterior à F041 |

## 3. Ordem sugerida (valor, risco, base existente)

1. **MOD-SPLIT** (autocontido, sem rede, sem UI complexa; risco baixo; reaproveita CRC32 e SHA-256 prontos).
2. **MOD-HEX** (autocontido; risco médio: edição em arquivo grande; precisa de ADR de salvamento).
3. **MOD-REMOTE / SFTP** (depende de decisão de biblioteca e de segurança de host key; ver ADR-007).
4. **MOD-SCRIPT** (maior superfície de segurança; depende da escolha de motor e de eventos estáveis).
5. Resíduos (fila, renomeio, sync) só depois de F036, F037 e F041 estarem entregues.

Regras do Alexandre que valem para tudo isto: bugs e performance primeiro; spec, plan e tasks aprovados antes de código; teste de tela só depois das 23:59 e sem roubar foco; sem push, merge ou release sem autorização; texto ao usuário sem jargão.
