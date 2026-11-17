# Spec F023: Busca Rápida Híbrida, Fluidez de UI e Sanificação de Recursos

## 1. Contexto e Objetivos

O projeto KyraFiles será distribuído publicamente no GitHub e deve oferecer busca de arquivos ultrarrápida (sub-50ms / instantânea na percepção do usuário), operando com excelência em qualquer máquina Windows sem exigir dependências externas instaladas e sem recorrer a daemons pesados em segundo plano (o indexador contínuo de background permanece 100% desarmado).

Adicionalmente, esta especificação atende à diretriz do Alexandre para uma análise e revisão profunda em quatro pilares vitais:
1. **Validação e saneamento de vazamentos de memória:** Identificação e eliminação de leaks em handlers de eventos, tokens de cancelamento (`CancellationTokenSource`), handles Win32 e retenção de coleções.
2. **Correção de lógica mal aplicada que compromete a fluidez:** Fim do I/O síncrono em loop (`Directory.Exists`), interrupção da reordenação e re-renderização total da árvore visual WinUI 3 a cada lote de 120ms, e imposição de limites para buscas interativas.
3. **Substituição de componentes arcaicos ou obsoletos:** Fim do uso de arquivos temporários em disco (`%TEMP%\*.csv`) para comunicação CLI, substituição de APIs UWP `Windows.Storage` por Win32/OLE DB de alto desempenho, e substituição de varreduras caras de processos (`Process.GetProcessesByName`) por detecção Win32 atômica via `FindWindow`.
4. **Ganhos de performance via modularização:** Desacoplamento da busca em pipeline reativo com detecção de motor em < 1ms, streaming progressivo de itens em lote com prioridade de despacho na UI, e motor de caminhamento rápido sob demanda (Fast Walker).

---

## 2. Requisitos Funcionais (FR)

### Eixo 1: Integração Híbrida e Detecção Inteligente
- **FR-001 (Detecção Atômica do Everything):** O sistema DEVE detectar se o voidtools Everything está ativo em memória utilizando chamada Win32 atômica `FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null)` (< 0,01 ms), evitando a chamada custosa `Process.GetProcessesByName`.
- **FR-002 (Modo Colaborativo com Everything):** Quando o Everything estiver disponível na máquina:
  - A comunicação DEVE ocorrer sem geração de arquivos CSV em disco, lendo em streaming contínuo via stdout do utilitário ou via IPC de memória.
  - O sistema NÃO DEVE realizar chamadas a `Directory.Exists` no disco para saber se o item é pasta; o tipo do item DEVE ser inferido diretamente dos atributos retornados pelo motor.
- **FR-003 (Busca Nativa Rápida sem Dependência Externa):** Quando o Everything NÃO estiver presente na máquina:
  - Para caminhos sob o escopo indexado do Windows (pastas de usuário, Downloads, Documentos, Desktop): o sistema DEVE consultar o índice nativo do Windows Search (`WSearch`) via OLE DB (`SystemIndex`) ou consulta leve sem sobrecarga de thumbnails.
  - Para caminhos fora do índice (como `C:\desenv`, pen drives, drives secundários): o sistema DEVE utilizar um *Fast Directory Walker* Win32 sob demanda.
- **FR-004 (Zero Daemons em Background):** Nenhum serviço em background, indexador contínuo ou processo residente em segundo plano pode ser iniciado ou executado. O consumo de CPU e RAM em repouso DEVE ser estritamente 0%.

### Eixo 2: Sanificação de Memória e Recursos
- **FR-005 (Descarte Correto de `CancellationTokenSource`):** O `ShellViewModel` DEVE descartar (`Dispose()`) explicitamente instâncias anteriores de `searchCTS` antes de alocar novas instâncias em buscas consecutivas.
- **FR-006 (Eliminação de Vazamento de Event Handlers):** As inscrições de eventos assíncronos durante a busca (como `SearchTick`) DEVEM ser estritamente desacopladas ou desinscritas (`-=`) ao término ou cancelamento da busca, impedindo retenção de instâncias de `ShellViewModel` ou coleções em closures na memória.
- **FR-007 (Gerenciamento Seguro de Handles Win32):** Todos os handles de busca Win32 (`FindCloseSafeHandle`, `SafeProcessHandle`) DEVEM ter descarte garantido via blocos `using` ou `try/finally`, mesmo em cenários de cancelamento ou timeout.

### Eixo 3: Fluidez e Virtualização na Interface de Usuário
- **FR-008 (Streaming Progressivo em Lotes - Batching):** A entrega de resultados para a interface DEVE ocorrer em dois estágios:
  - *Estágio Imediato:* Os primeiros 25 a 50 itens encontrados DEVEM ser despachados para a tela imediatamente com prioridade alta, garantindo resposta visual instantânea (< 50ms).
  - *Estágio Progressivo:* Os itens subsequentes DEVEM ser agregados e entregues em blocos (ex: 50 a 100 itens) com prioridade normal/baixa no `DispatcherQueue`, sem reordenar toda a coleção visual a cada micro-lote.
- **FR-009 (Cap de Resultados Interativos):** Para pesquisas interativas digitadas pelo usuário, o motor DEVE limitar os resultados iniciais (padrão: 500 itens), permitindo expansão sob demanda, evitando alocação desnecessária de dezenas de milhares de `ListedItem`.

### Eixo 4: Modularização e Aprendizado Arquitetural
- **FR-010 (Pipeline Modular de Busca):** A arquitetura de busca DEVE ser modularizada em três componentes independentes e testáveis:
  1. `ISearchDetector`: Identifica motores disponíveis e características do escopo.
  2. `ISearchStreamer`: Produz itens de busca de forma assíncrona desacoplada da UI.
  3. `ISearchUiAdapter`: Gerencia o despacho suave na coleção visual do WinUI 3.
- **FR-011 (Base de Conhecimento para o Novo App):** A documentação arquitetural da F023 DEVE registrar os padrões de engenharia de alta performance do Everything (estruturas de índice em RAM, consumo do diário USN NTFS e IPC Win32), servindo de fundação técnica para o desenvolvimento do futuro aplicativo independente.

---

## 3. Requisitos Não Funcionais (NFR)

- **NFR-001 (Latência de Busca com Everything):** Retorno dos primeiros resultados em menos de 10 ms quando o Everything estiver ativo.
- **NFR-002 (Latência de Busca Nativa):** Retorno dos primeiros resultados em menos de 50 ms para pastas indexadas pelo Windows Search.
- **NFR-003 (Consumo em Repouso):** 0% de CPU e 0 MB de alocação contínua de memória quando nenhuma busca estiver ativa.
- **NFR-004 (Estabilidade de Memória):** Execução de 50 buscas consecutivas na mesma sessão sem acréscimo residual não-coletável na memória heap gerenciada e de handles do sistema operacional.
- **NFR-005 (Compatibilidade Windows):** Funcionamento 100% autônomo e sem erros em qualquer instalação padrão do Windows 10 e Windows 11.

---

## 4. Critérios de Aceite e Verificação

- [ ] A aplicação compila sem erros via `mkfile`.
- [ ] O script de contenção `tools\perf\verify_no_index_service.ps1` é executado e aprova com código 0.
- [ ] Com o Everything ativo: busca retorna instantaneamente sem criar arquivos no `%TEMP%` e sem chamadas a `Directory.Exists`.
- [ ] Sem o Everything ativo: busca opera normalmente trazendo resultados sem travar a interface e sem mensagens de erro.
- [ ] 50 buscas consecutivas executadas sem vazamento de handles ou travamento do processo.
- [ ] A janela do KyraFiles abre, permanece fluida durante a digitação e a rolagem dos resultados ocorre a 60 FPS.
