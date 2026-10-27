# F056 - prova de tela de 27/10/2026 (janela 12:35-13:20)

Build testado: integra-f056-f057 (main integrada com F056 e F057), app registrado como FilesDev, aberto por iles-dev.exe <pasta do laboratorio>; menu aberto com clique direito guardado (Send-GuardedClick) e itens acionados por UI Automation; area de transferencia lida em PowerShell e comparada com o esperado calculado de forma independente (sem usar o PathFormatter).

| Rodada | Arquivo | AC-1 | AC-2 (WSL) | AC-3 (JSON) | AC-7 (3 arquivos) | Controle |
|---|---|---|---|---|---|---|
| 1 | resultado_rodada1_*.csv | **FAIL** (submenu nao achado na 1a tentativa) | NOT TESTED | PASS | PASS | PASS |
| 2 | resultado_rodada2_*.csv | PASS (7 formatos presentes) | PASS | PASS | PASS | PASS |

A rodada 1 reprovou em AC-1: o menu de contexto nao mostrou o submenu a tempo logo depois de o app abrir. Nas rodadas seguintes passou. NAO esta provado se foi lentidao de partida do app ou falha intermitente; fica como ponto a observar. O script tambem ajustou, entre as rodadas, a reabertura do menu entre AC-1 e AC-2 (o submenu antigo expirava).
