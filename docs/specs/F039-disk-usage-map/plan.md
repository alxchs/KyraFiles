# F039 — O que está ocupando meu disco — PLAN

Status: RASCUNHO (aguardando aprovação para implementação). Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026).

## 1. Arquitetura atual (lida no código em 25/11/2026)

- **CONFIRMED** (`src/KyraFiles.App.Search.Index/Basic/DirectoryScanner.cs`): `DirectoryScanner.ScanDirectory(Guid, string, IProgress<ScanProgress>?, CancellationToken)` varre árvores de diretórios sem privilégio de administrador lendo apenas metadados via `FileIdBothDirectoryInfo`, ignora pontos de reanálise (junções/symlinks) evitando contagem dupla e deduplica hard links via `HardLinkResolver`.
- **CONFIRMED** (`src/KyraFiles.App.Search.Index/Storage/VolumeIndex.cs`): armazena a estrutura indexada em memória por volume quando o índice da busca (F023) está disponível.
- **CONFIRMED** (`src/KyraFiles.App/Utils/Storage/Operations/FileSizeCalculator.cs` e `FilesystemHelpers.cs`): o app já possui cálculo auxiliar de tamanho de pasta e exclusão para a Lixeira com confirmação.
- **CONFIRMED**: não existe ainda uma página/painel dedicado de análise de uso de disco com agregação hierárquica progressiva, cancelamento em 1 s e atualização incremental após exclusão.

## 2. Abordagem

1. **Motor de agregação (`DiskUsageScanner`):**
   - Quando `VolumeIndex` (F023) estiver disponível e atualizado para a unidade, agregar tamanhos diretamente em memória; caso contrário, usar varredura incremental baseada em `DirectoryScanner` / `FileIdBothDirectoryInfo` (respeitando ACLs, ignorando reparse points/symlinks e sem baixar arquivos OneDrive "só na nuvem", checando atributo `FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS`).
   - Emitir progresso incremental a cada intervalo curto para que as 20 maiores pastas apareçam em ≤ 5 s (AC-1) e suportar cancelamento imediato via `CancellationToken` (≤ 1 s, AC-2).
   - Contabilizar diretórios sem acesso como nó agregado "Inacessível" (AC-6) e reconciliar com espaço usado do volume (`DriveInfo.TotalSize - TotalFreeSpace`) exibindo diferença explicada como "Sistema / Reservado" (AC-7).
2. **Modelo e memória estável (NFR-001):**
   - Manter árvore agregada por diretório (não instanciar 5 milhões de `ListedItem` na UI); expor apenas os filhos do diretório atual + top N maiores itens por filtros (tipo e idade, FR-003).
3. **Interface P1 (lista hierárquica) + P2 (mapa de blocos / treemap):**
   - Seguindo a decisão em `spec.md §8`, entregar primeiro a lista hierárquica com barra proporcional de tamanho, navegação "Entrar/Voltar", filtros por tipo/idade, "Mostrar no local" e "Excluir" (via `FilesystemHelpers` para a Lixeira com recálculo incremental sem reescanear o disco, AC-5); em seguida, adicionar a visualização de mapa de blocos (treemap) sobre o mesmo ViewModel.

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001, FR-002, NFR-001 | Novo `DiskUsageScanner.cs` em `KyraFiles.App` / `KyraFiles.App.Search.Index` reutilizando `DirectoryScanner.cs` e `VolumeIndex.cs` |
| US1, AC-1..AC-7, FR-003 | Novos `DiskUsageViewModel.cs` e `DiskUsagePage.xaml(.cs)` (ou aba/painel integrado em `ShellPanesPage`) |
| AC-4, AC-5 | Integração com navegação (`ShellPanesPage`) e `FilesystemHelpers.cs` |

**A NÃO mexer:** `KyraFiles.App.IndexService` (deve permanecer inativo conforme Gate F021) e lógica de escrita do `VolumeSnapshot`.

## 4. Riscos

- Contagem dupla por junctions/hard links ou download acidental de arquivos OneDrive: evitar usando `FILE_ATTRIBUTE_REPARSE_POINT` e `FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS` / `FILE_ATTRIBUTE_OFFLINE`.
- Pico de RAM em unidades com 5M+ arquivos: usar structs compactas de agregação por diretório e nunca materializar todos os arquivos em `ObservableCollection`.

## 5. Estratégia de teste

| AC | Como se prova |
|---|---|
| AC-1 / AC-2 | Varredura em `C:\`: top 20 pastas exibidas em ≤ 5 s; cancelar para em ≤ 1 s mantendo parciais |
| AC-3 / AC-4 / AC-5 | Navegar entra/volta; "Mostrar no local" abre pasta; "Excluir" envia à Lixeira e subtrai do total sem reescanear |
| AC-6 / AC-7 | Pastas protegidas contadas como "Inacessível"; soma fecha com espaço usado do disco |
| NFR-001 | Medir pico de RAM e tempo em árvore grande (≥ 100k arquivos reais ou sintéticos) |
