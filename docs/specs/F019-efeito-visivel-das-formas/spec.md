# F019 — Efeito visível das formas (janela, abas, área de arquivos) — SPEC

Estado: APROVADA (Claude, 12/10/2026). Decisão do Alexandre (12/10/2026): efeito **marcante em tudo**. Origem: "não achei os efeitos nos shapes e no formulário".

## Por que
Com o binário correto implantado, a forma escolhida em Configurações > Aparência mal aparece: só a borda da janela muda (~12 px) e as abas ficam iguais em todas as formas. `CornerRadius` só expressa cantos convexos; formas côncavas, de onda e serrilha só existem pelo recorte geométrico, hoje aplicado apenas à janela e à área de arquivos e com profundidade pequena.

## Cenários de aceite
- **AC-1** Dada qualquer forma de 1 a 9, Quando a seleciono em Aparência, Então a janela, cada aba e a área de arquivos mudam de contorno na hora, sem reiniciar.
- **AC-2** O contorno é claramente visível numa captura da janela inteira (profundidade ~28 px na janela, ~20 px na área de arquivos, ~16 px nas abas) e diferente entre as formas 1, 2, 3, 4, 5, 6, 7, 8 e 9.
- **AC-3** A forma 0 (Fluent) não recorta nada (aparência original das abas e da janela).
- **AC-4** Nenhum texto ou ícone importante fica cortado nas formas 1 a 9 (nomes de arquivo, botões da barra, itens da barra lateral, botões de janela, fechar da aba).
- **AC-5** Trocar a forma com várias abas abertas atualiza todas; abrir/fechar abas mantém a forma.
- **AC-6** Redimensionar a janela mantém o recorte correto (sem faixas).

## Requisitos
FR-001 recorte por forma em: contêiner da janela, borda da área de arquivos, contêiner de cada aba. FR-002 a profundidade escala por elemento. FR-003 reage à troca de forma. NFR-001 sem custo perceptível ao rolar listas grandes.

## Fora de escopo
Botões, caixas de texto e diálogos (seguem só com `CornerRadius`).

## Revisão 12/10/2026 — feedback do Alexandre ("não gostei… as cores nas extremidades com um acabando horrível")
Capturas do app com a forma "Sawtooth Triangle" (Aparência) mostraram: dentes de ~400 px de base na lateral da janela, ondas gigantes cortando a barra lateral e a barra de rolagem, e o fundo translúcido (acrílico) de outra cor aparecendo nas pontas. Causa: os construtores usam proporções da altura do elemento (ex.: 4 dentes em `h`), então o desenho cresce com a janela.

- **AC-7** O tamanho de cada detalhe da forma (dente, onda, chanfro, concavidade) é **fixo em pixels** e não depende do tamanho do elemento. Um dente de serrilha tem ~16 px de base e ~10 px de altura, em qualquer elemento.
- **AC-8** Em elementos grandes (janela, área de arquivos) a forma só altera os **4 cantos** (profundidade ~28 px e ~20 px); as **bordas longas ficam retas**. O perfil completo da forma (lateral inteira ondulada ou serrilhada) vale só para elementos pequenos (abas, altura < 80 px).
- **AC-9** Nenhuma faixa ou ponta de cor destoante nas extremidades: fora do contorno aparece apenas o fundo da janela, e nenhum painel (barra lateral, rolagem, cabeçalho) é cortado por dentes ou ondas em tamanho maior que o raio do canto.
- **AC-10** Conferido com o fundo "Blue" e backdrop "Thin Acrylic" (configuração do Alexandre) nas formas 1 a 9, com capturas dos quatro cantos da janela.

## Revisão 12/10/2026 (2) — não perder a referência visual do KyraFiles
Pedido do Alexandre: o efeito não pode fazer o aplicativo perder a referência visual ao KyraFiles.
- **AC-11** Com qualquer forma de 1 a 9, o aplicativo continua reconhecível como o KyraFiles: mesma estrutura (abas, barra de menu, barra de endereço, barra lateral, área de arquivos), mesmas cores do tema, ícones, tipografia e espaçamentos. **Só o contorno** muda. Nada é movido, escondido ou recolorido por causa da forma.
- **AC-12** A forma 0 (Fluent) é pixel a pixel o visual atual do KyraFiles (sem recorte e com os `CornerRadius` originais), servindo de referência de comparação.
- **AC-13** As formas 1 a 9 usam o mesmo conjunto de cantos e da mesma família de raios do KyraFiles como base (o KyraFiles original já arredonda 4/8 px): o recorte **acrescenta** o motivo da forma nos cantos e nas abas, sem remover os cantos arredondados do KyraFiles nos demais elementos.
- Verificação: tabela de captura lado a lado (forma 0 x forma N) da janela inteira; o Alexandre confirma que a estrutura é a mesma e só os contornos diferem.
