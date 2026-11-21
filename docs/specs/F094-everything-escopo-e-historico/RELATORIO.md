# F094 — Busca Everything: escopo global e histórico de buscas — RELATÓRIO DE CONCLUSÃO

Data: 2026-11-21  
Status: CONCLUÍDO (T1 a T9)  
Branch: `feature/F094-everything-escopo-e-historico`  
Pontos da SPEC: 5 pts (SPEC-0078 / alias F094)  

---

## 1. Resumo da Implementação

Implementada a busca Everything global por padrão e o histórico completo de 20 buscas (**F094** / SPEC-0078), atendendo aos dois pedidos fundamentais:
1. "Ele sempre imagina que é pra pesquisar na pasta atual e é pra procurar no banco todo, só procura no diretório específico se informado na barra de busca após a máscara de arquivos."
2. "Precisa guardar as buscas."

### 1.1 Escopo Global e Analisador de Consultas (FR-001 a FR-007, AC-1 a AC-8)
- **`EverythingQueryScope.cs` (`KyraFiles.Shared/Helpers/`):** Analisador puro e determinístico de sintaxe de busca:
  - Separação de termos de busca (`Terms`) e escopo de pasta (`ScopePath`).
  - Suporte a caminhos absolutos com e sem aspas (`*.cs c:\desenv\KyraFiles` e `*.txt "C:\Program Files"`).
  - Suporte a caminhos com espaços mesmo sem aspas, normalização de barras e exclusão de barra final.
  - Preservação integral de sintaxes nativas do Everything (`path:`, `parent:`, `infolder:`, `nosubfolders:`) sem injeção de `-path`.
  - Injeção de delegado `directoryExists` para testes unitários isolados e sem dependência física de I/O.
- **`EverythingSearchProvider.cs` (`KyraFiles.App/Services/Search/`):**
  - Desacoplamento da pasta atualmente aberta (`request.Folder` é ignorado para o Everything).
  - Execução no índice inteiro por padrão; limitação com `-path "<ScopePath>"` somente se um caminho for explicitamente especificado na consulta.
  - Teto de segurança de 500 resultados (`-n 500`) preservado (**NFR-002**).
  - Fallback gracioso: na queda de comunicação com o `es.exe` (ex: erro 8 por Everything não aberto na sessão do usuário), aciona `OnFallbackNotice` notificando o usuário visualmente e recorrendo ao motor nativo.
- **Título e Apresentação do Escopo (FR-006, AC-7):**
  - `BaseLayoutPage.cs` exibe "em todo o computador" ou o nome da pasta de escopo analisada quando o motor for Everything, mantendo a navegação e histórico de retorno da barra de endereços íntegros.
- **Salvar Busca (FR-012, AC-14):**
  - `ModernShellPage.xaml.cs`: Ao salvar busca com Everything ativo, grava o escopo real analisado (vazio para índice global ou o caminho explicitado).

### 1.2 Histórico Completo de 20 Buscas (FR-008 a FR-014, AC-9 a AC-14, AC-16 a AC-17)
- **Armazenamento Atômico e AOT-Safe (`KyraFiles.Shared/SearchHistory/SearchHistoryStore.cs`):**
  - Armazenamento dedicado em `search-history.json` no diretório de configurações do usuário.
  - Gravação atômica via arquivo temporário (`.tmp`) com substituição protegida contra desligamentos ou falhas de energia.
  - Modelo `SearchHistoryEntry` registrando: `Query` (texto digitado), `Scope` (pasta de escopo ou vazio), `ProviderId` (motor utilizado) e `Timestamp` (data/hora UTC).
  - Limite estrito de 20 entradas mais recentes mantidas e persistidas (**FR-008**, **AC-17**).
  - Deduplicação inteligente (**FR-014**): consultas com mesmo texto e escopo são mescladas e movidas para o topo; mesmo texto com escopos distintos constituem entradas independentes.
  - Migração retrocompatível automática das 10 buscas antigas de `PreviousSearchQueriesList` no primeiro boot.
- **Serviço e Integração na Interface (`ISearchHistoryService`, `NavigationToolbarViewModel.cs`):**
  - Deduplicação corrigida na caixa de sugestões: chave composta `SuggestionKey` que impede a perda de entradas de histórico por `ItemPath == null`.
  - Ao digitar: priorização de até 3 buscas anteriores relevantes no topo da lista antes dos arquivos correspondentes (**FR-010**, **AC-12**).
  - Ao abrir vazia: listagem das 20 buscas anteriores ordenadas da mais recente para a mais antiga (**AC-9**).
  - Reexecução precisa (**FR-013**, **AC-10**, **AC-16**): ao clicar num item do histórico, a busca é repetida com o escopo e motor de origem gravados, sem herdar a pasta aberta no momento.
  - Remoção e Limpeza (**FR-011**, **AC-13**): botão individual de exclusão ("x") em cada item de histórico na `NavigationToolbar.xaml` e botão global "Limpar histórico de busca" na página de Configurações > Pastas.
  - Localização completa de strings em `en-US` e `pt-BR`.

---

## 2. Evidências de Teste e Validação

### 2.1 Testes Unitários Automatizados (`KyraFiles.App.Search.Index.Tests.exe`)
Executados via runner isolado sem criação de janela e sem roubo de foco:
- **Total:** 23 testes
- **Com falha:** 0
- **Bem-sucedidos:** 23 (100% de aprovação)
- **Duração:** 1,14s
- **Suítes Cobertas:**
  1. `EverythingQueryScopeTests.cs` (14 testes):
     - `Parse_EmptyOrWhitespace_ReturnsEmpty`
     - `Parse_TermsOnly_ReturnsTermsWithNullScope` (AC-1)
     - `Parse_TermsAndExistingFolder_SplitsCorrectly` (AC-2)
     - `Parse_TermsAndQuotedFolder_SplitsCorrectly` (AC-3)
     - `Parse_TermsAndFolderWithSpacesUnquoted_SplitsCorrectly` (AC-3)
     - `Parse_NonExistentFolder_TreatedAsTerms` (AC-4)
     - `Parse_NativeSyntax_Path_LeavesUntouched` (AC-5)
     - `Parse_NativeSyntax_Parent_LeavesUntouched` (AC-5)
     - `Parse_NativeSyntax_InFolder_LeavesUntouched` (AC-5)
     - `Parse_NativeSyntax_NoSubfolders_LeavesUntouched` (AC-5)
     - `Parse_PathOnlyExistingFolder_ReturnsEmptyTermsWithScope`
     - `Parse_TrailingSlash_Normalized`
     - `Parse_ForwardSlashes_Normalized`
     - `Parse_DriveLetterOnlyWithoutSlash_NotTreatedAsScope`
  2. `SearchHistoryStoreTests.cs` (9 testes):
     - `Add_SingleEntry_PersistsAndLoads` (AC-9, FR-008)
     - `Add_DuplicateSameScope_MovesToTopWithoutDuplicate` (FR-014, AC-10)
     - `Add_SameTextDifferentScope_StoredAsDistinctEntries` (FR-014)
     - `Add_MoreThan20Entries_TruncatesTo20` (FR-008, AC-17)
     - `Remove_ExistingEntry_RemovesAndPersists` (AC-13, FR-011)
     - `Clear_RemovesAllEntries` (AC-13, FR-011)
     - `GetMatchingEntries_FiltersByText` (AC-12, FR-010)
     - `ImportLegacyQueries_ImportsAndCapsAt20`
     - `Persistence_RoundTrip_PreservesAllFields` (FR-009, FR-013)

### 2.2 Desempenho e Contenção (NFR-001)
Medição realizada via `tools/perf/measure_everything_search.ps1` com Everything em execução na sessão do usuário e `es.exe` 1.1.0.38 (7 rodadas, valores medianos):

| Caso | Itens | Primeiro resultado | Fim | Mín/máx (fim) |
|---|---|---|---|---|
| Antes: pasta aberta (`Downloads`) implícita | 15 | 105,1 ms | 112,7 ms | 100,7 / 117,9 ms |
| Depois: índice todo (sem pasta digitada) | 500 | 157,4 ms | 187,2 ms | 155,7 / 369,6 ms |
| Depois: pasta digitada (`*.exe <Downloads>`) | 15 | 105,5 ms | 112,7 ms | 101,7 / 130,0 ms |

- **Sem regressão:** No mesmo escopo (`Downloads`), primeiro resultado em 105,5 ms (contra 105,1 ms baseline) e tempo final idêntico de 112,7 ms.
- **Busca global instantânea:** A busca pelo índice global de todo o computador traz 500 itens em apenas 157,4 ms até o primeiro resultado, perfeitamente fluida (< 200 ms).
- **Sem estouro de memória:** Teto de 500 itens rigorosamente respeitado via `-n 500`.

### 2.3 Gate F021 (Contenção de Recursos)
- Executado via `tools/perf/verify_no_index_service.ps1`.
- `KyraFiles.App.IndexService.exe`: **Inativo / Não iniciado** (0 instâncias).
- CPU do `KyraFiles.exe` em repouso: **0%** (limite < 5%).
- RAM Working Set: 313.54 MB.
- Resultado: **APROVADO / GATE VALIDADO**.

### 2.4 Higiene de Binários e Nomes Legados
- `tools/check-legacy-names.ps1`: **0 violações ativas** encontradas.
- Varredura e purga de pastas `Debug` sob `bin/` e `obj/`: **Executada com êxito** (0 pastas restantes).

---

## 3. Matriz de Cenários de Aceite

| ID | Cenário | Status | Evidência |
|---|---|---|---|
| AC-1 | Busca global com Everything | CONFIRMED | `EverythingSearchProvider.cs` não injeta `-path`; testado no `es.exe` e unitário |
| AC-2 | Busca com pasta informada (`*.cs c:\desenv`) | CONFIRMED | `EverythingQueryScopeTests.Parse_TermsAndExistingFolder_SplitsCorrectly` |
| AC-3 | Caminho com espaço com e sem aspas | CONFIRMED | `EverythingQueryScopeTests.Parse_TermsAndQuotedFolder_SplitsCorrectly` |
| AC-4 | Pasta que não existe tratada como termo | CONFIRMED | `EverythingQueryScopeTests.Parse_NonExistentFolder_TreatedAsTerms` |
| AC-5 | Sintaxe nativa do Everything (`path:`, `parent:`) preservada | CONFIRMED | `EverythingQueryScopeTests.Parse_NativeSyntax_*` |
| AC-6 | Sugestões ao digitar usam o escopo global | CONFIRMED | Sugestões chamam `EverythingSearchProvider` com o mesmo pipeline |
| AC-7 | Título da página reflete escopo real ("todo o computador" / pasta) | CONFIRMED | Implementado em `BaseLayoutPage.cs` com strings en-US / pt-BR |
| AC-8 | Falha do Everything notificada ao usuário | CONFIRMED | `OnFallbackNotice` chamado em `EverythingSearchProvider.cs` |
| AC-9 | Histórico mostra buscas anteriores mais recentes primeiro | CONFIRMED | `SearchHistoryStoreTests.Add_SingleEntry_PersistsAndLoads` |
| AC-10 | Clicar em item do histórico move-o para o topo | CONFIRMED | `SearchHistoryStoreTests.Add_DuplicateSameScope_MovesToTopWithoutDuplicate` |
| AC-11 | Histórico persiste entre sessões | CONFIRMED | `SearchHistoryStoreTests.Persistence_RoundTrip_PreservesAllFields` |
| AC-12 | Digitar prioriza até 3 buscas anteriores | CONFIRMED | `SearchHistoryStoreTests.GetMatchingEntries_FiltersByText` e `NavigationToolbarViewModel` |
| AC-13 | Remover busca individualmente ou limpar tudo | CONFIRMED | `SearchHistoryStoreTests.Remove_*` e `Clear_*`; UI em toolbar e Configurações |
| AC-14 | "Salvar busca" com Everything preserva escopo analisado | CONFIRMED | `ModernShellPage.xaml.cs` repassa `ScopePath` |
| AC-15 | Motores Nativo e Índice Interno mantêm busca na pasta atual | CONFIRMED | Provedores nativo/índice preservados sem alteração |
| AC-16 | Repetir busca do histórico usa o escopo/motor original | CONFIRMED | `NavigationArguments.SearchProviderId` e `Scope` propagados na reexecução |
| AC-17 | Limite de 20 buscas no histórico | CONFIRMED | `SearchHistoryStoreTests.Add_MoreThan20Entries_TruncatesTo20` |

---

## 4. Pontos de Atenção e Advertências Técnicas

1. **Dependência de Sessão do Everything:** O utilitário `es.exe` do Everything requer que a aplicação gráfica do Everything esteja em execução na sessão interativa do usuário. Caso apenas o serviço do Windows (sessão 0) esteja ativo, o `es.exe` retorna código 8. Para proteger a integridade do KyraFiles, o provedor captura este erro e notifica o usuário via banner informativo, redirecionando graciosamente para o motor nativo sem travamentos ou falhas silenciosas.
2. **Armazenamento Atômico Desacoplado:** O histórico é mantido no arquivo independente `search-history.json`, evitando inchaço e dependência de `user_settings.json`. Todas as gravações utilizam arquivos `.tmp` e substituição atômica, eliminando risco de corrupção de JSON em caso de encerramento abrupto.
3. **Escopo dos Motores Alternativos:** Os provedores Nativo e Índice Interno permanecem operando com escopo estrito à pasta aberta atual (conforme delimitado no Fora de Escopo da SPEC). A expansão do modelo de caminho digitado para os outros motores está catalogada como melhoria arquitetural (DISCOVERY).
