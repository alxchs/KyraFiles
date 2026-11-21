# SPECS (indice mestre)

Gerado automaticamente a partir de `docs/production/production-control.json` (protocolo v2) em 2026-11-21T14:30:50.8276102-03:00 via `tools/Sync-ProductionDocs.ps1`. Nao editar a mao.

```json
{
    "total_specs":  78,
    "total_pontos":  366,
    "finalizadas":  {
                        "quantidade":  46,
                        "pontos":  201,
                        "percentual":  54.9
                    },
    "em_andamento":  {
                         "quantidade":  24,
                         "pontos":  105,
                         "percentual":  28.7
                     },
    "fila":  {
                 "quantidade":  7,
                 "pontos":  47,
                 "percentual":  12.8
             },
    "dependendo_alexandre":  {
                                 "quantidade":  0,
                                 "pontos":  0,
                                 "percentual":  0
                             },
    "impedidas":  {
                      "quantidade":  0,
                      "pontos":  0,
                      "percentual":  0
                  },
    "invalidas":  {
                      "quantidade":  1,
                      "pontos":  13,
                      "percentual":  3.6
                  }
}
```

## Resumo

| Categoria | Specs | Pontos | % |
|---|--:|--:|--:|
| finalizadas | 46 | 201 | 54.9 |
| em_andamento | 24 | 105 | 28.7 |
| fila | 7 | 47 | 12.8 |
| dependendo_alexandre | 0 | 0 | 0.0 |
| impedidas | 0 | 0 | 0.0 |
| invalidas | 1 | 13 | 3.6 |

## Agrupamento

**finalizadas (46)**: F002, F003, F006, F008, F009, F010, F012, F013, F018, F019, F021, F024, F027, F028, F031, F036, F037, F038, F040, F042, F043, F044, F045, F047, F048, F049, F052, F053, F056, F057, F058, F067, F068, F069, F071, F072, F081, F082, F083, F084, F087, F088, F091, F093, F092, F094

**em_andamento (24)**: F004, F005, F011, F014, F017, F020, F022, F023, F026, F029, F030, F032, F033, F034, F035, F060, F061, F062, F063, F065, F070, F086, F089, F090

**fila (7)**: F039, F041, F046, F050, F051, F054, F055

**dependendo_alexandre (0)**: -

**impedidas (0)**: -

**invalidas (1)**: F015

## Tabela mestra

| ID | Legado | Escopo | Pts | Estado | Categoria | Responsavel | Entrega/Commit |
|---|---|---|--:|---|---|---|---|
| SPEC-0001 | F002 | independent column resize | 3 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0002 | F003 | new vs duplicate tab | 3 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0003 | F004 | classic menu bar | 3 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0004 | F005 | preview details toggle | 2 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0005 | F006 | search provider | 5 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0006 | F008 | compact density | 2 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0007 | F009 | separate fonts | 3 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0008 | F010 | all explorer columns | 5 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0009 | F011 | search engine choice | 3 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0010 | F012 | explorer replacement | 8 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0011 | F013 | f3 agent ransack | 5 | completed | finalizadas | unknown | e6dcf50ae |
| SPEC-0012 | F014 | sistema help contextual | 5 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0013 | F015 | indice de busca | 13 | cancelled | invalidas | unknown | e6dcf50ae, f8e72cc9c |
| SPEC-0014 | F017 | win e persistent launcher | 5 | in_validation | em_andamento | unknown | e6dcf50ae, f8e72cc9c |
| SPEC-0015 | F018 | sobre e atualizacoes | 3 | completed | finalizadas | unknown | docs/releases/2026-11-15_1315_build-v4.3.17.md |
| SPEC-0016 | F019 | efeito visivel das formas | 3 | completed | finalizadas | unknown | e6dcf50ae, 28c3afed6 |
| SPEC-0017 | F020 | aba unica por endereco e posicao settings | 2 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0018 | F021 | bloqueio indexservice | 5 | completed | finalizadas | unknown | 8913475a2 |
| SPEC-0019 | F022 | correcao startup e instalador | 3 | in_validation | em_andamento | unknown | e6dcf50ae |
| SPEC-0020 | F023 | hybrid fast search | 8 | in_validation | em_andamento | unknown | 8913475a2, 5364a15b4 |
| SPEC-0021 | F024 | choque performance navegacao | 8 | completed | finalizadas | unknown | c0d3ae468 |
| SPEC-0022 | F026 | selecao coalescida | 5 | in_validation | em_andamento | unknown | c0d3ae468 |
| SPEC-0023 | F027 | checkbox preguicoso | 3 | completed | finalizadas | unknown | c0d3ae468 |
| SPEC-0024 | F028 | custo por passo da selecao | 5 | completed | finalizadas | unknown | c0d3ae468, ec3c053ef |
| SPEC-0025 | F029 | setas como explorer | 3 | in_validation | em_andamento | unknown | 8913475a2, 06bc7628e |
| SPEC-0026 | F030 | preview fechado tecla segurada | 3 | in_validation | em_andamento | unknown | 8913475a2, ba2af8149 |
| SPEC-0027 | F031 | abas rapidas | 5 | completed | finalizadas | unknown | faef02f5f |
| SPEC-0028 | F032 | fixar aba | 3 | in_validation | em_andamento | unknown | 8913475a2, ba2af8149 |
| SPEC-0029 | F033 | copiar mover para | 3 | in_validation | em_andamento | unknown | 8913475a2, ba2af8149 |
| SPEC-0030 | F034 | lista sem checkbox | 2 | in_validation | em_andamento | unknown | 8913475a2, eb15c68b6 |
| SPEC-0031 | F035 | second instance | 5 | in_validation | em_andamento | unknown | 2ab7a65bc, 8913475a2 |
| SPEC-0032 | F036 | copy queue | 8 | completed | finalizadas | unknown | d036a49de |
| SPEC-0033 | F037 | bulk rename preview | 5 | completed | finalizadas | unknown | - |
| SPEC-0034 | F038 | named workspaces | 5 | completed | finalizadas | agy | - |
| SPEC-0035 | F039 | disk usage map | 8 | not_started | fila | unknown | - |
| SPEC-0036 | F040 | duplicate finder | 5 | completed | finalizadas | agy | - |
| SPEC-0037 | F041 | folder compare sync | 8 | not_started | fila | unknown | - |
| SPEC-0038 | F042 | saved searches | 3 | completed | finalizadas | agy | v4.3.22 |
| SPEC-0039 | F043 | operation history | 5 | completed | finalizadas | agy | v4.3.22 |
| SPEC-0040 | F044 | spacebar quick look | 3 | completed | finalizadas | unknown | 13f76b6eb |
| SPEC-0041 | F045 | project actions | 5 | completed | finalizadas | agy | v4.3.22 |
| SPEC-0042 | F046 | deep network search | 8 | not_started | fila | unknown | - |
| SPEC-0043 | F047 | build folder cleaner | 3 | completed | finalizadas | unknown | - |
| SPEC-0044 | F048 | file diff | 5 | completed | finalizadas | agy | - |
| SPEC-0045 | F049 | links symlinks junctions | 5 | completed | finalizadas | AGY | bbd610f31 |
| SPEC-0046 | F050 | embedded terminal | 8 | not_started | fila | unknown | - |
| SPEC-0047 | F051 | code search | 5 | not_started | fila | unknown | - |
| SPEC-0048 | F052 | git repo view | 5 | completed | finalizadas | unknown | 2e401a04f |
| SPEC-0049 | F053 | encoding eol column | 2 | completed | finalizadas | AGY | 5b7fdd32c |
| SPEC-0050 | F054 | technical preview | 5 | not_started | fila | unknown | - |
| SPEC-0051 | F055 | repo map | 5 | not_started | fila | unknown | - |
| SPEC-0052 | F056 | copy path formats | 2 | completed | finalizadas | unknown | d036a49de |
| SPEC-0053 | F057 | dialog button shortcuts | 3 | completed | finalizadas | unknown | d036a49de |
| SPEC-0054 | F058 | installer window to front | 3 | completed | finalizadas | agy | v4.3.22 |
| SPEC-0055 | F060 | diferenciais sftp script hex split | 13 | in_validation | em_andamento | unknown | - |
| SPEC-0056 | F061 | win e sempre pronto | 5 | in_validation | em_andamento | unknown | 0856cd321, cd00407d3 |
| SPEC-0057 | F062 | diagnostico memoria ecossistema | 5 | in_validation | em_andamento | unknown | - |
| SPEC-0058 | F063 | otimizacao memoria zero regressao | 8 | in_validation | em_andamento | unknown | - |
| SPEC-0059 | F065 | busca nome e caminho | 5 | in_validation | em_andamento | unknown | 28ecad12a |
| SPEC-0060 | F067 | sponsors e modern powershell | 3 | completed | finalizadas | unknown | - |
| SPEC-0061 | F068 | documentacao local offline | 5 | completed | finalizadas | unknown | - |
| SPEC-0062 | F069 | sair para explorer apos quedas | 3 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0063 | F070 | github login oauth | 3 | in_validation | em_andamento | unknown | 28ecad12a |
| SPEC-0064 | F071 | integrity downloaded files | 5 | completed | finalizadas | unknown | 611e5f342 |
| SPEC-0065 | F072 | documentacao multilingue | 5 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0066 | F081 | identity kyrafiles | 8 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0067 | F082 | settings modal window | 5 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0068 | F083 | kyrafiles brand and icon | 3 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0069 | F084 | kyrafiles module renaming | 3 | completed | finalizadas | unknown | 28ecad12a |
| SPEC-0070 | F086 | instancia unica | 3 | in_validation | em_andamento | unknown | 28ecad12a |
| SPEC-0071 | F087 | identidade pacote e dados | 5 | completed | finalizadas | alexandre/agy | 0d6047ccd, v4.3.19.0 |
| SPEC-0072 | F088 | remocao vinculo ancestral | 2 | completed | finalizadas | unknown | b4897aa82, 1fcf37136, 0d6047ccd |
| SPEC-0073 | F089 | fontes nao aplicadas | 3 | in_validation | em_andamento | unknown | fc2101f59, b4897aa82 |
| SPEC-0074 | F090 | escala da interface | 5 | in_validation | em_andamento | unknown | - |
| SPEC-0075 | F091 | identidade kyrafiles no codigo e migracao transparente | 8 | completed | finalizadas | alexandre/agy | 0d6047ccd, 15fdaae16 |
| SPEC-0076 | F093 | compilacao rapida e deploy local via quick ship | 3 | completed | finalizadas | alexandre/agy | docs/releases/2026-11-15_1315_build-v4.3.17.md |
| SPEC-0077 | F092 | paridade e reestilizacao da barra lateral nativa | 5 | completed | finalizadas | alexandre/agy | docs/releases/2026-11-15_1315_build-v4.3.17.md |
| SPEC-0078 | F094 | busca everything escopo global e historico de buscas | 5 | completed | finalizadas | claude/agy | - |
