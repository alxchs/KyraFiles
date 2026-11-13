# F090 - Escala da interface (zoom visual da tela inteira)

Status: **APROVADA pelo Alexandre em 12/11/2026** ("Specs aprovadas"). Estado anterior: RASCUNHO para aprovação do Alexandre (Claude, 04/11/2026). Origem: "senti falta de configuração para mudar a escala de visualização da tela" (Alexandre) e pedido da AGY. O ID F089 ficou com a correção das fontes.

## Problema
Hoje só existe "Densidade da interface" (Normal, Compacto, Ultra compacto), que muda alturas de linha e itens. Não aumenta nem diminui ícones, botões, abas, barra de endereço nem textos de uma vez.

## História de usuário (P1)
Como usuário, quero escolher um percentual de escala em Configurações > Aparência e ver a interface inteira crescer ou diminuir, para usar o app confortavelmente em qualquer monitor.

## Cenários de aceite
- **AC-1** Dado 100%, quando escolho 125%, então textos, ícones, botões, abas, barra de endereço, barra lateral e listas ficam 25% maiores, sem cortar nada e sem barra de rolagem horizontal nova na janela.
- **AC-2** Dado 80%, então tudo fica 20% menor, nítido, e a lista mostra mais itens.
- **AC-3** Dado que fechei e reabri o app, então a escala continua a mesma desde a primeira tela.
- **AC-4** Dado uma escala diferente de 100%, quando abro um menu de contexto, uma dica, um diálogo ou a janela de Configurações, então eles aparecem legíveis e na mesma escala escolhida (ou o limite está documentado, ver pendência).
- **AC-5** Dado o cursor, quando clico e arrasto itens, abro menus e uso a barra de rolagem, então cliques e arraste acertam o alvo certo em qualquer escala.
- **AC-6** Dado "Padrão (100%)", então o app volta ao tamanho original.
- **AC-7** Dado a janela maximizada ou redimensionada, então a interface continua preenchendo a janela inteira, sem faixas vazias nem conteúdo cortado.
- **AC-8** Dado que o Windows já usa 150% de escala, então a escolha do app se soma a ela de forma previsível e o texto não fica borrado.

## Requisitos
- **FR-1** Níveis fixos: 80%, 90%, 100%, 110%, 125%, 150%. (A AGY sugeriu também controle contínuo: ver pendência.)
- **FR-2** A escolha vale para todas as janelas do app e persiste.
- **FR-3** A troca é imediata, sem reiniciar.
- **FR-4** A escala não substitui a densidade; as duas coexistem.
- **NFR-1** Sem piora de desempenho de abertura e navegação (§18.5).
- **NFR-2** Texto nítido (sem rasterização borrada).
- **NFR-3** Acessível por teclado, com nome para leitores de tela.

## Casos de borda
Janela pequena com 150%; várias janelas; alto contraste; monitor com DPI diferente; arrastar abas entre janelas; ao mudar a escala com uma pasta aberta, a seleção e a rolagem são mantidas.

## Fora de escopo
Mudar a escala do Windows; fonte (F089); redesenho do layout.

## Critérios de sucesso
Capturas em 80%, 100% e 150% com o app aberto, e teste de clique e arraste em 125%.

## [NEEDS CLARIFICATION]
1. Níveis fixos ou também controle contínuo (deslizante)? Recomendo fixos.
2. Menus, dicas e diálogos precisam seguir a escala já na primeira versão? (veja o risco no plano)
3. Atalho Ctrl + "+" e Ctrl + "-" para a escala global do app é desejado? Hoje esses atalhos podem estar ligados ao zoom da lista de arquivos.
