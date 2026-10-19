# Tarefas de Implementação F023: Busca Rápida Híbrida, Fluidez de UI e Sanificação de Recursos

## Lista de Tarefas

### Bloco 1: Sanificação de Memória e Correção do `ShellViewModel.cs`
- [x] **T01 - Descarte Seguro de CTS:** Corrigir `CancelSearch` e `SearchAsync` em `ShellViewModel.cs` para garantir que instâncias anteriores de `searchCTS` sejam canceladas e descartadas (`Dispose()`) antes de criar nova instância.
- [x] **T02 - Desinscrição Determinística de Eventos:** Remover inscrições anônimas em `search.SearchTick` ou migrá-las para manipulação com desinscrição explícita no bloco `finally`, eliminando retenção de closures e objetos na memória heap.
- [x] **T03 - Despacho Progressivo e Fim do Jank na UI:** Refatorar o callback `OnBatchReceived` para inserir os itens progressivamente na coleção sem recriar a lista completa nem invocar ordenação total repetidas vezes a cada 120ms.

### Bloco 2: Modernização Radical do `EverythingSearchProvider.cs`
- [x] **T04 - Detecção Win32 Atômica:** Substituir a chamada pesada `Process.GetProcessesByName("Everything")` por verificação Win32 instantânea via `PInvoke.FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null)`.
- [x] **T05 - Streaming via StandardOutput:** Substituir a geração de arquivo CSV temporário no `%TEMP%` por streaming direto assíncrono linha a linha via pipe de `StandardOutput` do `es.exe`.
- [x] **T06 - Eliminação do `Directory.Exists` em Laço:** Extrair se o item é diretório diretamente dos dados/flags do `es.exe` (ex: colunas ou sufixo com barra), eliminando 100% das chamadas síncronas de I/O em disco.
- [x] **T07 - Limite Interativo:** Adicionar `-max-results 500` para consultas interativas digitadas pelo usuário, evitando sobrecarga de dados desnecessários.

### Bloco 3: Robustez da Busca Nativa para Publicação no GitHub (Sem Everything)
- [x] **T08 - Otimização do Provedor Nativo:** Garantir que o `NativeFilesSearchProvider.cs` atue com streaming leve e resposta instantânea para pastas indexadas pelo Windows Search (`WSearch`), sem prefetch antecipado de miniaturas na UI thread.
- [x] **T09 - Fallback Transparente:** Garantir que o `SearchProviderFactory.cs` selecione o Everything quando ativo na máquina ou faça fallback automático e silencioso para o provedor nativo quando ausente, sem falhas para usuários do GitHub.

### Bloco 4: Validação, Portões de Qualidade e Comprovação Visual
- [x] **T10 - Verificação do Portão Anti-Daemon:** Executar `tools\perf\verify_no_index_service.ps1` e certificar saída com código 0 (IndexService permanece 100% desarmado).
- [ ] **T11 - Teste de Estresse de Memória:** Executar bateria de buscas consecutivas verificando estabilidade de memória e ausência de vazamento de handles de kernel.
- [x] **T12 - Compilação Oficial via `mkfile`:** Compilar a aplicação completa via `mkfile` em modo Release.
- [ ] **T13 - Validação Visual Física:** Abrir o executável gerado fisicamente na máquina, realizar buscas com e sem Everything ativo, verificar a velocidade instantânea e atestar ausência de congelamento da interface.
- [ ] **T14 - Registro Arquitetural das Lições do Everything:** Documentar os padrões de índice em RAM, leitura de USN Journal e IPC para subsidiar o futuro desenvolvimento do app independente superior ao Everything.
