# F094 — Busca Everything: escopo global e histórico — PLAN

Status: **APROVADO** junto com a spec (Alexandre, 21/11/2026). Atualizado com FR-013/FR-014.

## 1. Arquitetura atual (lida no código em 21/11/2026)

| Fato | Onde | Estado |
|---|---|---|
| O provedor Everything acrescenta `-path "<pasta aberta>"` ao `es.exe` sempre que o texto não tem `:\`, `:/` nem começa com `\` e a pasta existe. É a causa do problema 1. | `src/KyraFiles.App/Services/Search/EverythingSearchProvider.cs:144-152` | CONFIRMED (leitura) |
| A pasta vem de `SearchRequest.Folder`, preenchida com `WorkingDirectory` na busca principal e nas sugestões. | `ShellViewModel.cs:3618`, `NavigationToolbarViewModel.cs:1298` | CONFIRMED (leitura) |
| A página de resultados mostra "busca por X em <pasta aberta>" (`SearchPagePathBoxOverrideText`) e o Atualizar recria a busca com `WorkingDirectory`. | `BaseLayoutPage.cs:660-667`, `BaseShellPage.cs:639` | CONFIRMED (leitura) |
| Quando o `es.exe` falha, cai em `NativeFilesSearchProvider` na pasta atual **sem aviso** (`OnFallbackNotice` não é chamado). | `EverythingSearchProvider.cs:233-241` | CONFIRMED (leitura) |
| Histórico existe: `SaveSearchQueryToList` guarda as 10 últimas em `GeneralSettingsService.PreviousSearchQueriesList` (configurações do usuário, persistidas). | `NavigationToolbarViewModel.cs:1049-1059`, `GeneralSettingsService.cs:80` | CONFIRMED (leitura); persistência entre sessões INFERRED |
| **Defeito do histórico:** a lista de sugestões deduplica por `ItemPath`. Entradas de histórico (`new SuggestionModel(query, true)`) têm `ItemPath == null`, então só a primeira entra, e uma entrada antiga pode ficar presa na lista (o `toRemove` também compara por `ItemPath`). É a causa do problema 2. | `NavigationToolbarViewModel.cs:1325-1340`, `SuggestionModel.cs:88-92` | INFERRED (leitura do código, não reproduzido no app) |
| Histórico só aparece com a caixa vazia; ao digitar, só resultados de arquivo. | `NavigationToolbarViewModel.cs:1277-1283` | CONFIRMED (leitura) |
| "Salvar busca" (F042) usa `WorkingDirectory` como escopo. | `ModernShellPage.xaml.cs:386-393` | CONFIRMED (leitura) |

## 2. Abordagem

### 2.1 Escopo (FR-001 a FR-007)
1. **Analisador puro em `KyraFiles.Shared`**: `EverythingQueryScope.Parse(string query, Func<string,bool> directoryExists)` → `(string Terms, string? ScopePath, bool HasNativePathSyntax)`.
   - Procura, da esquerda para a direita, o primeiro token que começa com `X:\`, `X:/` ou `\\` (aspas respeitadas). Junta esse token e todos os seguintes (cobre caminho com espaço sem aspas); tira aspas e barra final; se `directoryExists` for verdadeiro, vira `ScopePath` e o que vem antes vira `Terms`.
   - Se não existir, tenta encurtar pelo fim? **Não** (ambíguo): o texto vai inteiro como está (AC-4).
   - `HasNativePathSyntax` = texto contém `path:`, `parent:`, `infolder:`, `nosubfolders:` (sem diferenciar maiúsculas) → não mexe em nada (AC-5).
   - Função pura com `directoryExists` injetado: testável sem disco.
2. **`EverythingSearchProvider`**: deixa de ler `request.Folder`. Usa o analisador: com `ScopePath` → `-path "<ScopePath>"` + `Terms`; sem `ScopePath` → só o texto. Na queda para o nativo, chama `request.OnFallbackNotice` com texto localizado (FR-007).
3. **Título da página de resultados (FR-006)**: em `BaseLayoutPage`, quando o provedor atual for Everything (`ISearchProviderFactory.GetCurrentProvider().Id == "everything"`), o nome exibido vira o `ScopePath` analisado ou a string nova "todo o computador". A navegação continua usando `WorkingDirectory` como hoje (Voltar volta para a pasta de origem).
4. Sugestões (FR-005) e Atualizar passam a seguir a regra de graça, porque a decisão fica no provedor.

**Descartado:** mudar `SearchRequest.Folder` para vazio nos chamadores. Espalharia a regra por 3 lugares e quebraria os motores Nativo/Índice (AC-15). A regra pertence ao provedor Everything.

**Descartado:** passar o caminho cru ao Everything como termo (comportamento atual quando há `:\`). Funciona como "contém este texto no caminho", o que também pega `c:\desenv2`; `-path` é exato e inclui subpastas.

### 2.2 Histórico (FR-008 a FR-011, FR-013, FR-014)
0. **Armazenamento próprio** em `KyraFiles.Shared/SearchHistory/SearchHistoryStore.cs` (padrão do `SavedSearchStore` da F042, JSON com contexto gerado, gravação atômica via `.tmp`): `search-history.json` na pasta de configurações. Cada entrada: texto, escopo, motor, data. Na primeira execução importa as 10 buscas antigas de `PreviousSearchQueriesList` (sem motor, sem escopo). Serviço `ISearchHistoryService` no app.
   - Escopo gravado: Everything = vazio (o escopo vai no próprio texto, FR-002); demais motores = pasta de onde a busca partiu.
   - Repetir (FR-013): `IShellPage.SubmitSearch(query, folder, providerId)`; o motor viaja por `NavigationArguments.SearchProviderId` → `FolderSearch.ProviderId` → `ShellViewModel.SearchAsync` (usa o motor pedido se disponível, senão o atual).
   - Buscas salvas antigas com escopo, abertas com Everything: o escopo é posto no texto (`EverythingQueryScope.Compose`), senão virariam busca global.
1. Corrigir a deduplicação: `SuggestionModel.SuggestionKey` (`ItemPath` para arquivos, texto+escopo para histórico) e sincronização que preserva a ordem (antes só acrescentava no fim).
2. Limite: 20 entradas guardadas e mostradas (decisão do Alexandre, 21/11/2026). Comparação sem diferenciar maiúsculas.
3. Ao digitar: antes de esperar o provedor, inserir até 3 entradas do histórico que contêm o texto (FR-010).
4. Remover/limpar (FR-011): botão "x" no item de histórico (template da sugestão em `NavigationToolbar.xaml`) e "Limpar histórico de busca" em Configurações > Pastas (onde ficam as opções de busca). Strings novas em `Strings/en-US` e `pt-BR`.
5. Persistência (FR-009) já existe; só confirmar no app.

### 2.3 Salvar busca (FR-012)
Em `ModernShellPage.SaveCurrentSearchButton_Click`, com Everything ativo, `ScopePath` = escopo analisado (vazio = todo o índice).

## 3. Mapa requisito → código
| Requisito | Arquivo |
|---|---|
| FR-001..004 | novo `src/KyraFiles.Shared/Helpers/EverythingQueryScope.cs`; `EverythingSearchProvider.cs` |
| FR-005 | (sem mudança; vem do provedor) |
| FR-006 | `Views/Layouts/BaseLayoutPage.cs`; `Strings/*/Resources.resw` (en-US, pt-BR) |
| FR-007 | `EverythingSearchProvider.cs` |
| FR-008, FR-010 | `ViewModels/UserControls/NavigationToolbarViewModel.cs`, `Data/Models/SuggestionModel.cs` |
| FR-011 | `UserControls/NavigationToolbar.xaml(.cs)`, página de Configurações Geral + ViewModel |
| FR-012 | `Views/Shells/ModernShellPage.xaml.cs` |

**Não mexer:** `NativeFilesSearchProvider`, `IndexedSearchProvider`, `AgentRansackSearchProvider`, `FolderSearch`, `SearchProviderFactory`, `SavedSearchStore`, CsWin32.

## 4. Riscos
- Busca global devolve muito mais itens: o teto de 500 (NFR-002) segura memória; conferir que o teto continua sendo passado (`-n`).
- `es.exe` recebe argumentos juntados por espaço: `ScopePath` precisa ir entre aspas; aspas dentro do caminho não existem no Windows.
- Unidade só com letra (`c:`) sem barra: não é tratada como escopo (evita confundir com termos); documentar.
- Strings novas em idiomas além de en-US/pt-BR caem no inglês (padrão do projeto).

## 5. Estratégia de teste
| AC | Como |
|---|---|
| AC-2, AC-3, AC-4, AC-5 | testes unitários do analisador (com `directoryExists` falso/verdadeiro) |
| AC-1, AC-2, AC-6, AC-7 | app aberto, motor Everything, UI Automation sem foco (§16 do método) + captura |
| AC-8 | app aberto com o serviço do Everything parado |
| AC-9..AC-13 | teste unitário da deduplicação/ordem + app aberto e reaberto |
| AC-14 | app aberto: salvar busca global e conferir o escopo no diálogo |
| AC-15 | app aberto com motor Nativo, mesma consulta antes/depois |
| NFR-001 | mesma consulta (`*.exe`) cronometrada antes e depois, mesmo estado do índice |
| Gate F021 | `tools/perf/verify_no_index_service.ps1` |
