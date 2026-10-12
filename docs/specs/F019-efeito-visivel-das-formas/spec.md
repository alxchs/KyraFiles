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
