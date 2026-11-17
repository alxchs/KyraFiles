# F058 — Janela do instalador e da atualização sempre à frente — SPEC

Status: **APROVADA pelo Alexandre em 12/11/2026** ("Specs aprovadas"). Estado anterior: **PROPOSTA, aguardando aprovação do Alexandre** (pedido em 26/10/2026: "precisa servir para o instalador de qualquer projeto; a janela de atualização precisa sempre vir para a frente"). Instância no KyraFiles do modelo `~/.dev-method/templates/spec-instalador-janela-frente.md` (METODO §15); mudou o modelo, mude aqui. Sem tecnologia, arquivo ou classe.

- [ ] **APROVADA pelo Alexandre** (data: ____)

## 1. Por quê
Janela de instalação ou atualização que abre **atrás** de outras passa despercebida: a pessoa acha que nada aconteceu, clica de novo, ou a atualização fica parada esperando uma resposta que ninguém viu.

## 2. Histórias de usuário
- **US1 (P1)** Ao instalar ou atualizar o KyraFiles, quero ver a janela do instalador na frente de todas as outras, qualquer que seja a forma de abrir.
- **US2 (P1)** Quero que uma tecla que eu estava digitando em outro programa não confirme nem cancele nada no instalador por acidente.
- **US3 (P2)** Se o Windows não permitir trazer a janela, quero um aviso visível (a barra de tarefas piscando), nunca silêncio.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | outros programas abertos, um deles com foco | abro o instalador com duplo clique | a janela do instalador aparece na frente e com foco |
| AC-2 | o KyraFiles aberto | clico em "Atualizar agora" no próprio programa | o programa fecha e a janela do instalador aparece na frente, não atrás do navegador ou do chat que eu estava usando |
| AC-3 | atualização iniciada em segundo plano (automática) | o instalador precisa de uma resposta ou mostra progresso | a janela vem para a frente uma vez; depois não rouba o foco de novo a cada etapa |
| AC-4 | um programa em tela cheia (jogo, vídeo, apresentação) | o instalador abre | a janela aparece na frente, ou a barra de tarefas pisca se o sistema impedir; nunca fica invisível |
| AC-5 | eu estava digitando em outro programa quando a janela surgiu | continuo digitando por 1 segundo | nenhuma tecla confirma, cancela ou escolhe opção no instalador nesse intervalo |
| AC-6 | o instalador pede para fechar o programa em uso, ou avisa erro ou reinício | a pergunta aparece | ela também vem para a frente |
| AC-7 | instalação termina | a página final ou o programa recém-instalado abre | ficam na frente |
| AC-8 | duas instâncias do instalador | abro a segunda | a primeira vem para a frente; a segunda não abre outra janela |

## 4. Requisitos
- **FR-001** Toda janela do instalador/atualizador (assistente, perguntas, erros, página final) é ativada na frente ao aparecer.
- **FR-002** Vale para qualquer forma de início: duplo clique, iniciado pelo próprio programa, linha de comando, tarefa agendada.
- **FR-003** Trava anti-digitação: por 1 segundo depois de a janela surgir, teclas não acionam botões.
- **FR-004** Se o sistema recusar trazer a janela, a barra de tarefas pisca até a pessoa atender.
- **FR-005** Sem exigir administrador só por causa disso.
- **NFR-001** Não rouba o foco repetidamente durante a instalação.

## 5. Casos de borda
- Modo silencioso/sem janela (instalação por script): nada aparece e nada rouba foco.
- Várias telas: a janela aparece na tela onde a pessoa está trabalhando.
- Sessão bloqueada: aparece quando a sessão voltar.

## 6. Fora de escopo
- Conteúdo e texto das páginas do instalador.

## 7. Critérios de sucesso
- Em 10 de 10 tentativas, com outro programa em foco, a janela aparece na frente.

## 8. Pontos em aberto
- [NEEDS CLARIFICATION] A trava anti-digitação vale também quando a pessoa abriu o instalador de propósito? Recomendação: sim, 1 segundo é imperceptível.

