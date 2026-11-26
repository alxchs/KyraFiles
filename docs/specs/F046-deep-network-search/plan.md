# F046 — Busca profunda na rede — PLAN

Status: APROVADO com ajustes (Claude, 25/11/2026; ver "Ajustes da revisão"). Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026).

## 1. Arquitetura atual (lida no código em 25/11/2026)

- **CONFIRMED** (`src/KyraFiles.App/Data/Contracts/ISearchProvider.cs`): define `ISearchProvider`, `SearchRequest` (já possui `OnBatchReceived`, `OnFallbackNotice`, `OnTruncated`, `MaxItemCount`, `SearchSubfolders`) e `SearchCapabilities` (`IsInstantStreaming`).
- **CONFIRMED** (`src/KyraFiles.App/Services/Search/SearchProviderFactory.cs` e `NativeFilesSearchProvider.cs`): despacham buscas locais e fallbacks quando o índice local não cobre o caminho.
- **CONFIRMED** (`src/KyraFiles.App.Search.Index/Basic/DirectoryScanner.cs`): otimizado para volumes locais (`VolumeInfo` / `FileIdBothDirectoryInfo`); não cobre enumeração multi-servidor UNC/SMB com timeout independente por servidor nem descoberta de compartilhamentos (`NetShareEnum`).
- **CONFIRMED**: a medição `R-rede-baseline` citada na `spec.md` ainda não foi executada.

## 2. Abordagem

1. **T0 — Baseline `R-rede-baseline` (pré-requisito da spec):** medir tempo, CPU e memória da busca atual do app e do Windows Explorer em caminho UNC/rede (ou loopback `\\localhost\...` quando sem NAS) para estabelecer a linha de base de NFR-001.
2. **Provedor `NetworkSearchProvider : ISearchProvider`:**
   - Ativado automaticamente para caminhos UNC (`\\servidor\compartilhamento` ou unidades mapeadas de rede) ou escopo "todas as pastas compartilhadas de `\\servidor`" (enumeradas via `NetShareEnum` em `KyraFiles.App.CsWin32`).
   - Varredura em streaming por lotes (`OnBatchReceived`, FR-001) com `SemaphoreSlim` limitando concorrência por servidor (FR-002) e `CancellationTokenSource` com timeout configurável por servidor para não travar os demais se um estiver desligado (AC-3).
   - Respeita permissões do usuário atual: diretórios com `UnauthorizedAccessException` são ignorados e contabilizados em resumo como "inacessíveis" (AC-5, FR-003).
   - Suporte a caminhos longos (`\\?\UNC\...`) e detecção de queda de VPN/rede sem travar a UI.
3. **Cache/Índice opcional por compartilhamento (FR-004):**
   - Conforme decisão em `spec.md §8`, cache opcional em disco **desligado por padrão**, com TTL configurável, botão "Atualizar agora" e invalidação rápida para detectar arquivos novos na repetição (AC-6).
   - **Sem busca por conteúdo na rede nesta versão** (`spec.md §8`).

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001, FR-002, FR-003, AC-1..AC-7 | Novo `NetworkSearchProvider.cs` em `src/KyraFiles.App/Services/Search/` + `SearchProviderFactory.cs` |
| US3 (descoberta de shares) | `src/KyraFiles.App.CsWin32/NativeMethods.txt` (`NetShareEnum` se necessário) |
| FR-004 (índice opcional por share) | Novo `NetworkShareIndexCache.cs` + configuração em Settings |
| NFR-001 | Medição `R-rede-baseline` (T0) e validação comparativa (T5) |

**A NÃO mexer:** `KyraFiles.App.IndexService` (gate F021) e busca local de volumes NTFS (`VolumeIndex`).

## 4. Riscos

- Saturação de banda/IOPS em NAS ou travamento de thread em share offline: usar timeout por servidor e `Channel`/lotes limitados com cancelamento cooperativo (≤ 1 s, AC-2).
- Unidades mapeadas apontando para servidor offline: resolver UNC real antes de iniciar e aplicar timeout por servidor.

## 5. Estratégia de teste

| AC | Como se prova |
|---|---|
| AC-1 / AC-2 | Streaming exibe primeiros resultados antes do fim; "Cancelar" para em ≤ 1 s |
| AC-3 | 2 servidores (1 offline): offline expira no timeout e reporta "sem resposta"; outro retorna normal |
| AC-4 / AC-5 | Escopo `\\servidor` varre shares acessíveis e marca inacessíveis sem vazar nomes |
| AC-6 / AC-7 | Repetição com cache opcional habilitado é mais rápida e detecta arquivo novo; filtros funcionam |
| NFR-001 | CPU e RAM estáveis vs baseline `R-rede-baseline` |

## 6. Ajustes da revisão (Claude, 25/11/2026)

- `NetShareEnum` entra em `src/KyraFiles.App.CsWin32/NativeMethods.txt`; `NETAPI32.dll` existe em todo Windows suportado, então adicionar o `DirectPInvoke` correspondente no `KyraFiles.App.csproj` (regra do CLAUDE.md do projeto).
- T0 pode usar `\\localhost\<compartilhamento de teste>` quando não houver NAS; o handoff diz qual foi usado, porque loopback não mede latência real.
- Timeout por servidor precisa cobrir também a primeira chamada de enumeração (que é onde um servidor desligado trava), não só a leitura de pastas.
- Cache opcional (FR-004) guarda só nomes e metadados, nunca conteúdo, e fica em `LocalState` do app.
