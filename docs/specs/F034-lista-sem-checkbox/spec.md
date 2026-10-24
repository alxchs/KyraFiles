# F034 — Lista de arquivos e pastas sem caixa de seleção

Status: **APROVADA** (Claude, 23/10/2026, pedido do Alexandre: "na lista de arquivos e pastas, remova qualquer vestígio de tentativa de pintar um checkbox. Se isso for muito importante, coloque um checkbox em algum lugar da configuração, porque isso só gasta recurso para uma coisa inútil").

## Contexto
Hoje toda linha da lista (Detalhes, Lista, Ícones, Blocos, Cartões) carrega uma caixa de seleção invisível. Ela aparece no lugar do ícone quando o mouse passa por cima, **mesmo com a opção "Mostrar caixas de seleção ao selecionar itens" desligada**, e recebe o clique nessa área. O cabeçalho de Detalhes tem ainda uma caixa "selecionar tudo". Cada linha criada e cada mudança de seleção fazem trabalho para manter essas caixas, que o Alexandre não usa.

A caixa tem uso real para quem seleciona vários itens só com o mouse ou com toque, sem Ctrl. Por isso não sai do produto: vira opção, desligada por padrão, na opção que já existe em Configurações › Pastas.

## Requisitos
- **FR-1:** com a opção desligada, nenhuma linha da lista tem caixa de seleção, em nenhum modo de exibição: ela não aparece nem com o mouse em cima do ícone, e clicar no ícone age como clicar na linha.
- **FR-2:** com a opção desligada, o cabeçalho de Detalhes não tem a caixa "selecionar tudo".
- **FR-3:** com a opção desligada, criar linhas, rolar e mudar a seleção não fazem nenhum trabalho ligado à caixa de seleção.
- **FR-4:** a opção "Mostrar caixas de seleção ao selecionar itens" (Configurações › Pastas) vem **desligada** por padrão. Quem a ligou de propósito continua com ela ligada.
- **FR-5:** com a opção ligada, as caixas voltam como antes: aparecem ao passar o mouse no ícone, marcam e desmarcam a seleção, e o cabeçalho tem "selecionar tudo".
- **FR-6:** ligar ou desligar a opção vale para a lista aberta sem reiniciar o app.

## Critérios de aceite
- **AC-1:** opção desligada, Detalhes: passar o mouse sobre o ícone de várias linhas não mostra caixa (o ícone continua lá); não há caixa no cabeçalho.
- **AC-2:** opção desligada, modos Ícones e Cartões: mesmo resultado do AC-1.
- **AC-3:** opção desligada: clicar no ícone de uma linha seleciona só aquela linha (como clicar no nome); Ctrl+clique e Shift+clique continuam funcionando.
- **AC-4:** ligar a opção: sem reiniciar, as caixas aparecem no hover e no cabeçalho; marcar a caixa de 2 linhas seleciona as 2.
- **AC-5:** desligar de novo: sem reiniciar, as caixas somem.
- **AC-6:** instalação que nunca mexeu na opção abre com ela desligada.

## Fora de escopo
Seleção por toque com pressionar e segurar (continua a do Windows). Outras caixas de seleção que não são da lista (diálogos, configurações).

## Histórico
- 23/10/2026: criada.
