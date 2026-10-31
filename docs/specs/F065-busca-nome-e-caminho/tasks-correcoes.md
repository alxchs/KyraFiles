# F065 — Busca: Nome e Caminho nas colunas — TASKS DE CORREÇÃO

Status: APROVADO (Claude, 30/10/2026, após revisão; execução autorizada pelo Alexandre: "Assuma o que ela não começou ainda que consiga acabar até as 14:00"). Complementa `tasks.md` (T0..T5, entregues pela AGY). Branch: `feature/f065-correcoes`.

Motivo: os testes do T3 não chamavam o código do app (recalculavam a regra e usavam colunas em ordem diferente da saída real do `es.exe`); o motor Agent Ransack também cria resultados sem opacidade (segunda ocorrência do mesmo sintoma, regra de causa comum); o T2 aplicou duas correções para a mesma coisa; a medição T0/T4 rodou o `es.exe` sozinho, fora do app.

| # | Tarefa | Atende | Dono | Pronto quando | Estado |
|---|---|---|---|---|---|
| T6 | Opacidade do item de lista nasce 1 (o oculto continua esmaecido onde é definido) | FR-001, AC-1..3, AC-6 | Claude | build 0/0; decisão em `DECISIONS.md` | feito (Claude, 30/10) |
| T7 | Leitura da linha `caminho<TAB>atributos<TAB>tamanho<TAB>data` do `es.exe` numa classe pura usada pelo provedor; testes com linhas reais que falham se a leitura for quebrada | FR-001, AC-1, AC-6 | Claude | testes novos passam e falham com a leitura quebrada (saída colada) | feito (Claude, 30/10) |
| T8 | Remover o BOM adicionado em `ShellViewModel.cs` e `DetailsLayoutPage.xaml.cs` | qualidade | Claude | diff sem BOM | feito (Claude, 30/10) |
| T9 | Apagar `SearchItemOpacityAndPathTests.cs` | qualidade | Claude | suíte sem os testes que só recalculavam a regra | feito (Claude, 30/10) |
| T10 | Handoff com estado honesto por `AC-n` | todos | Claude | handoff do Claude | feito (Claude, 30/10) |
| T11 | T2: provar qual das duas correções da coluna Caminho resolve e manter só uma | FR-002, AC-4, AC-5, AC-7 | AGY | captura às 09:00 de 31/10 antes/depois de cada variante | pendente |
| T12 | Provas de tela por motor (Everything, índice, nativo, Agent Ransack) e desempenho medido no app | AC-1..7, NFR-001 | AGY | capturas e medianas às 09:00 de 31/10 | pendente |
