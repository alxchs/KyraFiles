# F033 — Copiar para / Mover para no menu de contexto

Status: **APROVADA** (Claude, 21/10/2026, pedido do Alexandre: "ao selecionar um grupo de arquivos e abrir o menu do clique direito, disponibilizar opção de copiar para/mover para, com confirmação sempre tendo como default o não. Na lista de opções de pasta de destino listar as últimas 12 pastas selecionadas").

## Histórias
1. Como usuário, com vários arquivos selecionados, quero copiá-los ou movê-los para uma pasta que uso com frequência direto pelo clique direito, sem abrir outra aba nem usar a área de transferência.
2. Quero uma confirmação antes, e que o padrão seja não fazer nada, para um Enter sem querer não mover meus arquivos.

## Requisitos
- **FR-1:** com um ou mais itens selecionados, o menu de contexto mostra "Copiar para" e "Mover para", cada um com um submenu.
- **FR-2:** o submenu lista as últimas 12 pastas escolhidas como destino (a mais recente primeiro), cada uma como "Nome (caminho completo)", seguidas de um separador e de "Escolher pasta...", que abre o seletor de pasta do Windows.
- **FR-3:** a lista de últimas pastas é a mesma para copiar e mover, guardada entre sessões. Escolher uma pasta (da lista ou pelo seletor) a coloca no topo; uma pasta que já estava na lista não se repete; acima de 12, sai a mais antiga.
- **FR-4:** pasta da lista que não existe mais (apagada, unidade desconectada) não aparece no submenu.
- **FR-5:** a pasta atual não aparece como destino de "Mover para" (mover para o mesmo lugar não faz nada).
- **FR-6:** antes de copiar ou mover, aparece uma confirmação com a ação, a quantidade de itens e o caminho de destino, com os botões "Sim" e "Não". O botão padrão é **"Não"**: Enter e Esc respondem "Não".
- **FR-7:** confirmado, a operação usa o mesmo mecanismo do colar do app: progresso, conflitos de nome, desfazer (Ctrl+Z).
- **FR-8:** respondido "Não" ou cancelado o seletor, nada muda e a pasta não entra na lista.

## Critérios de aceite
- **AC-1:** selecionar 3 arquivos, clique direito: aparecem "Copiar para" e "Mover para"; sem nenhuma pasta usada ainda, o submenu tem só "Escolher pasta...".
- **AC-2:** "Copiar para" → "Escolher pasta..." → pasta X → confirmação com Enter: nada é copiado. De novo, clicando "Sim": os 3 arquivos aparecem em X e X passa a ser o 1º item do submenu.
- **AC-3:** depois de usar 13 pastas diferentes, o submenu mostra as 12 mais recentes.
- **AC-4:** "Mover para" → X → "Sim": os arquivos saem da pasta atual e vão para X; Ctrl+Z os traz de volta.
- **AC-5:** apagar a pasta X: ela some do submenu.
- **AC-6:** fechar e reabrir o app: a lista continua lá.

## Fora de escopo
Arrastar para os itens do submenu; copiar para várias pastas de uma vez; lista separada por ação.

## Histórico
- 21/10/2026: criada.
- 21/10/2026 (plano): o item de menu do app não tem dica (tooltip); o caminho completo vai no próprio texto, "Nome (caminho)" (FR-2).
