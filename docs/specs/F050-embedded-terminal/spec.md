# F050 — Terminal embutido no rodapé — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
O desenvolvedor alterna o tempo todo entre o gerenciador e o terminal e precisa manter as duas janelas na mesma pasta. Um terminal no rodapé que acompanha a pasta atual elimina o vai e vem.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero abrir/fechar um terminal no rodapé com um atalho.
- **US2 (P1)** Como desenvolvedor, quero que ele comece na pasta atual e, se eu quiser, acompanhe quando eu navegar.
- **US3 (P2)** Como desenvolvedor, quero escolher o interpretador (PowerShell, cmd, WSL, Git Bash).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta `C:\projeto` aberta | aperto o atalho do terminal | o rodapé abre com o prompt em `C:\projeto` |
| AC-2 | "acompanhar pasta" ligado | navego para outra pasta | o terminal muda de pasta (sem interromper um comando em execução) |
| AC-3 | comando em execução | navego | o terminal não é interrompido e avisa que não mudou de pasta |
| AC-4 | terminal aberto | redimensiono o rodapé | a saída reflui e o cursor não se perde |
| AC-5 | fecho o terminal com comando em execução | fecho | o app pergunta antes de encerrar o processo |
| AC-6 | pasta de rede ou caminho acima de 260 caracteres | abro o terminal | abre numa pasta válida e explica a mudança, sem erro |
| AC-7 | selecionei um arquivo | uso "colar caminho" | o caminho entre aspas aparece na linha de comando, sem executar |

## 4. Requisitos
- **FR-001** Interpretadores configuráveis.
- **FR-002** Teclado: quando o terminal tem o foco, os atalhos do gerenciador não roubam teclas.
- **FR-003** Nenhum processo órfão ao fechar o app (medido).
- **NFR-001** Abrir em até 1 s; memória do terminal medida e informada.

## 5. Casos de borda
- Vários terminais (um por aba); acentos; cores e rolagem; encerramento forçado do app.

## 6. Fora de escopo
- Substituir o Windows Terminal; abas e divisões dentro do rodapé.

## 7. Critérios de sucesso
- Rodar `git status` na pasta que estou vendo, sem trocar de janela.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: vale o custo de memória e de manutenção de um terminal próprio, ou basta o atalho que abre o Windows Terminal já na pasta (que já existe)? Recomendação: decidir após medir o custo do componente; sem medição, adiar.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Candidata a adiar: alto custo e já existe "abrir no terminal".
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
