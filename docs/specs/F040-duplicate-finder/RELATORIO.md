# F040 — Localizar arquivos duplicados — RELATÓRIO DE CONCLUSÃO

Data: 2026-11-19  
Status: CONCLUÍDO (T1, T2, T3)  
Branch: `feature/f040-duplicate-finder`  
Pontos da SPEC: 5 pts  

---

## 1. Resumo da Implementação

Implementado o localizador e removedor seguro de arquivos duplicados (**F040** / SPEC-0036):

1. **Núcleo Puro (`KyraFiles.Shared/DuplicateFinder/`):**
   - `DuplicateFinderEngine.cs`: Pipeline em 3 estágios:
     - **Estágio 1 (Tamanho):** Agrupamento rápido por tamanho e descarte antecipado de arquivos com tamanho único (**NFR-001** — disco não é lido se o tamanho for único). Filtragem de pastas de desenvolvimento (`.git`, `node_modules`, `bin`, `obj`, etc.) e arquivos do sistema por padrão (AC-7, FR-004). Arquivos bloqueados ou inacessíveis são adicionados a `UnverifiedFiles` sem abortar a análise (AC-6).
     - **Estágio 2 (Quick-Hash):** Leitura de apenas 4 KiB de cabeçalho dos arquivos sobreviventes do mesmo tamanho para descarte rápido de falsos positivos.
     - **Estágio 3 (SHA-256 Streaming O(1)):** Cálculo de resumo SHA-256 em blocos fixos de 64 KiB para memória estritamente constante e segura contra exaustão de RAM em arquivos grandes.
     - **Modos de Comparação:** Suporte a `ContentHashSha256`, `QuickHashAndSize` e `NameAndSizeUnverified` (rotulado explicitamente como *"não garantido"* conforme decisão §8).
     - **Cancelamento:** Cancelamento imediato via `CancellationToken` (AC-5).
   - `DuplicateSelectionRules.cs`:
     - **Invariante Rígida (FR-002 / AC-4):** É estritamente impossível selecionar todas as cópias de um grupo para exclusão (`CanSelectForDeletion` bloqueia seleção da última cópia).
     - Regras automáticas: `ApplyKeepOldest` (AC-3), `ApplyKeepNewest`, `ApplyKeepInFolder` e `ClearSelection`.
   - `DuplicateGroup.cs`, `DuplicateFileItem.cs`, `DuplicateFinderOptions.cs`, `DuplicateFinderResult.cs`.

2. **Interface de Usuário WinUI 3 & Ações (`KyraFiles.App`):**
   - `DuplicateFinderDialog.xaml` e `.xaml.cs`: Diálogo WinUI 3 apresentando grupos de arquivos duplicados, cabeçalho de grupo com economia de espaço, lista de cópias com checkbox de exclusão, datas formatadas e status. Barra de ferramentas com botões para "Manter mais antigo", "Manter mais recente", "Desmarcar tudo" e reanálise.
   - `DuplicateFinderDialogViewModel.cs`: Gerencia estado da análise, progresso em segundo plano, seleção com validação de invariante e cálculo dinâmico de espaço recuperável.
   - `AppDialogService.cs`: Registrado mapeamento de `DuplicateFinderDialogViewModel` para `DuplicateFinderDialog`.
   - `OpenDuplicateFinderAction.cs`: Ação `[GeneratedRichCommand]` registrada no catálogo de comandos para pastas selecionadas ou pasta atual.
   - **Exclusão Segura (FR-003):** Exclusão exclusiva para a Lixeira do Windows via `FileOperationsHelpers.DeleteItemAsync(..., permanently: false, ...)` com histórico e desfazer nativos.

---

## 2. Evidências de Teste

### Testes Unitários (`tests/KyraFiles.App.Search.Index.Tests/DuplicateFinderTests.cs`)
- **Total:** 7 testes
- **Com falha:** 0
- **Bem-sucedidos:** 7 (100% de sucesso)
- **Cenários cobertos:**
  1. `FindDuplicates_ExactDuplicates_FindsGroupsAndReclaimableSpace` (AC-1)
  2. `FindDuplicates_SameSizeDifferentContent_NotGroupedInHashMode` (AC-2)
  3. `SelectionRules_KeepOldest_MarksNewerForDeletion` (AC-3)
  4. `SelectionRules_NeverAllowsSelectingLastCopy` (AC-4 / FR-002)
  5. `FindDuplicates_CancellationRequested_CancelsGracefully` (AC-5)
  6. `FindDuplicates_LockedOrInaccessibleFile_AddedToUnverifiedList` (AC-6)
  7. `FindDuplicates_SystemAndDevFolders_IgnoredByDefault` (AC-7 / FR-004)

### Gate F021 (Contenção de Recursos)
- Executado via `tools/perf/verify_no_index_service.ps1`.
- `KyraFiles.App.IndexService.exe`: Inativo / Ausente (confirmado).
- Gate F021: **VALIDADO COM SUCESSO**.

### Verificação de Resíduos e Nomes Legados
- `tools/check-legacy-names.ps1`: **0 violações** encontradas.
- Limpeza preventiva de `src/KyraFiles.App/bin/x64/Debug`: **Executada**.

---

## 3. Pontos de Atenção e Advertências Técnicas

1. **Invariante de Exclusão:** A interface e a camada de domínio impedem programaticamente que o usuário selecione a última cópia restante de qualquer grupo de duplicados. Se houver 3 arquivos e 2 estiverem marcados, tentar marcar o terceiro é sumariamente rejeitado.
2. **Exclusão Segura:** Nenhuma operação apaga arquivos de forma permanente diretamente. Todas as exclusões são enviadas à Lixeira do Windows, permitindo restauração imediata.
3. **Modo Nome e Tamanho:** O modo rápido sem verificação de hash é rotulado visualmente como "Não garantido" para evitar que o usuário assuma identidade de conteúdo sem conferência de SHA-256.
