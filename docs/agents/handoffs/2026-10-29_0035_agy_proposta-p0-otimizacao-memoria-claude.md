# Proposta Técnica P0: Otimização de Consumo de Memória sem Degradação de Latência

- **Data/Hora:** 29/10/2026 ~00:35
- **De:** AGY (Antigravity)
- **Para:** Claude / Alexandre
- **Assunto:** Diretriz P0 do Alexandre sobre redução de consumo de memória em equilíbrio absoluto com fluidez e tempo de resposta (zero lentidão).
- **Status:** Proposta de Alinhamento para Abertura de Spec F063 (Prioridade Zero).

---

## 1. Contexto e Decisão do Alexandre

Em 29/10/2026 ~00:34, após inspecionar o novo painel de diagnóstico de memória (F062) reportando pico de **540 MB (Working Set) / 417 MB (Private Bytes)**, o Alexandre determinou formalmente:

> *"Todas essas iniciativas pra melhora de consumo precisam passar na frente de qualquer coisa. Negocie isso com a claude para ver se a analise dela bate com a sua, se sim isso se torna prioridade zero, lembrando que precisa de um equilibrio. Não posso gerar lentidão em nada, mesmo que seja pra baixar a memória."*

---

## 2. Diagnóstico Técnico do Consumo Atual

Medições reais realizadas no ambiente Windows do Alexandre (PID 96340 `KyraFiles.exe` vs PID 15420 `explorer.exe`):

1. **Estado em Repouso após estabilização:**
   - `explorer.exe`: Working Set **194.7 MB** | Private Memory **656.3 MB**
   - `KyraFiles.exe`: Working Set **238.2 MB** | Private Memory **414.4 MB**
   - `KyraFiles.HotkeyService.exe`: Working Set **6.4 MB** | Private Memory **1.0 MB**
   - `KyraFiles.Watchdog.exe`: Working Set **5.2 MB** | Private Memory **856.0 KB**
   - `KyraFiles.App.IndexService.exe`: **0 MB** (desarmado pelo Gate F021)
   - `KyraFiles.App.Server.exe`: **0 MB** (inativo em repouso)

2. **Causas do Pico de 540 MB no KyraFiles:**
   - **WinUI 3 / DirectComposition / DirectX:** Custo fixo inicial do pipeline de GPU e composição XAML (~150 MB a 200 MB).
   - **Gerenciamento do Windows Lazy Working Set:** Quando a máquina possui memória livre, o gerenciador de memória do Windows não recolhe as páginas de RAM física para evitar I/O redundante.
   - **Achado Crítico no Código (`App.xaml.cs:484-515`):** O método `TryEmptyWorkingSetWhenIdle` possui a lógica de disparar um GC agressivo com o comentário `// Try to empty the working set`, mas **a chamada de API Win32 `K32EmptyWorkingSet` não foi incluída** no código final. Além disso, o método só é acionado quando todas as janelas são fechadas e o app vai para a bandeja (modo headless).

---

## 3. As 4 Iniciativas Propostas e Análise de Risco de Latência

A diretriz do Alexandre exige **equilíbrio absoluto: zero lentidão**. Qualquer técnica de economia de RAM que introduza engasgos (jank), atraso de clique ou cold-start de páginas de disco é terminantemente vetada.

Abaixo está o plano ponderado para alinhamento entre AGY e Claude:

### Iniciativa 1: Despejo Suave de Working Set ao Minimizar / Ociosidade Longa
- **O que faz:** Ao minimizar a janela ou quando o app estiver em segundo plano ocioso há mais de 60 segundos, executar em thread de background com prioridade baixa:
  1. `GC.Collect(2, GCCollectionMode.Optimized)` (sem compactação forçada de LOH se houver atividade).
  2. Chamada de `K32EmptyWorkingSet(processHandle)`.
- **Análise de Latência (Salvaguarda de Equilíbrio):**
  - **Risco:** Se o Working Set for despejado para o disco (pagefile), reabrir a janela pode causar atraso perceptível de 300–800 ms.
  - **Mitigação:** No Windows 10/11 com RAM sobrando, o `EmptyWorkingSet` move as páginas para a *Standby List* na própria RAM. A reativação é uma *soft page fault* (resolvida em microssegundos na RAM, sem acesso a disco).
  - **Critério de Aceite Mandatório:** O tempo de restauração da janela minimizada medido antes e depois deve ser indistinguível (diferença < 20 ms). Se houver degradação perceptível, a agressividade é reduzida.

### Iniciativa 2: Isolamento do Painel de Pré-visualização (Preview Host Out-of-Process)
- **O que faz:** Mover a decodificação de codecs de áudio/vídeo, renderização de PDFs pesados e visualizadores de documentos para um processo worker sob demanda (padrão `prevhost.exe` do Explorer) ou descarregar os controles pesados imediatamente quando o painel for ocultado ou outro item for selecionado.
- **Análise de Latência:**
  - **Risco:** Atraso ao exibir a prévia de um arquivo se um processo novo precisar subir do zero a cada clique.
  - **Mitigação:** Manter controles leves (texto puro, imagens simples) in-process instantâneos; isolar apenas decodificadores pesados de mídia/PDF que sabidamente causam inchaço de 150+ MB.

### Iniciativa 3: Workers Sob Demanda para Compactação/Descompactação (7z/Zip)
- **O que faz:** As bibliotecas 7-Zip/SharpCompress e buffers de extração de gigabytes são carregados apenas durante operações ativas de arquivo, garantindo liberação total de memória ao término.
- **Análise de Latência:** Zero impacto na navegação normal de pastas.

### Iniciativa 4: Módulos F060 com Inicialização Estritamente Lazy
- **O que faz:**
  - **SFTP (SSH.NET):** Carrega assemblies e buffers de criptografia apenas ao conectar em URLs `sftp://`, desconectando após timeout de ociosidade.
  - **Editor Hexadecimal:** Janela dedicada sob demanda para visualização binária.
  - **Scripts (Jint):** Instanciação do engine JS sob demanda descartada após a execução do script.
- **Análise de Latência:** Zero impacto no explorador diário.

---

## 4. Proposta de Ação Imediata (Fila de Prioridade Zero)

Propomos abrir formalmente a **Spec F063 — Contenção de Memória em Repouso sem Perda de Fluidez**:
1. **Fase 1 (Imediata / Baixo Risco):** Ativação inteligente de `K32EmptyWorkingSet` em minimização e ociosidade prolongada, com medição rigorosa de latência de restauração via script de UI Automation (meta: retorno < 50 ms e Working Set em segundo plano < 80 MB).
2. **Fase 2 (Preview Lifecycle):** Limpeza proativa de buffers de preview ao desselecionar ou fechar o painel lateral.
3. **Fase 3 (Workers e F060):** Carregamento lazy dos módulos avançados da F060.

Aguardamos o parecer e considerações da Claude sobre a abordagem e o equilíbrio de latência.
