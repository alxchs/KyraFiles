# F025 — Relatório parcial (20/10/2026, Claude)

Escopo medido até aqui: **tecla segurada** (`Down` e `End`, 5 s) na pasta de 1.600 itens. **Não medidos ainda:** troca de aba, abertura do app, trace de CPU, pasta real do Alexandre. Medição feita pelo Claude (a pedido do Alexandre, "impossível usar como está").

## Método (CONFIRMED: comandos e saída em `hold_key.csv`)
- Pasta: `C:\FilesUXLab\perf\1600` (`tools\perf\f025_setup_lab.ps1`, 1.600 arquivos de texto pequenos, sem miniaturas).
- Script: `tools\perf\f025_hold_key.ps1 -Label <x> -Key Down|End -Runs 3`. Tecla repetida a ~30 Hz com `keybd_event`; CPU = diferença de `TotalProcessorTime` ao redor da ação; UI = `SendMessageTimeout(WM_NULL)` a cada ~100 ms; memória = `WorkingSet64` e `PrivateMemorySize64` (MB).
- Ordem A-B-A, mesma máquina, mesma pasta, mesmo script, Release x64:
  - **A** = nosso build 4.3.1 (`feature/f025...`, commit `1ecea25ce`);
  - **B** = upstream puro no commit-base `0e3c17ca4` (4.2.37). Para compilar com o SDK instalado foram copiados do fork só `Directory.Build.props`, `Directory.Packages.props` e `nuget.config` (toolchain, sem mudança de comportamento). Sem isso o upstream não compila (CsWin32/analyzers).

## Resultados (CPU em ms por 5 s de tecla; 3 rodadas, mediana)
| Build | Down: CPU | Down: UI max (ms) | End: CPU | End: UI max (ms) | Private MB (após rodadas) | WS MB |
|---|---|---|---|---|---|---|
| A1 atual | 7.844 | 321 | 8.141 | 555 | 583 a 692 | 683 a 714 |
| B upstream | 8.281 | 110 | 7.375 | 694 | 297 a 427 | 429 a 534 |
| A2 atual | 7.141 | 444 | 2.688 | 21 | 423 a 603 | 536 a 701 |

## Leitura
1. **CPU ao segurar a tecla: o upstream puro gasta o mesmo que o nosso** (≈ 6 a 9 s de CPU em 5 s, ou seja ~1,5 núcleo). **CONFIRMED** nesta pasta. Logo, esse custo **não vem do nosso diff**; é do base (Files/WinUI). Isso apoia, só para este item, a tese da AGY.
2. **Nenhum dos dois "congelou" no meu teste:** o pico de latência da UI foi de 21 a 1.882 ms e nenhum ping estourou os 5 s; a UI se recuperou em ≤ 371 ms depois de soltar a tecla. O "congelamento total" do relato **não foi reproduzido** com esta pasta/script (OBSERVED). Pode depender da pasta real, de miniaturas, de abas ou do teclado físico: NOT TESTED.
3. **Memória: o nosso build usa mais.** Memória privada ~420 a 690 MB contra ~300 a 430 MB do upstream, e cresce de rodada em rodada nos dois (OBSERVED). A diferença (~+250 MB privados) é **nossa** (INFERRED: não isolei qual feature).
4. **Variação alta:** `End` no A2 deu ~2,7 s contra ~8 s no A1 com o mesmo build. Três rodadas não bastam para afirmar diferença fina entre A e B no CPU.
5. **Não concluo** nada sobre viabilidade do projeto. A tese "congênito do WinUI" só ficou confirmada para o custo de CPU de tecla segurada, não para o "impossível usar" relatado.

## Próximos passos propostos (PENDING DECISION do Alexandre)
- Medir **troca de aba** e **abertura do app** nos dois builds (ainda não medidos, são 2 dos 3 sintomas).
- Gravar **trace** (WPR/dotnet-trace) do nosso build com tecla segurada, para ver onde vão 7 s de CPU.
- Repetir na **pasta real** em que o Alexandre sente a lentidão (qual é? tamanho, tipos, miniaturas?).
- Caçar a diferença de ~250 MB de memória privada por bisseção das features F015 a F023.

---

## Adendo 1 (20/10/2026 15:05) — o Alexandre reproduz o problema real; medição refeita com a fila de teclas

Relato do Alexandre: o congelamento acontece na pasta de laboratório criada pelo Claude (`C:\FilesUXLab\perf\1600`) **e** em `D:\DriveD\Downloads` (1.577 itens: 611 `.pdf`, 215 `.png`, 172 `.jpg`, 10 pastas); **outras aplicações aguentam a tecla segurada**. Isso derruba a hipótese de que a pasta de laboratório "não é representativa": ela já trava para ele.

A primeira medição (acima) via só latência de UI e CPU e **não via a fila de teclas**. O script ganhou duas coisas (`f025_hold_key.ps1`, saída em `hold_key2.csv`): (a) o item focado ao final (UI Automation) e (b) o tempo de escoamento depois de soltar. `f025_thr_hold.ps1` mede CPU por thread só na janela da tecla.

Item final alcançado em 5 s com ~152 teclas enviadas a ~30 Hz (índice estimado: lista ordenada por nome, `backup_*` primeiro; **INFERRED** pela ordem de classificação, não lido da UI):

| Build | Rodadas (linha alcançada) | CPU por 5 s |
|---|---|---|
| Nosso 4.3.1 (`hold_key2.csv`, C1/T/D1) | 43, 36, 52, 59, 79 | 11,7 a 14,7 s |
| Upstream puro (C2/D2b) | 60, 80, 86, 60, 69 | 9,9 a 13,4 s |

- **OBSERVED:** nos **dois** builds o app processa só ~10 a 17 linhas/s das ~30 que o teclado manda; o resto é descartado ou coalescido. O escoamento depois de soltar é ≈ 0 (nenhuma linha anda depois de soltar), então o app não acumula fila: ele **perde** teclas enquanto está saturado.
- **OBSERVED (CPU por thread, janela de 5,1 s):** nosso build: thread de UI 3.797 ms (**74% de um núcleo**), outra thread 2.297 ms (provável compositor/render), total 8.327 ms. Upstream: UI 2.250 e 3.219 ms (44% e 63%), outra thread 2.2 a 2.7 s, total 5,5 a 7,1 s. O thread de UI é o gargalo nos dois; **no nosso build ele trabalha mais** (uma amostra só; **INFERRED**, pouca amostra).
- **INFERRED, não confirmado:** nosso build alcança menos linhas em média (mediana 52 contra 69) com mais CPU de UI. A variação entre rodadas é grande (36 a 79 no nosso) e **não permite afirmar** que somos mais lentos que o upstream; falta mais amostra.
- **NOT TESTED:** como o upstream se comporta para o Alexandre (a pergunta "o KyraFiles original também trava para você?" está aberta); o `Downloads` real (só li a lista de arquivos, não rodei a tecla lá).
- **Trace gerenciado** (`trace_atual_down.nettrace`, `dotnet-sampled-thread-time`, 15 MB): é tempo de relógio com threads em espera, dominado por `Task.InternalWaitCore` (bloqueio síncrono em thread do pool), `Directory.Exists`, `NamedPipeClientStream` (conexão de pipe), `WatchForDirectoryChanges`, `TryEnqueue`. **Não identifica** o custo do thread de UI, que é majoritariamente nativo (layout/render do WinUI). Para isso falta um trace nativo (WPR precisa de administrador).

### Leitura revisada
- A tese "o upstream tem o mesmo custo de CPU" **se sustenta**, mas ela **não** diz que é inviável: o base também só processa ~15 linhas/s nesta máquina com 1.600 itens. Quem decide se isso é aceitável é o Alexandre, comparando com o KyraFiles original que ele conhece.
- A saída tecnicamente viável não é "trocar o framework": é **reduzir o trabalho por mudança de seleção** (o thread de UI gasta ~250 a 300 ms de CPU por linha) e/ou **coalescer as repetições de tecla** (processar só a última por quadro). Isso é hipótese a testar por A/B, não correção pronta.
