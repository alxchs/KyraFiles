# F096 — Testes não alteram arquivos do repositório — SPEC

ID estável: SPEC-0080 (alias F096).
Status: **APROVADA** (Alexandre, 27/11/2026: "Assuma vc as tarefas abaixo", sobre a tarefa que a Claude escreveu para a AGY).

## 1. Por quê
- Três testes de medição gravam planilhas de evidência dentro do repositório a cada execução. Toda rodada de testes deixa três arquivos modificados, que sujam o estado do projeto e já geraram uma pendência e um stash.
- As planilhas saem com vírgula decimal quando a máquina está em português, o que mistura casas decimais com separador de coluna e estraga os dados.

## 2. Histórias de usuário
- **US1 (P1)** Como quem roda os testes, quero que uma execução comum não altere nada do que está no controle de versão.
- **US2 (P2)** Como quem precisa registrar evidência, quero gerar as planilhas no lugar de sempre ligando uma opção explícita.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | a opção de evidência desligada | rodo todos os testes | nenhum arquivo do repositório fica modificado |
| AC-2 | a opção de evidência ligada | rodo os testes de medição | as três planilhas são geradas na pasta de evidência como antes |
| AC-3 | máquina em português | as planilhas são geradas | os números usam ponto decimal e cada linha tem o mesmo número de colunas do cabeçalho |
| AC-4 | qualquer opção | rodo todos os testes | todos passam |

## 4. Requisitos
- **FR-1** Sem a opção ligada, a planilha vai para a pasta temporária do próprio teste, apagada ao fim.
- **FR-2** A opção é uma variável de ambiente com valor exato `1`.
- **FR-3** Números nas planilhas independem do idioma da máquina.
- **NFR-1** Nenhum teste novo abre janela nem rouba o foco.

## 5. Fora de escopo
- Mudar o que os testes medem ou seus limites de aprovação.
- Regenerar as planilhas já commitadas.
