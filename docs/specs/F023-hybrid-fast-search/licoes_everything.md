# Lições Arquiteturais do Everything e Diretrizes para Motor de Busca Nativo Independente (F023 — T14)

**Data:** 2026-10-24  
**Contexto:** Especificação F023 / Tarefa T14  
**Objetivo:** Sistematizar a engenharia reversa conceitual do voidtools Everything, registrar os erros e armadilhas identificados no indexador legado do KyraFiles, e fornecer as bases arquiteturais para a criação de um motor de busca independente superior.

---

## 1. Por que o Everything é Rápido: Princípios Fundamentais

O voidtools Everything é amplamente considerado o padrão de ouro de indexação e busca em sistemas operacionais Windows. Sua velocidade não decorre de truques superficiais, mas de decisões de engenharia de baixo nível que respeitam a física do sistema de arquivos NTFS:

### 1.1 Leitura Direta de MFT vs Varredura Tradicional de Diretórios
- **Varredura tradicional (FindNextFile / Directory.EnumerateFileSystemEntries):**
  - Cada diretório visitado exige uma chamada ao kernel do Windows e múltiplas operações de I/O em disco para ler os registros do diretório, resolver metadados e retornar estruturas `WIN32_FIND_DATA`.
  - Em um disco com 2 milhões de arquivos espalhados em centenas de milhares de pastas, o tempo de busca síncrona varia de 30 segundos a minutos, gerando concorrência intensa de leitura no disco.
- **Leitura Direta da MFT (`$MFT`) via USN Record Enumeration:**
  - O Everything ignora a árvore lógica de diretórios durante a inicialização.
  - Ele utiliza a control code de baixo nível `FSCTL_ENUM_USN_DATA` (ou lê o stream raw `$MFT` quando dispõe de privilégios de administrador/backup).
  - A leitura ocorre de forma sequencial em blocos contíguos de 64 KB a 1 MB. O disco lê os registros de arquivos sequencialmente na velocidade máxima de transferência do hardware (centenas de MB/s em SSDs NVMe).
  - 2 a 3 milhões de arquivos são enumerados e carregados para a memória em **menos de 1 segundo**.

### 1.2 Monitoramento em Tempo Real via USN Journal
- Em vez de usar `ReadDirectoryChangesW` (que sobrecarrega os buffers de eventos e estoura com facilidade em caso de muitas operações de arquivo), o Everything monitora o **NTFS USN Change Journal** (`FSCTL_READ_USN_JOURNAL`).
- Toda operação de criação, renomeação, exclusão ou alteração de atributos em volumes NTFS gera um registro USN de 64 a 128 bytes no log de transações do próprio driver NTFS.
- O Everything lê esse log periodicamente ou sob demanda, aplicando deltas imediatos ao seu índice em RAM com consumo de CPU indetectável (< 0,01%).

### 1.3 Estrutura de Dados Estritamente em Memória RAM
- **Zero Bancos de Dados em Disco para Consultas:** O Everything não utiliza SQLite, SQL Server Compact, RocksDB ou árvores B em disco para responder a buscas por nome de arquivo. Todo o índice reside 100% em memória RAM contígua.
- **Flat Structs Compactas:**
  - Cada arquivo é representado por uma struct compacta (cerca de 24 a 32 bytes) contendo:
    - ID do registro MFT (64 bits).
    - ID do registro MFT do diretório pai (64 bits).
    - Atributos e datas básicas codificadas.
    - Ponteiro ou offset em um bloco contíguo de strings (String Pool).
- **Consumo de Memória:** Para indexar 1.000.000 de arquivos, o Everything consome apenas cerca de **40 a 50 MB de RAM**.
- **Busca por Nome em CPU L1/L2 Cache-Friendly:** As buscas por substring ou expressões regulares percorrem arrays contíguos de memória, tirando total proveito de prefetching da CPU e vetorização SIMD (AVX2/AVX-512). O tempo de resposta de uma consulta típica é de **1 a 5 milissegundos**.

---

## 2. Diagnóstico do KyraFiles: O que NÃO Fazer (Erros do `IndexService` Legado)

O desenvolvimento do `KyraFiles.App.IndexService` legado cometeu quase todos os erros clássicos de arquitetura de indexação no Windows:

1. **Uso de Banco Relacional em Disco (SQLite / EF Core):**
   - Gravar milhões de arquivos em tabelas SQLite com índices B-Tree fragmenta o arquivo de banco em disco, consome centenas de megabytes de I/O contínuo e gera commits transacionais que travam o disco.
2. **Varredura Recursiva em Camada de Aplicação:**
   - O serviço usava varredura clássica de pastas via .NET, competindo por I/O com os programas em uso pelo usuário.
3. **CPU Contínuo em Repouso:**
   - Mesmo quando "ocioso", processos de reconciliação e flush mantinham 15% a 30% de CPU consumidos, o que motivou a diretriz mandatória de corte e contenção (Gate F021).
4. **Chamadas de I/O em Laços de Resultados (`Directory.Exists` / `File.Exists`):**
   - Ao receber resultados do `es.exe`, a UI executava `Directory.Exists(path)` síncrono para cada linha antes de adicionar à tela, bloqueando o thread de streaming.
5. **Comunicação por Arquivo Físico no Disco (`%TEMP%\*.csv`):**
   - Gravar resultados de busca em CSV no disco, fechar o processo, reler o arquivo do disco e excluí-lo gerava centenas de operações de I/O inúteis por pesquisa.

---

## 3. O Padrão Híbrido F023 Implementado no KyraFiles

Para atender à demanda imediata sem sobrecarregar a máquina, a arquitetura F023 estabeleceu o padrão híbrido seguro:

1. **Detecção Atômica Ultra-Rápida:**
   - Em vez de varrer a lista de processos com `Process.GetProcessesByName`, utiliza `PInvoke.FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null)`. Tempo de verificação: **< 0,001 ms**.
2. **Streaming Assíncrono por Pipe (`StandardOutput`):**
   - Comunicação direta com `es.exe` sem nenhum arquivo intermediário no disco.
   - Linhas processadas e emitidas em lotes conforme chegam pelo pipe.
3. **Extração de Atributos dos Metadados do Pipe:**
   - Eliminação de 100% das chamadas a `Directory.Exists`/`File.Exists`. O tipo do item é derivado dos metadados emitidos pelo executável.
4. **Despacho em Lotes na UI sem Travamento:**
   - Primeiro lote imediato (primeiros 25 a 50 itens) garante percepção instantânea de resposta.
   - Desinscrição determinística e descarte imediato de `CancellationTokenSource` eliminam vazamentos de handles de kernel em pesquisas rápidas.
5. **Fallback Transparente para Motor Nativo:**
   - Na ausência do Everything, o `NativeFilesSearchProvider` assume silenciosamente via Windows Search (`WSearch`), sem falhas ou janelas de erro para usuários externos.

---

## 4. Especificação Conceitual para o Futuro Motor Independente ("Superior ao Everything")

Para superar o Everything no futuro sem depender de ferramentas de terceiros, o novo motor autônomo deve seguir as seguintes diretrizes:

### 4.1 Arquitetura do Processo
- **Processo:** Serviço ou daemon leve em C++20 / Rust nativo (executável único, < 5 MB).
- **Privilégios:** Execução com privilégio `SeManageVolumePrivilege` e `SeBackupPrivilege` para abertura de handles em `\\.\C:` com `GENERIC_READ`.
- **Modo de Operação:** 100% autônomo, sem interface visual própria, exposto via IPC para front-ends modernos.

### 4.2 Pipeline de Dados e Índices
- **Fase 1 (Cold Start — ~0,8s para 2 milhões de arquivos):**
  - Abertura do handle do volume.
  - Leitura dos registros USN usando `FSCTL_ENUM_USN_DATA`.
  - Montagem de tabela hash de File Reference Numbers (FRN) para reconstrução instantânea dos caminhos completos.
  - Alocação em bloco único contíguo de memória virtual (`VirtualAlloc`).
- **Fase 2 (Keep-Alive — 0% CPU em repouso):**
  - Thread em espera bloqueante em `FSCTL_READ_USN_JOURNAL` com timeout adaptativo.
  - Aplicação de deltas in-memory (inclusão/exclusão em O(1)).
- **Fase 3 (Consultas — < 2 ms por busca):**
  - Algoritmo de correspondência fuzzy e regex acelerado por instruções SIMD AVX2.
  - Indexação invertida de n-gramas em memória para busca instantânea à medida que o usuário digita.

### 4.3 Comunicação Inter-Processo (IPC) de Ultra-Baixa Latência
- **Canal Principal:** Memória Compartilhada Win32 (`CreateFileMapping` / `MapViewOfFile`) estruturada como Ring Buffer circular atômico sem locks (lock-free).
- **Notificação:** Eventos Win32 nomeados (`CreateEvent` / `SetEvent`).
- **Fallback:** Named Pipe assíncrono para clientes sem privilégio de mapeamento direto.
- **Vantagem:** O front-end consome dezenas de milhares de itens em microssegundos sem overhead de serialização de texto ou parsing de strings.

---

## 5. Conclusão
A F023 resolveu em definitivo os gargalos, vazamentos e bloqueios na camada de aplicação do KyraFiles, transformando a busca em uma experiência fluida e sem consumo abusivo de recursos. As lições aqui documentadas formam o roteiro definitivo para quando o projeto optar por embarcar seu próprio motor nativo em nível de sistema de arquivos.
