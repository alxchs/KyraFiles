# F042 — Buscas salvas na barra lateral — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Buscas que se repetem ("PDFs desta semana", "Downloads acima de 100 MB", "o que mexi hoje") hoje são refeitas à mão. Com a busca rápida (F023) elas podem ser instantâneas e viver como pastas virtuais.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero salvar a busca atual com um nome e achá-la na barra lateral.
- **US2 (P1)** Como usuário, quero que ela se refaça sozinha ao abrir, com datas relativas ("últimos 7 dias") sempre atuais.
- **US3 (P2)** Como usuário, quero editar, renomear e apagar buscas salvas.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | busca `*.pdf` com filtro "modificado nos últimos 7 dias" | "Salvar busca" | aparece na barra lateral com o nome dado |
| AC-2 | busca salva | clico nela amanhã | resultados refletem "7 dias" a partir de amanhã |
| AC-3 | busca com escopo de uma pasta que não existe mais | abro | aviso claro, sem erro; posso editar o escopo |
| AC-4 | busca salva | "Editar" | filtros, escopo e nome podem ser alterados e salvos |
| AC-5 | busca salva | "Remover" | some da barra lateral sem apagar arquivos |
| AC-6 | busca salva aberta | seleciono resultados | cópia, abrir local e demais ações funcionam como em uma pasta normal |
| AC-7 | fecho e reabro o app | olho a barra lateral | as buscas continuam lá, na mesma ordem |

## 4. Requisitos
- **FR-001** Guardar texto, filtros (tipo, tamanho, data relativa ou absoluta) e escopo.
- **FR-002** Usar o provedor de busca ativo (Everything quando disponível).
- **FR-003** Reordenar por arrastar.
- **NFR-001** Abrir uma busca salva sobre índice pronto mostra os primeiros resultados em até 1 s (medido).

## 5. Casos de borda
- Provedor de busca indisponível; milhares de resultados (paginar); nome repetido.

## 6. Fora de escopo
- Alertas quando surgir resultado novo; compartilhar buscas.

## 7. Critérios de sucesso
- "O que mexi hoje" abre com um clique e acerta sem digitar nada.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: incluir buscas salvas por conteúdo (mais lentas) ou só por nome/metadado? Recomendação: só nome e metadado.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Depende de F023.
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
