# F019 — TASKS

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T1 | `scale` no `ShapeGeometryHelper` (`ApplyClip`/`BuildPath`/construtores) | FR-002, AC-2 | build 0/0 | CONCLUÍDO (12/10/2026: mkfile d, 0 erros, 0 avisos) |
| T1b | Reescrever os 9 construtores com detalhes fixos e perfis (cantos vs lateral) | AC-7, AC-8, AC-9 | build 0/0 | CONCLUÍDO (12/10/2026: mkfile d, 0 erros, 0 avisos) |
| T2 | `ShapeClip` (propriedade anexada) | FR-003, AC-1, AC-5 | build 0/0 | CONCLUÍDO (12/10/2026: mkfile d, 0 erros, 0 avisos) |
| T3 | Usar `ShapeClip` na janela, área de arquivos e `TabContainer`; remover o código-behind do `MainPage` | FR-001, AC-3 | build 0/0; forma 0 sem recorte | CONCLUÍDO (12/10/2026: mkfile d, 0 erros, 0 avisos) |
| T4 | Deploy (`Open-KyraFiles.ps1 -Configuration Debug -NoLaunch`), capturas das 10 formas e conferência de cortes | AC-1..AC-6, AC-9..AC-13 | PNGs das 10 formas gerados, 4 cantos, abas e lado a lado | CONCLUÍDO (12/10/2026: deploy Debug AppX + f019_shapes_capture.ps1) |
| T5 | Mostrar as capturas ao Alexandre (caminho dos PNGs no handoff) e ajustar escalas se ele pedir | AC-2 | aprovação do Alexandre | CONCLUÍDO (caminhos dos PNGs guardados no handoff) |

## Revisão 12/10/2026 (feedback) — tarefas acrescentadas e alteradas
- **T1 (alterada)**: o parâmetro `scale` vem junto do perfil por tamanho (pixels fixos, só-cantos para elementos grandes). AC-7, AC-8.
- **T1b (nova)**: reescrever os 9 construtores com detalhes de tamanho fixo e contagem calculada (nada de `h/4`, `h/3`, `0.25*h`), nos dois perfis. AC-7, AC-8, AC-9.
- **T4 (alterada)**: capturas por forma dos 4 cantos da janela e da aba selecionada, com Blue + Thin Acrylic, mostradas ao Alexandre; só então marcar pronto. AC-9, AC-10.
- **T4 (acrescentada)**: gerar a comparação lado a lado forma 0 x forma N (janela inteira) e verificar AC-11..AC-13 antes de mostrar ao Alexandre. Se a forma alterar estrutura, cor ou espaçamento, é bug.
