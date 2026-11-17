# F057 — Atalhos de teclado nos botões das caixas de diálogo — SPEC

Status: **APROVADA pelo Alexandre em 26/10/2026**, com **alcance global** (todas as 17 caixas de diálogo do app, não só a de exclusão). Pedido dele com a captura da caixa "Excluir item". Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 26/10/2026)

## 1. Por quê
Na caixa "Excluir item" (e nas parecidas), os botões **Excluir** e **Cancelar** e a opção **Excluir permanentemente** só se alcançam com o mouse ou com Tab e Enter. Falta o atalho de uma tecla, sublinhada no nome do botão, como em qualquer programa do Windows. Quem usa o teclado perde tempo e erra mais.

## 2. Histórias de usuário
- **US1 (P1)** Como quem usa o teclado, quero apertar a letra sublinhada de um botão da caixa de diálogo para acioná-lo sem o mouse.
- **US2 (P1)** Como quem apaga arquivos, quero marcar ou desmarcar "Excluir permanentemente" por uma letra, sem sair do teclado.
- **US3 (P2)** Como quem usa o KyraFiles em outro idioma, quero que a letra sublinhada faça parte do texto traduzido, e não fique presa ao inglês.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | caixa "Excluir item" aberta | olho os botões | cada botão mostra uma letra sublinhada (ex.: **E**xcluir, **C**ancelar) |
| AC-2 | mesma caixa | aperto a letra do botão principal | a ação é executada, igual ao clique |
| AC-3 | mesma caixa | aperto a letra de Cancelar | a caixa fecha sem apagar nada |
| AC-4 | mesma caixa, opção "Excluir permanentemente" desmarcada | aperto a letra da opção | a opção fica marcada; apertar de novo a desmarca; a caixa **não** fecha nem executa nada |
| AC-5 | qualquer caixa com mais de um controle | comparo as letras | nenhuma letra se repete na mesma caixa |
| AC-6 | idioma diferente do inglês | abro a caixa | a letra sublinhada pertence ao texto traduzido e funciona |
| AC-7 | campo de texto em foco (ex.: renomear no conflito de cópia) | digito uma letra | a letra entra no campo; **não** aciona botão |
| AC-8 | Enter e Esc | uso em qualquer caixa | continuam como hoje (Enter = botão padrão, Esc = cancelar) |
| AC-9 | botão desabilitado | aperto a letra dele | nada acontece |

## 4. Requisitos
- **FR-001** Todo botão (principal, secundário, fechar) e toda opção marcável das caixas de diálogo do app tem letra de atalho sublinhada.
- **FR-002** A letra sai do texto que cada botão mostra no idioma em uso (a primeira letra latina livre), sem mexer em tradução. Texto sem letra latina = sem atalho, nunca texto quebrado.
- **FR-003** Sem repetição de letra dentro de uma mesma caixa (verificado por teste automático sobre os textos de todos os idiomas).
- **FR-004** O atalho não deve exigir Alt para funcionar com a caixa aberta; o sublinhado aparece sempre, não só ao apertar Alt.
- **NFR-001** Nenhum custo perceptível ao abrir a caixa (medido).

## 5. Casos de borda
- Texto do botão muda em tempo de execução (ex.: "Excluir" vira "Excluir permanentemente"): a letra continua valendo e o sublinhado acompanha.
- Idiomas sem letra latina (japonês, chinês, árabe): usa a letra latina entre parênteses, como o Windows faz, ou não tem atalho.
- Letra igual ao atalho global do app: com a caixa aberta, vale a caixa.

## 6. Fora de escopo
- Menus de contexto e a barra de comandos (já têm as suas teclas).
- Reorganizar o texto das caixas.

## 7. Critérios de sucesso
- Apagar um arquivo definitivamente só com o teclado, em 2 teclas, sem Tab.

## 8. Pontos em aberto
- [x] **Letras:** automáticas pela primeira letra do texto de cada controle (em inglês D, C, P; em português E, C, P). Decidido em 26/10/2026.
- [x] **Alcance:** global, as 17 caixas (decisão do Alexandre, 26/10/2026).

## 9. Histórico
- 26/10/2026: aprovada com alcance global; FR-002 ajustado (letra automática por texto, não por marcador de tradução).
