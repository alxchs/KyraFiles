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
