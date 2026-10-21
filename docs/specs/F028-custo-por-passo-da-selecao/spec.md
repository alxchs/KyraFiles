# F028 — Custo por passo da seleção: registro, QuickLook e PgUp/PgDn

Status: **APROVADA** pelo Alexandre em 20/10/2026 ("arrume tudo num único passo"). Origem: trace do thread de UI com `Down` segurado (`docs/agents/evidence/F027/trace_down_f027.speedscope.json`).

## Contexto
Com `Down` segurado a posição na grade e a barra de rolagem não atualizam em tempo real. O trace mostrou, no thread de UI, trabalho evitável a cada passo: varredura do registro (`IsVSCodeInstalled`, 6,7%), reconstrução do toolbar (11%), detecção de QuickLook/PowerToys Peek (4,3%). O PgUp/PgDn processa só ~2 teclas/s.

## Requisitos
- **FR-1:** Ler `IDEPath`/`IDEName` não varre o registro mais de uma vez por execução do app.
- **FR-2:** Mudar a seleção não detecta provedores de pré-visualização (QuickLook, Seer, PowerToys Peek) enquanto a seleção ainda está mudando; o popup aberto acompanha a seleção 150 ms depois da última mudança.
- **FR-3:** PgUp/PgDn usam o comportamento nativo da lista (uma página por tecla, item sempre visível).
- **FR-4:** Home/End/Ctrl+Home/Ctrl+End continuam como estão.

## Critérios de aceite
- **AC-1:** Segurar `Down` 5 s e `PageDown` 5 s na pasta de 1.600 itens: linhas alcançadas maiores ou iguais às da F027 (79 e 61), CPU de UI menor (`f025_hold_key.ps1`, trace via `f025_speedscope_top.ps1`; no trace de `Down`, `IsVSCodeInstalled` e `DetectAvailability` somem da lista).
- **AC-2:** "Abrir no IDE" continua aparecendo quando o VS Code está instalado.
- **AC-3:** Com o popup de preview aberto, mudar a seleção e parar atualiza o preview em até ~300 ms.
- **AC-4:** PgDn/PgUp deixam o item selecionado visível.

## Fora de escopo
Shift/Ctrl nas teclas Home/End (regressão da F024); custo nativo do WinUI.
## [NEEDS CLARIFICATION]
Nenhum.

## Histórico
- **20/10/2026 (ampliação, aprovada pelo Alexandre: "arrume tudo num único passo"):** a medição mostrou que, com a tecla segurada, as repetições se acumulam na fila, a tela fica atrás da tecla e a linha com foco é reciclada para outro item (o foco "vai e volta"). Acrescentados:
  - **FR-5:** com uma tecla de navegação (setas, PgUp, PgDn) segurada, a próxima repetição só é aceita depois que o passo anterior foi desenhado na tela; nenhuma repetição acumulada continua mexendo a seleção depois de soltar.
  - **FR-6:** a lista mantém a folga padrão de linhas pré-criadas (igual ao upstream), para a rolagem com o mouse não esvaziar a tela.
  - **AC-5:** em quadros capturados com `PgDn` e `Down` segurados, a linha selecionada anda só para frente e a barra de rolagem acompanha (`tools\perf\f028_contact_sheet.ps1`).
- **20/10/2026 (2ª ampliação):** trace com `PgDn` mostrou o carregamento das propriedades de cada linha (`StorageFile`, nuvem, propriedades) em 16% a 25% do thread de UI, entre uma página e outra.
  - **FR-7:** enquanto uma tecla de navegação está pressionada, as linhas que aparecem não carregam propriedades estendidas; as que ficarem visíveis carregam 200 ms depois de soltar.
