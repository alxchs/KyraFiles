# F041 — Comparar e sincronizar duas pastas — PLAN

Status: APROVADO com ajustes (Claude, 25/11/2026; ver "Ajustes da revisão"). Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026).

## 1. Arquitetura atual (lida no código em 25/11/2026)

- **CONFIRMED** (`src/KyraFiles.App/Views/ShellPanesPage.xaml.cs` e `src/KyraFiles.App/Data/Contexts/MultiPanes/IMultiPanesContext.cs`): o app já gerencia múltiplos painéis (`ShellPanesPage`, `IsMultiPaneActive`, `ActivePane`, `GetPane(0)`/`GetPane(1)`).
- **CONFIRMED** (`src/KyraFiles.App/Utils/Storage/Operations/IOperationQueue.cs`, `OperationQueue.cs`, `FilesystemOperations.cs`): existe fila de operações de cópia (F036) e helpers de sistema de arquivos (`IFilesystemOperations`).
- **CONFIRMED**: não existe motor de comparação entre as duas pastas dos painéis nem tela/painel de prévia de sincronização.

## 2. Abordagem

1. **Motor de comparação (`FolderCompareService`):**
   - Recebe `leftPath` e `rightPath` (dos dois painéis do `ShellPanesPage`), opções (`IncludeSubfolders`, `IgnorePatterns` como `*.tmp`/`.git`, `CompareMode`: `SizeAndDate` vs `ContentHash`, `TimestampToleranceSeconds` para NAS/rede, `NamesOnly` para US3).
   - Faz enumeração em streaming assíncrono (`EnumerationOptions` com `IgnoreInaccessible = true`, sem seguir symlinks por padrão) para entregar os primeiros resultados em ≤ 5 s mesmo com 100 mil arquivos por lado (NFR-001).
   - Trata conflito arquivo-vs-pasta com mesmo nome e diferenças apenas de caixa (case-insensitive no NTFS).
   - Modo `ContentHash` exibe aviso de custo antes de ler conteúdo (AC-4).
2. **Prévia e seleção (`FolderCompareViewModel` + `FolderCompareView`):**
   - Lista apenas diferenças por padrão (com toggle "Mostrar iguais", AC-1), indicando motivo (`Só na esquerda`, `Só na direita`, `Mais novo na esquerda/direita`, `Conflito tipo`).
   - Permite marcar/desmarcar itens e escolher direção (`Esquerda → Direita`, `Direita → Esquerda`, `Bidirecional` sem sobrescrever em modo "só nomes", US2/US3).
   - **Sem modo espelho (exclusão no destino) nesta versão**, conforme decisão em `spec.md §8`.
3. **Execução pela fila (`OperationQueue`):**
   - Ao clicar "Aplicar", enfileira apenas os itens marcados na `IOperationQueue` (FR-002), respeitando cancelamento (AC-5) com resumo do que foi copiado e do que faltou.

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| US1, US3, AC-1, AC-4, AC-6, FR-003, NFR-001 | Novo `FolderCompareService.cs` |
| US2, AC-2, AC-5, FR-001, FR-002 | Novos `FolderCompareViewModel.cs` e `FolderCompareDialog/Page.xaml(.cs)` integrados a `IOperationQueue.cs` / `FilesystemOperations.cs` |
| Gatilho nos painéis duplos | Ação nova em `src/KyraFiles.App/Actions/` lendo os 2 painéis via `IMultiPanesContext` / `ShellPanesPage` |

**A NÃO mexer:** lógica de `OperationQueue` além do consumo pela interface `IOperationQueue`; não adicionar exclusão automática/espelho nesta versão.

## 4. Riscos

- 100 mil arquivos por lado travando a UI: usar `Dictionary<string, FileEntry>` em background thread e virtualização na lista de prévia.
- Pastas de rede com relógio defasado (FAT/SMB): usar tolerância configurável em segundos (padrão 2 s, AC-6).

## 5. Estratégia de teste

| AC | Como se prova |
|---|---|
| AC-1 | Comparar 2 pastas (2 só esq, 1 só dir, 1 mais novo dir): 4 diferenças listadas, iguais ocultos |
| AC-2 | Desmarcar 1 item e aplicar `Esquerda → Direita`: apenas marcados copiados, origem intacta |
| AC-4 / AC-6 | Alternar tamanho+data vs conteúdo (aviso); tolerância de data evita falso positivo |
| AC-5 | Cancelar no meio: para, mantém já copiados, exibe resumo |
| NFR-001 | Benchmark com 100k arquivos/lado: primeiro resultado ≤ 5 s e janela responsiva |

## 6. Ajustes da revisão (Claude, 25/11/2026)

- Política de sobrescrita explícita: item "mais novo de um lado" é copiado por cima do outro pela fila, com a confirmação de substituição que a fila já usa; conflito arquivo-vs-pasta nunca é resolvido sozinho (fica desmarcado com aviso).
- Nada é apagado em nenhum lado nesta versão (sem espelho), nem por "Bidirecional".
- Diferença só de caixa no nome é mostrada como diferença de nome, não como "só de um lado".
- Modo `ContentHash` lê em blocos (nunca o arquivo inteiro na memória) e pode ser cancelado.
