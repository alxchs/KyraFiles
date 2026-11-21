# F094 — Medição de desempenho (NFR-001)

Medido em 21/11/2026, Heloize-PC, Everything aberto na sessão do usuário, `es.exe` 1.1.0.38.
Comando: `tools/perf/measure_everything_search.ps1` (7 execuções por caso, mesmos argumentos que o app monta: `-tsv -no-header -attrib -size -dm -n 500`, consulta `*.exe`). Valores em mediana.

| Caso | Itens | Primeiro resultado | Fim | Mín/máx (fim) |
|---|---|---|---|---|
| Antes: pasta aberta (`Downloads`) implícita | 15 | 105,1 ms | 112,7 ms | 100,7 / 117,9 ms |
| Depois: índice todo (sem pasta digitada) | 500 | 157,4 ms | 187,2 ms | 155,7 / 369,6 ms |
| Depois: pasta digitada (`*.exe <Downloads>`) | 15 | 105,5 ms | 112,7 ms | 101,7 / 130,0 ms |

## Leitura
- **Mesma busca, mesmo escopo: sem regressão** (105,1 → 105,5 ms até o primeiro resultado; 112,7 → 112,7 ms até o fim). CONFIRMED pela tabela acima.
- Busca no índice todo é uma consulta diferente, pedida pela spec (FR-001): traz 33 vezes mais itens e custa +52 ms até o primeiro resultado, abaixo de 0,2 s. O teto de 500 itens (NFR-002) continua valendo.
- Caminho com espaço entre aspas (`-path "C:\Program Files" *.txt`): 500 itens, 0 fora do escopo (AC-3, CONFIRMED no `es.exe`; no app, NOT TESTED).
- Antes do Everything ser aberto na sessão, só o serviço rodava (sessão 0) e o `es.exe` saía com erro 8 sem resultados; é o caso coberto pela correção de FR-007.

Pico de memória do app: NOT TESTED (precisa do app aberto).
