# Plano de Implementação F023: Busca Rápida Híbrida, Fluidez de UI e Sanificação de Recursos

## 1. Arquitetura e Diagnóstico Técnico

### 1.1 Diagnóstico dos Quatro Pilares Solicitados

1. **Vazamentos de Memória (Memory Leaks):**
   - *CancellationTokenSource:* Em `ShellViewModel.SearchAsync`, novas instâncias de `searchCTS` são criadas sem invocar `Dispose()` nas anteriores. Em sessões com dezenas de pesquisas rápidas, handles de kernel associados ao token permanecem órfãos.
   - *Inscrições Anônimas de Eventos:* O evento `search.SearchTick += async (s, e) => ...` vincula uma expressão lambda contendo closures sobre `this` e coleções locais que nunca são desfeitas (`-=`), impedindo coleta de GC.
   - *Handles Win32:* Em cenários de cancelamento assíncrono em `FolderSearch.cs`, handles de busca podem não ser fechados deterministicamente se ocorrer timeout na thread paralela.

2. **Lógica Mal Aplicada que Prejudica a Fluidez:**
   - *I/O Síncrono no Laço de Resultados:* `EverythingSearchProvider.cs` executa `Directory.Exists(path)` síncrono para cada item. Em buscas com milhares de arquivos, isso impõe milhares de viagens ao sistema de arquivos NTFS bloqueando threads de trabalho.
   - *Tempestade de Reordenação e Redesenho na UI:* A cada 120ms (`lastBatchUiTick`), o `ShellViewModel` recria a `ConcurrentCollection`, dispara ordenação completa de todos os itens acumulados (`OrderFilesAndFoldersAsync`) e notifica reavaliação visual completa no WinUI 3 (`ApplyFilesAndFoldersChangesAsync`). Isso causa micro-travamentos (*jank*) e engasgos na digitação do usuário.
   - *Falta de Limitação Interativa:* O `es.exe` e o `FolderSearch` não impunham limites razoáveis de resultados iniciais para a tela, inundando a UI com até dezenas de milhares de elementos.

3. **Componentes Arcaicos ou Obsoletos:**
   - *Comunicação por Arquivo Físico em Disco:* `es.exe` era invocado com `-export-csv temp.csv`, gravando no `%TEMP%`, aguardando a finalização do processo, relendo o arquivo do disco e apagando-o. O padrão moderno deve usar streaming assíncrono via pipe de `StandardOutput`.
   - *Varredura Pesada de Processos para Detecção:* `Process.GetProcessesByName("Everything")` itera por todos os processos do Windows. A chamada Win32 atômica `FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null)` responde em nanossegundos com zero impacto.
   - *APIs UWP `Windows.Storage` com Prefetch Excessivo:* `FolderSearch` configura prefetch antecipado de miniaturas e propriedades básicas de forma síncrona/pesada, sobrecarregando o runtime WinUI 3.

4. **Modularização de Comportamento para Ganho de Performance:**
   - Desacoplamento da busca em três camadas claras:
     - `SearchEngineSelector`: Detecção instantânea de motor (Everything vs Windows Search vs Win32 Walker).
     - `SearchBatchStreamer`: Leitura e agrupamento de itens em memória sem travar threads.
     - `SearchUiBatchDispatcher`: Despacho em lotes escalonados na UI (primeiros 25 itens com alta prioridade para sensação imediata; demais itens em blocos de 50 a 100 com prioridade baixa).

---

## 2. Estratégia de Implementação em Fases

### Fase 1: Saneamento de Memória e Correção do `ShellViewModel.cs`
- Ajustar `SearchAsync` e `CancelSearch` para descartar adequadamente instâncias antigas de `searchCTS`.
- Substituir a inscrição anônima em `search.SearchTick` por método nomeado devidamente desinscrito no encerramento ou por canal direto de lotes (`OnBatchReceived`).
- Otimizar o despacho de lotes na UI:
  - Eliminar reordenação completa a cada lote parcial.
  - Adicionar itens progressivamente à coleção existente em vez de recriar a lista completa repetidas vezes.
  - Ordenação final executada apenas quando a busca for concluída ou pausada.

### Fase 2: Otimização Radical do `EverythingSearchProvider.cs`
- Substituir a checagem lenta `Process.GetProcessesByName` pela detecção atômica `FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null)`.
- Eliminar o arquivo CSV temporário no `%TEMP%`.
- Invocar o `es.exe` configurado para streaming direto por `StandardOutput` com formato otimizado (ex.: `-size -date-modified -attributes` ou colunas estruturadas).
- Extrair o atributo de diretório diretamente dos dados emitidos pelo `es.exe` (sem chamar `Directory.Exists` no disco).
- Adicionar o parâmetro `-max-results 500` nas buscas interativas.

### Fase 3: Aprimoramento da Busca Nativa Rápida (`NativeFilesSearchProvider.cs`)
- Configurar a busca nativa para aproveitar o índice nativo do Windows (`WSearch`) sem prefetch pesado e desnecessário de thumbnails no início.
- Implementar caminhamento rápido de diretórios (*Fast Directory Walker*) sob demanda para caminhos não-indexados.
- Garantir que se o Everything não estiver disponível na máquina, a busca nativa assuma transparentemente com fluidez.

### Fase 4: Validação de Performance, Vazamento e Portões de Qualidade
- Execução de bateria de 50 buscas consecutivas monitorando handles e memória gerenciada.
- Execução do script `tools\perf\verify_no_index_service.ps1` (portão obrigatório).
- Compilação do executável via `mkfile`.
- Abertura visual física do KyraFiles e validação da busca na prática (digitação, resposta imediata, sem congelamentos).

---

## 3. Gestão de Riscos e Mitigações

| Risco | Impacto | Mitigação |
| :--- | :--- | :--- |
| Usuário do GitHub sem Everything instalado | Médio | Fallback transparente para o motor nativo (Windows Search + Fast Walker); sem mensagens de erro ou bloqueios. |
| Busca em drives com milhares de arquivos travar a UI | Alto | Streaming em lotes com cap inicial de 500 itens e inserção progressiva sem reordenar a lista inteira repetidas vezes. |
| Reativação acidental do indexador pesado de fundo | Crítico | O `KyraFiles.App.IndexService` permanece estritamente desarmado (`IsAvailable => false`, sem processo em background); validação automática por script. |
| Vazamento de memória em buscas canceladas rapidamente | Médio | Descarte explícito de CTS e desinscrição obrigatória de delegates no bloco `finally`. |
