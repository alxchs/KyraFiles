# F049 — Links simbólicos, junctions e hardlinks — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Quem desenvolve usa links para compartilhar pastas entre projetos (`node_modules`, assets, configurações), mas no Windows criá-los exige linha de comando (`mklink`). O app não cria nem mostra links.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero criar um link simbólico, uma junction ou um hardlink por menu, sem digitar comando.
- **US2 (P1)** Como desenvolvedor, quero ver que um item é um link e para onde aponta.
- **US3 (P2)** Como desenvolvedor, quero apagar um link sem apagar o conteúdo de destino.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta `A` selecionada | "Criar link para esta pasta…" e escolho o local e o nome | o link é criado e aparece com o ícone de link |
| AC-2 | pasta com link | olho a coluna "Destino do link" | mostra o caminho de destino |
| AC-3 | link para pasta | "Excluir" | só o link some; o conteúdo do destino fica intacto |
| AC-4 | link cujo destino foi apagado | listo a pasta | o link aparece como "quebrado", sem erro nem travamento |
| AC-5 | falta de permissão para criar link simbólico | tento criar | o app explica a causa e oferece a junction (para pastas) ou o hardlink (para arquivos) |
| AC-6 | link dentro da pasta | calculo o tamanho | o conteúdo do destino não é contado em dobro |
| AC-7 | link relativo | copio para outra pasta | o app pergunta se copia o link ou o conteúdo |

## 4. Requisitos
- **FR-001** Tipos: simbólico (arquivo/pasta), junction, hardlink.
- **FR-002** Distinguir visualmente cada tipo.
- **FR-003** Nunca seguir o link ao excluir ou calcular tamanho.
- **NFR-001** Mostrar o destino não pode atrasar a listagem (medido em pasta de 10 mil itens).

## 5. Casos de borda
- Ciclo de links; link entre unidades; link para caminho de rede; nome com caracteres inválidos.

## 6. Fora de escopo
- Editar o destino de um link existente (decidir abaixo).

## 7. Critérios de sucesso
- Criar um link de pasta em menos de 10 s, sem abrir terminal.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: permitir editar o destino de um link existente? Recomendação: não nesta versão.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
