# F025 — Baseline do upstream e trace: o diagnóstico da F024 precisa de prova

Brief de **medição** (sem comportamento novo, sem código de produto). Escrito pelo Claude em 20/10/2026 depois de revisar a F024.
Executora: AGY. Revisor: Claude. Decisão final (continuar ou encerrar o projeto): Alexandre.

Estado deste brief: **APROVADO pelo Alexandre em 20/10/2026** ("quero seguir sua sugestão e tentativa").

## 1. Por que esta tarefa existe

A F024 terminou com o veredito "WinUI 3 é inviável, encerre o projeto". Esse veredito **não tem prova no repositório**:

- os scripts de medição foram apagados (continuam no git: `git show ecb9c6d29:tools/perf/<nome>`);
- a AC-001 (Home/End/PgUp/PgDn em ≤ 150 ms) nunca teve um número;
- as "causas raiz" (teclado a 30 Hz, remontagem de aba, instância única) não vieram de profiler;
- nunca se mediu o **upstream puro** na mesma pasta. Sem isso não se sabe se o culpado é o WinUI 3 (nesse caso o upstream trava igual) ou o nosso diff (F015 a F023).

Já existe uma referência antiga em `tools/perf/README.md` (upstream v4.2.9, 01/10/2026): ~610 MB de working set e 34% de um núcleo parado. Ela mostra que o upstream já é pesado, mas **não** mede teclado segurado nem troca de aba. Esta tarefa preenche o que falta.

## 2. Pergunta única que a tarefa responde

> Na mesma máquina, na mesma pasta de 1.600 itens, o **upstream puro** também congela ao segurar `Down`/`End`, também demora para trocar de aba e também passa de 1 GB?

- Se **sim**: a tese do framework ganha força; o custo do nosso diff é secundário.
- Se **não**: o problema está no nosso código e dá para achá-lo por bisseção (F015 a F023).
- Se **parcial**: separar por sintoma (teclado, aba, abertura, memória) e tratar cada um.

## 3. O que NÃO fazer

- Não alterar código de produto (`src/`). Esta tarefa só mede.
- Não declarar causa sem trace ou A/B. "Provável" é `INFERRED`.
- Não apagar scripts, saídas nem traces depois de usar: o que prova o número fica no repositório (`tools/perf/` e `docs/agents/evidence/F025/`). Só o **laboratório de arquivos** (a pasta com 1.600 itens) é descartável, e só depois de o Claude revisar.
- Não escrever "inviável", "congênito" nem "encerrar" no relatório. Quem decide é o Alexandre.
- Não commitar na `main`. Branch: `feature/f025-diagnostico-baseline-upstream`.

## 4. Passos (cada um é um commit)

### P1 — Restaurar os scripts
`git show ecb9c6d29:tools/perf/setup_1600_lab.ps1`, `f024_keyboard_nav_test.ps1`, `f024_watcher_sync_test.ps1` e `cleanup_f024_lab.ps1` voltam para `tools/perf/` (renomear com prefixo `f025_`). Ajustar para receber o **executável alvo por parâmetro** (`-Exe`), para rodar o mesmo script nos dois builds.
Pronto quando: `pwsh -File tools\perf\f025_keyboard_nav.ps1 -Exe <caminho> -WhatIf` imprime o plano sem abrir nada.

### P2 — Lab de 1.600 itens
Criar o laboratório em `C:\KyraFilesUXLab\perf\1600` e registrar cada caminho em `docs/specs/F024-choque-performance-navegacao/test_artifacts_registry.md` (ou no registro novo da F025). Esta é a **única** coisa descartável.

### P3 — Compilar o upstream puro
`git worktree add ..\KyraFiles-upstream-base upstream/main` (worktree ao lado do repositório, nunca em `%TEMP%`). Compilar em Release x64 com `mkfile r`. O upstream tem de ser **o mesmo commit-base** do nosso fork (`git merge-base HEAD upstream/main`), para a comparação ser justa; registre o hash.
Atenção: o app é empacotado (MSIX). Registre o layout pelo script do projeto; feche a instância anterior antes. **Dois builds não podem estar instalados ao mesmo tempo com a mesma identidade**: meça um, desregistre, meça o outro, e registre a ordem.
Pronto quando: o upstream abre, abre a pasta `1600` e o hash do commit está no relatório.

### P4 — Medir (os dois builds, mesmos scripts, mesma ordem)
Para cada build, 1 rodada de aquecimento + 3 rodadas medidas. Registrar **mediana**, e os 3 valores:

| Medida | Como |
|---|---|
| Abrir a pasta `1600` | tempo até estabilizar a lista (`bench.ps1`) |
| Home / End / PgUp / PgDn / Ctrl+Home / Ctrl+End | tempo do disparo até o item de destino ficar selecionado |
| **Segurar `Down` por 5 s** | CPU do processo (`TotalProcessorTime` antes/depois), e se a UI respondeu a um clique de teste logo depois (sim/não + atraso) |
| Segurar `End` por 5 s | idem |
| Trocar de aba (2 abas abertas, 10 trocas) | tempo por troca |
| Working set e **memória privada** | `Get-Process`: `WorkingSet64` **e** `PrivateMemorySize64`, nomeando qual é qual |
| CPU parado, 10 s | `idle.ps1 -WaitS 10` |

Regra: CPU acumulada de vida do processo **não é** CPU de uma ação. Sempre diferença entre duas leituras.

### P5 — Trace do nosso build segurando `Down`
Gravar um trace de 5 s com o teclado segurado (WPR/ETW com perfil de CPU, ou `dotnet-trace collect --process-id <pid> --profile cpu-sampling`). Salvar o arquivo bruto e um resumo: as 10 funções com mais tempo, por thread (UI vs outras). Só com isso se pode escrever uma causa.

### P6 — Relatório
`docs/agents/evidence/F025/relatorio.md` com:

1. tabela upstream × atual, cada célula com os 3 valores e a mediana;
2. o comando que gerou cada número;
3. o resumo do trace;
4. cada afirmação marcada `NOT TESTED` / `OBSERVED` / `INFERRED` / `CONFIRMED` (só `CONFIRMED` se há comando e saída);
5. a leitura segundo a seção 2 (sim / não / parcial), **sem recomendação de cancelar**;
6. se o resultado for "não" ou "parcial": o próximo passo proposto (bisseção), como `PENDING DECISION` em `DECISIONS.md`.

Depois: handoff em `docs/agents/handoffs/AAAA-MM-DD_HHMM_agy_f025.md` e atualização do `AGENT_CONTEXT.md`.

## 5. Critério de aceite (revisão do Claude)

- [ ] Tabela com upstream e atual, 3 rodadas cada, mediana, comando.
- [ ] Trace bruto e resumo do build atual segurando `Down` no repositório.
- [ ] Nenhum arquivo de prova apagado; só o lab de 1.600 itens removido, com `Test-Path` falso registrado, **depois** da revisão.
- [ ] Nenhuma palavra "inviável/congênito/encerrar" sem número e trace que a sustentem.
- [ ] Zero alteração em `src/`.
- [ ] `git status` limpo; commits fora da `main`.

## 6. Se a AGY ficar sem cota

Regra global: continua depois, em outra conta ou quando renovar. O Claude pode assumir **medição/análise**, e (autorização de 12/10/2026) o código quando a cota da AGY zerar. Mesmo assim, o Claude registra no relatório quem mediu o quê.
