# F036 — Fila de cópias com pausar e retomar — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Várias cópias/movimentos ao mesmo tempo disputam o mesmo disco e ficam todos lentos; não dá para pausar uma cópia grande para liberar o disco, e uma falha no meio de milhares de arquivos obriga a refazer tudo.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero pausar e retomar uma cópia ou movimento em andamento, para liberar o disco ou a rede sem perder o progresso.
- **US2 (P1)** Como usuário, quero que operações para o mesmo disco entrem em fila (uma por vez), para cada uma terminar mais rápido.
- **US3 (P2)** Como usuário, quero ver velocidade e tempo restante reais e, ao fim com erros, "tentar de novo só os que falharam".

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | cópia de muitos arquivos em andamento | clico "Pausar" | **Nível 1 (garantido):** a transferência para ao terminar o arquivo corrente (em até 2 s se os arquivos forem pequenos) e o progresso fica salvo; "Retomar" continua do próximo arquivo, sem recopiar os já copiados. **Nível 2 (fase C, aprovada pelo Alexandre em 25/10/2026):** num único arquivo grande, a pausa vale no meio do arquivo e a retomada continua do ponto em que parou |
| AC-2 | 3 cópias grandes para o mesmo destino, limite padrão 2 | inicio a terceira | as duas primeiras progridem juntas e a terceira aparece "Na fila" e começa quando uma terminar; mudando o limite para 1, a segunda também espera; **apagar ou renomear arquivos enquanto as cópias rodam não espera na fila** |
| AC-3 | cópias para discos diferentes | inicio ambas | rodam ao mesmo tempo |
| AC-4 | cópia com 3 arquivos que falham (bloqueados) | termina | resumo lista os 3 e "Tentar de novo" copia só eles |
| AC-5 | cópia na fila | clico "Cancelar" | sai da fila sem copiar nada e sem tocar nos arquivos de origem |
| AC-6 | fecho o app com cópia em andamento | tento fechar | o app avisa e oferece pausar ou cancelar; nada fica pela metade sem aviso |

## 4. Requisitos
- **FR-001** Pausar, retomar, cancelar e reordenar a fila por operação.
- **FR-002** Limite de **cópias e movimentos simultâneos por destino** (volume local ou servidor de rede), **configurável (1 a 4, padrão 2)**; acima do limite, a operação espera na fila ("Na fila"). **Apagar e renomear não entram nessa fila**: rodam sem esperar cópia.
- **FR-003** Mostrar velocidade (média dos últimos segundos) e tempo restante.
- **FR-004** Operação de movimento DEVE só apagar a origem de itens já copiados e verificados.
- **FR-005** Para arquivo grande (a partir de um limite configurável), a pausa DEVE valer no meio do arquivo e a retomada DEVE continuar do ponto em que parou, com o conteúdo final idêntico ao da origem.
- **FR-006** Um arquivo em cópia, pausada ou cancelada NUNCA DEVE aparecer com o nome final enquanto estiver incompleto; cancelar DEVE remover o parcial e, se havia um arquivo anterior com o mesmo nome (substituir), DEVE mantê-lo intacto.
- **FR-007** Para destinos que o caminho de arquivo grande não cobre com a mesma fidelidade do motor atual (sistema de arquivos que não seja NTFS ou ReFS, execução como administrador, pasta inteira), a cópia DEVE seguir pelo motor atual, sem aviso de erro ao usuário.
- **FR-008** Um arquivo que tenha fluxos alternativos (por exemplo, a marca de "baixado da internet") NUNCA DEVE perder esses dados em silêncio: se o destino não os suporta (como um NAS compartilhado que não guarda fluxos), a cópia DEVE seguir pelo motor atual, que avisa o usuário.
- **NFR-001** Pausar não pode aumentar o uso de memória nem travar a janela (medido).

## 5. Casos de borda
- Origem ou destino desconectado durante a pausa (pendrive, rede).
- Arquivo alterado durante a pausa.
- Conflito de nome tratado antes de entrar na fila, não no meio.

## 6. Fora de escopo
- Retomar após reiniciar o computador. Limite de velocidade configurável.

## 7. Critérios de sucesso
- Copiar 2 arquivos de 2 GB em sequência termina o primeiro no mesmo tempo que sozinho (medido).

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: quando destino for rede, serializar também por servidor? Recomendação: sim.]
- [DECIDIDO 24/10/2026, vale a recomendação: retomar após fechar o app entra agora ou depois? Recomendação: depois.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
- 2026-10-25 — AC-1 reescrito em dois níveis (decisão D-F036-1, opção b, por instrução do Alexandre: "se não der certo use a (b)"). Motivo: o motor de cópia do shell não pausa no meio do arquivo e o spike da cópia por blocos não passou na régua de fidelidade (evidência em docs/agents/evidence/F036/spike-copia-por-blocos.md).
- 2026-10-25 — fase C aprovada pelo Alexandre ("Melhor aplicar então"): AC-1 nível 2 vira compromisso; FR-005 a FR-007 acrescentados (decisão D-F036-2).
- 2026-10-25 — FR-008 acrescentado após o teste no NAS (a marca de baixado da internet some em silêncio com CopyFile2 e File.Copy num destino sem fluxos).
- 2026-10-25 — FR-002 e AC-2 reescritos conforme a decisão (b) do Alexandre (D-F036-3): limite configurável por destino, padrão 2; apagar e renomear fora da fila.
