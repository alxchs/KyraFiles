# Evidencia F035 T1 — Controle Negativo (Reproducao do SemaphoreFullException)

- **Data/Hora:** 2026-10-25 23:45:28
- **Ambiente:** FilesDev (Release x64) no codigo original sem correcao
- **Parametros:** 20 rodadas, 5 disparos simultaneos por rodada, janela principal estacionada
- **Pilha de erro capturada:**
```text
 ---> System.Threading.SemaphoreFullException: Adding the specified count to the semaphore would cause it to exceed its maximum count. |    at Files.App.Program..cctor()
```

## Tabela de Rodadas (gerada a partir de t1_controle_negativo.csv)

| Rodada | Disparos | Tempo Disparo (ms) | Eventos 1026 | Reproduzido | Observacao |
|---|---|---|---|---|---|
| 1 | 5 | 138 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 2 | 5 | 115 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 3 | 5 | 115 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 4 | 5 | 111 | 0 | FALSE | Nenhum evento 1026 |
| 5 | 5 | 142 | 0 | FALSE | Nenhum evento 1026 |
| 6 | 5 | 108 | 1 | TRUE | SemaphoreFullException capturada no log 1026 |
| 7 | 5 | 131 | 0 | FALSE | Nenhum evento 1026 |
| 8 | 5 | 131 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 9 | 5 | 125 | 0 | FALSE | Nenhum evento 1026 |
| 10 | 5 | 142 | 0 | FALSE | Nenhum evento 1026 |
| 11 | 5 | 105 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 12 | 5 | 163 | 0 | FALSE | Nenhum evento 1026 |
| 13 | 5 | 104 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 14 | 5 | 127 | 0 | FALSE | Nenhum evento 1026 |
| 15 | 5 | 107 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 16 | 5 | 122 | 0 | FALSE | Nenhum evento 1026 |
| 17 | 5 | 112 | 0 | FALSE | Nenhum evento 1026 |
| 18 | 5 | 107 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |
| 19 | 5 | 138 | 0 | FALSE | Nenhum evento 1026 |
| 20 | 5 | 134 | 3 | TRUE | SemaphoreFullException capturada no log 1026 |

## Veredito do Controle Negativo

- Rodadas testadas: 20
- Rodadas com crash reproduzido: 10
- Taxa de reproducao: 50%
- **Status:** CONTROLE NEGATIVO CONFIRMADO (PASS - defeito provado no codigo atual)
