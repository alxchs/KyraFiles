# Handoff Claude → AGY — itens assumidos pelo Claude até 14:00 (30/10/2026)

Autorização do Alexandre (30/10/2026, 13:1x): "Assuma o que ela não começou ainda que consiga acabar até as 14:00 e a avise pra pular o que vc assumir".

## AGY: PULE estes itens (OWNER = Claude)

| Item | O quê | Onde fica a entrega |
|---|---|---|
| F065-T6 | Opacidade do item nasce 1 (causa comum do Nome invisível; cobre o Agent Ransack) | `feature/f065-correcoes` |
| F065-T7 | Leitura da linha do `es.exe` em classe pura + testes que chamam o código real | `feature/f065-correcoes` |
| F065-T8 | Remover o BOM de `ShellViewModel.cs` e `DetailsLayoutPage.xaml.cs` | `feature/f065-correcoes` |
| F065-T9 | Apagar `SearchItemOpacityAndPathTests.cs` (só recalculava a regra) | `feature/f065-correcoes` |
| F065-T10 | Estados honestos por `AC-n` (handoff do Claude) | `docs/agents/handoffs/` |
| Q1 | Remover o gatilho de 60 s ao perder o foco | `feature/f063-correcoes` |
| Q2 | Espera limitada e cancelável antes do trim | `feature/f063-correcoes` |
| Q13 | Spec F063 de volta a RASCUNHO, métricas em memória privada (±10 ms) | `feature/f063-correcoes` |
| Q9 | Nota interna da 4.3.9 com estados rebaixados e texto do usuário corrigido | `feature/f063-correcoes` |
| Q11 | Registrar os descumprimentos (Stop-Process, `git checkout --`, ShowWindow) e exigir a explicação | `docs/agents/` |

Se algum destes não ficar pronto até 14:00, o handoff final do Claude dirá qual volta para a AGY. Até lá, não edite os arquivos deles.

## Continua com a AGY (não começar antes de ler este arquivo)

- F065-T2: provar qual das duas correções da coluna Caminho resolve (evento no início da busca ou leitura do estado da página) e manter só uma. Precisa de tela: às 09:00 de 31/10.
- F065: provas de tela por motor (Everything, índice, nativo, Agent Ransack) e desempenho medido no app, às 09:00 de 31/10.
- Q12: refazer o script de medição com UI Automation e instância própria.
- Q4/Q8: medição de 10 rodadas e provas de tela das 09:00 de 31/10 (F063, F060).
- A1 (configurações perdidas ao fechar), A2 (Sobre com memória privada), A3 (instalador: Watchdog primeiro, religar Win+E, não esconder falha).
- F064: só depois da F063 voltar limpa.

Regras de sempre: `docs/agents/REGRA-NAO-PULAR.md`, sem push, sem tocar na `main`.
