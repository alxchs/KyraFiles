# F032 — Fixar aba

Status: **APROVADA** (Claude, 21/10/2026, pedido do Alexandre: "fixar aba").

## Histórias
1. Como usuário, quero fixar as abas das pastas que uso sempre, para elas não serem fechadas sem querer e continuarem lá quando eu reabrir o app.

## Requisitos
- **FR-1:** o menu da aba (clique direito) tem "Fixar aba" numa aba comum e "Desafixar aba" numa aba fixada.
- **FR-2:** abas fixadas ficam sempre à esquerda das comuns, na ordem em que foram fixadas. Fixar uma aba a leva para logo depois da última fixada; desafixar a leva para logo depois da última fixada que restar.
- **FR-3:** a aba fixada mostra um ícone de alfinete no lugar do botão de fechar e continua mostrando o nome da pasta.
- **FR-4:** a aba fixada não fecha por: botão de fechar, clique do meio, Ctrl+W, "Fechar outras abas", "Fechar abas à esquerda/direita", "Fechar todas as abas". Para fechar, primeiro desafixa.
- **FR-5:** dentro da aba fixada a navegação é livre (entrar em pastas, voltar, avançar), como em qualquer aba.
- **FR-6:** o estado de fixada é salvo com a sessão e restaurado ao reabrir o app, junto com a ordem.
- **FR-7:** arrastar para reordenar não coloca aba comum entre as fixadas nem fixada entre as comuns; ao soltar fora da faixa certa, a aba volta para a faixa dela.
- **FR-8:** "Mover aba para nova janela" numa aba fixada leva a aba desafixada.

## Critérios de aceite
- **AC-1:** fixar a 3ª de 4 abas: ela vai para a 1ª posição, com alfinete e sem botão de fechar.
- **AC-2:** com 1 fixada e 3 comuns, "Fechar outras abas" a partir de uma comum deixa a fixada e a comum clicada.
- **AC-3:** Ctrl+W na fixada não fecha nada.
- **AC-4:** fechar e reabrir o app restaura a aba fixada em 1º lugar e fixada.
- **AC-5:** desafixar devolve o botão de fechar e permite fechar.

## Casos de borda
Todas as abas fixadas (nova aba entra depois delas); só uma aba e ela fixada (Ctrl+W não fecha; fechar a janela continua fechando o app normalmente).

## Fora de escopo
Aba fixada compacta (só ícone); fixar em várias janelas ao mesmo tempo.

## Decisão
A aba fixada mantém o nome da pasta (só troca o botão de fechar pelo alfinete). Num gerenciador de arquivos o nome da pasta é a informação principal; navegadores encolhem para o ícone porque o ícone já identifica o site. Se o Alexandre preferir compacta, muda a FR-3.

## Histórico
- 21/10/2026: criada.
