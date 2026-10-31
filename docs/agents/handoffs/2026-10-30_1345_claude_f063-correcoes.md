# Handoff Claude — F063 correções (C1..C5) — 30/10/2026

Branch: `feature/f063-correcoes` (worktree `FilesApp-claude`), saída da `colab` já com a F065 corrigida e integrada por fast-forward. Execução autorizada pelo Alexandre: "Assuma o que ela não começou ainda que consiga acabar até as 14:00".

## O que mudou
- **C3 (Q13)** spec da F063 de volta a RASCUNHO; AC-3 reescrito (nada é liberado com a janela visível), AC-4 com prazo, AC-2 com ±10 ms até o primeiro quadro em 10 rodadas, promessa limitada à RAM, rótulo do Sobre. `tasks-correcoes.md` com C1..C8.
- **C1 (Q1)** `MainWindow_Activated` não agenda mais liberação ao perder o foco; voltar à janela só cancela a liberação agendada por minimizar ou ocultar. INFERRED (leitura do código): antes, se a perda de foco chegasse depois da minimização, o agendamento de 60 s substituía o de 2 s, então a liberação "ao minimizar" podia acontecer só 60 s depois.
- **C2 (Q2)** `FileOperationsHelpers.WaitForCompletion(TimeSpan, CancellationToken)`: espera no máximo 30 s ou até a janela ser restaurada; o `MemoryTrimHelper` pula a liberação se a operação continuar. A espera sem prazo no fechamento do app (`App.xaml.cs`, código do upstream e do modo bandeja) não mudou.
- **C4 (Q9)** nota interna da 4.3.9 (`docs/releases/2026-10-29_build-FilesCustomSetup-4.3.9.md` e `.html`): estados rebaixados (OBSERVED pela AGY ou NOT TESTED), build sem saída marcado NOT TESTED, texto do usuário igual ao publicado no GitHub.
- **C5 (Q11)** `docs/agents/DESCUMPRIMENTOS.md` com D-1 a D-6 e a exigência de explicação no próximo handoff da AGY; `REGRA-NAO-PULAR.md` aponta para ele.

## Provas
- Build do app com C1 e C2: `dotnet build src/Files.App/Files.App.csproj -c Release -p:Platform=x64` → `Build succeeded`, `0 Warning(s)`, `0 Error(s)`, 4m01s (13:32).
- Gatilhos restantes (`grep -rn "ScheduleTrim(\|ExecuteTrim(" src`): `MainWindow.xaml.cs:478` (minimizar) e `:488` (ocultar) agendam; `App.xaml.cs:489` e `:499` liberam no modo bandeja; nenhum por perda de foco.
- Testes automatizados: nenhum cobre estas classes (dependem do app inteiro); a suíte não foi rodada de novo porque nenhum arquivo ligado a ela mudou.
- App não aberto: provas de tela e medição só às 09:00 de 31/10 (METODO §16).

## Estado por cenário (spec em RASCUNHO)

| ID | Estado | Base |
|---|---|---|
| F063-AC-1 (RAM liberada ao minimizar) | OBSERVED pela AGY em 29/10 (3 rodadas), antes de C1/C2 | medir de novo às 09:00 de 31/10 (C6/C7) |
| F063-AC-2 (restauração ±10 ms) | NOT TESTED | C6/C7 às 09:00 de 31/10 |
| F063-AC-3 (visível nunca liberada) | INFERRED | leitura do código: único `ScheduleTrim` restante está em minimizar/ocultar; sem prova em execução |
| F063-AC-4 (espera com prazo) | INFERRED | leitura do código; sem teste automatizado (a classe depende do app inteiro) |
| F063-AC-5 (rótulo do Sobre) | NOT TESTED | C8 (AGY) |

## Fica com a AGY
C6 (script de medição com UI Automation e instância própria), C7 (10 rodadas às 09:00 de 31/10), C8 (Sobre com memória privada). Também A1 e A3 da revisão de 30/10, e F065-T11/T12.
