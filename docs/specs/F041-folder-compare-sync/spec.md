# F041 — Comparar e sincronizar duas pastas — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Fazer backup para pendrive/NAS ou conferir duas versões de uma pasta exige programa extra. Com o painel duplo já existente, falta mostrar só o que difere e copiar as diferenças. Total Commander e Directory Opus têm "Sincronizar".

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero comparar as pastas dos dois painéis e ver só o que é novo, diferente ou falta de um lado.
- **US2 (P1)** Como usuário, quero copiar as diferenças numa direção (esquerda→direita, direita→esquerda ou nos dois) revendo antes.
- **US3 (P2)** Como usuário, quero um modo "só nomes" (copia o que falta, nunca troca o que existe).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | duas pastas com 2 arquivos só na esquerda, 1 só na direita, 1 mais novo na direita | comparo | os 4 itens aparecem com o motivo; os iguais ficam ocultos (com opção de mostrá-los) |
| AC-2 | resultado da comparação | desmarco 1 item e escolho "esquerda→direita" | só os marcados são copiados; nenhum arquivo da esquerda é alterado |
| AC-3 | modo "espelho" ligado | aplico | arquivos que só existem no destino são listados como "a remover" e só vão para a Lixeira com confirmação explícita |
| AC-4 | comparar por "tamanho e data" vs "conteúdo" | alterno | o critério muda o resultado conforme esperado; "conteúdo" avisa do custo |
| AC-5 | operação em andamento | cancelo | para; o que já copiou permanece, o resumo diz o que faltou |
| AC-6 | pasta de rede com relógio diferente | comparo | tolerância de data configurável evita falsos "diferentes" |

## 4. Requisitos
- **FR-001** Prévia obrigatória; nada é copiado ou apagado sem o usuário aplicar.
- **FR-002** Respeitar a fila de cópias (F036) quando existir.
- **FR-003** Comparar subpastas (opcional) e ignorar padrões (`*.tmp`, `.git`).
- **NFR-001** 100 mil arquivos por lado: primeiro resultado em até 5 s, janela responsiva (medido).

## 5. Casos de borda
- Arquivo na esquerda, pasta com o mesmo nome na direita. Links simbólicos. Nomes que só diferem em maiúsculas.

## 6. Fora de escopo
- Agendamento automático; sincronização contínua em segundo plano.

## 7. Critérios de sucesso
- Atualizar um pendrive de backup copiando só o novo, com prévia, em menos de 5 cliques.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: modo espelho (apaga no destino) entra agora? Recomendação: não; só copiar nesta versão.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
