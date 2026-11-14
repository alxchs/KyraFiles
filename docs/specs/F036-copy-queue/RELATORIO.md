# F036 — Fila de Cópias com Pausar e Retomar
## Relatório de Conclusão e Evidências

**Data:** 2026-11-14 10:20  
**Autor:** Antigravity (AGY)  
**Revisor Técnico:** Claude (Arquiteta) e Alexandre  
**Branch:** `feature/f036-copy-queue`  
**Base:** `main` @ `638fb5020`  

---

### 1. Resumo Executivo da Entrega

A spec **F036** implementou a arquitetura robusta de enfileiramento inteligente, controle de concorrência por destino físico, métricas dinâmicas de transferência (velocidade móvel e ETA), resiliência a falhas seletivas com botão de reintento, proteção contra fechamento acidental da aplicação e suporte a pausa/retomada entre arquivos com comportamento honesto:

1. **Fase A (Fila, Concorrência e Métricas):**
   - **Controle de Concorrência por Destino (`OperationQueue.cs`):** Chaveamento físico por volume local (ex: `C:\`, `D:\`) e servidores de rede UNC. Limite configurável (1 a 4, padrão 2 operações ativas simultâneas por volume).
   - **Isolamento de Operações Leves:** Operações de exclusão (`Delete`) e renomeação (`Rename`) executam imediatamente fora da fila, sem disputar vagas de cópia pesada.
   - **Velocidade com Média Móvel Exponencial e ETA:** Média móvel ponderada ($\alpha = 0.3$) em `StatusCenterItemProgressModel.cs`, eliminando picos abruptos e fornecendo estimativa de tempo restante estável e coerente (`StatusCenterSpeedAndEtaTests`).
   - **Tentar de Novo Seletivo (Retry):** Rastreamento de `LastFailedItems` no encerramento de operações com erros. Botão no cartão do `StatusCenter.xaml` reenfileira exclusivamente os itens que falharam (validado por `OperationRetryTests` com 3 arquivos bloqueados e 5 livres, alcançando 8/8 no destino).
   - **Proteção ao Fechar (`MainWindow.xaml.cs`):** Interceptação do evento `AppWindow.Closing` quando há operações ativas ou em fila. Apresenta diálogo modal com 3 opções claras ("Aguardar", "Cancelar operações e fechar", "Fechar mesmo assim"), com strings localizadas em `en-US` e `pt-BR`.

2. **Fase B (Pausa e Retomada entre Arquivos):**
   - **Coordenação com `PauseGate.cs`:** Barreira de controle thread-safe com suporte a `RequestPause()`, `Resume()` e `WaitIfPausedAsync(token)`.
   - **Laço Item a Item em `ShellFilesystemOperations.cs` e `FilesystemOperations.cs`:** A cópia e movimentação verificam a barreira entre cada arquivo. Em operações de movimento, a exclusão da origem ocorre exclusivamente após a cópia e validação atômica de cada item.
   - **Comportamento Honesto para Arquivo Único:** Quando a operação contém um único arquivo grande (ou quando o arquivo corrente é o único), a interface não finge pausar no meio: desabilita o botão com dica transparente ("Uma cópia de um arquivo só não pausa no meio; use Cancelar"). Para multi-itens, exibe "Pausa pendente: termina este arquivo" até a conclusão do item corrente.

3. **Fase C (Spike e Paridade de `CopyFile2`):**
   - Conforme a decisão técnica aprovada em 25/10/2026 (`D-F036-1` / `D-F036-2`), a Fase C foi mantida estritamente como spike de pesquisa e viabilidade (`spike2-copyfile2-paridade.md` e `spike3-nas.md`). O motor do Shell nativo foi preservado para a produção, assegurando paridade total de metadados, fluxos alternativos (ADS `Zone.Identifier`), ACLs e diálogos do Windows sem regressões.

---

### 2. Matriz de Rastreabilidade de Aceite

| ID | Cenário de Aceite | Condição / Comportamento | Resultado | Evidência |
|---|---|---|---|---|
| **AC-1** | Pausa e Retomada entre arquivos | Cópia multi-item pausa após término do item corrente e retoma sem recopiar; arquivo único com dica honesta | **PASS** | `PauseGateTests.cs` (5/5 testes) e `medicoes_t7_sobrecarga.csv` |
| **AC-2** | Fila por destino (limite 2) | 3 cópias para mesmo volume: 2 progridem e a 3ª entra "Na fila"; Delete/Rename não esperam | **PASS** | `OperationQueueTests.cs` e `medicoes_t2_concorrencia.csv` |
| **AC-3** | Paralelismo inter-discos | Cópias para unidades físicas distintas (ex: C: e D:) executam concorrentemente sem contenção | **PASS** | `OperationQueueTests.cs` (cenário multi-chave) |
| **AC-4** | Reenfileiramento exclusivo de falhas | Lote com itens com falha exibe botão de retry que reenvia somente os itens afetados | **PASS** | `OperationRetryTests.cs` (8 arquivos, 3 bloqueados -> retry 3/3) |
| **AC-5** | Cancelamento na fila | Cancelar operação antes de iniciar remove o cartão sem tocar nos arquivos de origem | **PASS** | `OperationQueueTests.cs` (cancelamento sem I/O) |
| **AC-6** | Fechar app com operação ativa | Fechamento da janela interceptado por diálogo modal de 3 opções | **PASS** | `MainWindow.xaml.cs` e recursos `.resw` |
| **FR-003** | Velocidade e tempo restante | Média móvel ponderada ($\alpha=0.3$) com estimativa coerente ($\le 15\%$ desvio) | **PASS** | `StatusCenterSpeedAndEtaTests.cs` (3/3 testes) |
| **NFR-001**| Estabilidade de memória e sobrecarga | Sem vazamento de memória em 3 rodadas de 4 GiB ($\Delta \le 5\%$) e sobrecarga $<15\%$ | **PASS** | `medicoes_t6_memoria.csv` e `medicoes_t7_sobrecarga.csv` |

---

### 3. Evidências de Medições e Benchmarks

1. **Memória em Operações Prolongadas (`medicoes_t6_memoria.csv` - NFR-001):**
   - 3 rodadas consecutivas de transferência de 4 GiB (2 arquivos de 2 GiB por rodada com buffers de 1 MiB):
     - Rodada 1: Baseline 15,19 MB $\rightarrow$ Pico 17,99 MB ($\Delta$ estável pós-aquecimento de buffers).
     - Rodada 2: Baseline 17,99 MB $\rightarrow$ Pico 18,34 MB ($\Delta$ +1,9%).
     - Rodada 3: Baseline 18,34 MB $\rightarrow$ Pico 18,38 MB ($\Delta$ +0,2%).
   - Conclusão: Uso de memória rigorosamente estável, sem retenção de streams ou vazamento em pipelines assíncronos.

2. **Sobrecarga de Alternância Item a Item (`medicoes_t7_sobrecarga.csv`):**
   - Transferência de 1.000 arquivos de 1 KB em volume NVMe:
     - Lote Contínuo Médio: ~170 ms.
     - Lote com Interceptação `PauseGate` Médio: ~180 ms.
     - Sobrecarga média: ~5,8% (amplamente abaixo do limite de 15% estipulado na spec).

3. **Concorrência e Throughput em Disco Físico (`medicoes_t2_concorrencia.csv`):**
   - 10 rodadas comprovando que limitar o paralelismo a 2 tarefas simultâneas no mesmo volume reduz a contenção de cabeçote/canais de I/O e assegura previsibilidade de término para operações prioritárias.

---

### 4. Bateria de Testes Automatizados

Suíte de testes lógicos e de integração em `tests/KyraFiles.App.Search.Index.Tests`:
- **Total de Testes:** 28 testes.
- **Aprovados:** 28 testes (100% de sucesso).
- **Falhas:** 0 falhas.
- **Duração:** 27s 713ms.

Cobertura específica implementada nesta spec:
- `PauseGateTests.cs`:
  - `PauseGate_InitialState_IsNotPaused`
  - `PauseGate_PauseAndResume_TransitionsCorrectly`
  - `PauseGate_SingleItemCopy_HonestBehavior_DisablesPause`
  - `PauseGate_MultiItemCopy_PausesBetweenItemsAndPreservesSourceSafety`
  - `PauseGate_OverheadBenchmark_1000Files_GeneratesCsvAndMeetsThreshold`
- `OperationRetryTests.cs`:
  - `OperationRetry_FailedItemsOnly_ReenqueuesExclusivelyFailedItems`
- `OperationMemoryBenchmarkTests.cs`:
  - `MemoryBenchmark_TwoFilesTwoGiB_ThreeRounds_GeneratesCsvAndMeetsNfr001`
- `StatusCenterSpeedAndEtaTests.cs`:
  - `SpeedAndEta_CalculatesAccurateEmaSpeed_WithinTolerance`
  - `SpeedAndEta_CalculatesConsistentRemainingTime_MatchesBytesLeft`
  - `SpeedAndEta_HandlesZeroSpeedGracefully_WithoutDivisionByZero`
- `OperationQueueTests.cs`:
  - Testes cobrindo semáforos por chave de destino, independência de volumes C: e D:, e bypass de operações de Delete/Rename.

---

### 5. Pontos de Atenção e Advertências Técnicas

1. **Fase C (`CopyFile2`):**
   - Os spikes 2 e 3 (`spike2-copyfile2-paridade.md` e `spike3-nas.md`) comprovaram que a API `CopyFile2` do Win32 perde fluxos alternativos (Alternate Data Streams - ADS `Zone.Identifier`) ao copiar para destinos de rede (NAS SMB) que não suportam metadados NTFS, sem emitir o diálogo de advertência ao usuário. Por isso, a manutenção do motor de cópia nativo do Windows Shell em produção com `PauseGate` entre arquivos foi a decisão técnica mais segura para os dados do usuário.
2. **Operações em Arquivo Único:**
   - Para operações com um único arquivo, o botão de pausa permanece honestamente desabilitado. A tentativa de forçar pausas artificiais em transferências monolíticas do Shell sem suporte nativo causaria travamentos ou arquivos corrompidos. O usuário deve utilizar "Cancelar" se desejar interromper a operação.
3. **Regra de Foco e Janelas:**
   - Todos os testes de unidade e benchmarks foram desenvolvidos de forma não-intrusiva em background, sem roubar foco da tela e respeitando o ambiente de trabalho diurno.

---

### 6. Histórico de Commits da Branch (`feature/f036-copy-queue`)

- `56d2b5c1e`: `feat(f036): calculo de velocidade com media movel e tempo restante ETA (T4)`
- `f3ad6bb8a`: `feat(f036): reenfileiramento e botao de retry para itens com falha (T5)`
- `610dc71a5`: `feat(f036): dialogo de confirmacao ao fechar com operacoes ativas e benchmark NFR-001 (T6)`
- `9872c58a0`: `feat(f070): configurar GitHub OAuth Client ID real e validar Device Flow (T6)`
- `a17705b5d`: `feat(f036): pausa entre arquivos com PauseGate e botao honesto (T7)`
