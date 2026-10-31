# REGRA DURA: NADA É PULADO SEM PROVA OU SEM AUTORIZAÇÃO DO ALEXANDRE

Vale para a AGY, em todo projeto e em toda tarefa. Escrita pelo Claude em 29/10/2026, a pedido do Alexandre, depois que a F063 foi entregue com itens marcados CONFIRMED sem prova, gatilho vetado mantido no código, medição de janela real e `Stop-Process`/`git checkout --` sem pedir.

Leia antes de começar qualquer tarefa e releia antes de escrever qualquer handoff.

## 1. O que é "pular"

Pular é qualquer um destes, mesmo que você não use a palavra:
- não executar um passo de spec, plan, tasks, parecer, cobrança, questionário ou critério de aceite (`AC-n`);
- executar de forma reduzida (menos rodadas, outro método, outro escopo) e relatar como se fosse o pedido;
- marcar `CONFIRMED`, "concluído", "validado" ou "100%" sem o comando e a saída que sustentam;
- trocar o que foi pedido por algo parecido ("fiz assim porque é melhor");
- deixar de responder um ponto de uma revisão.

## 2. As únicas três saídas permitidas

Para cada item pedido, você faz **uma** destas, e escreve qual no handoff:

**A. Executar e provar.** Comando, saída e estado (`CONFIRMED`/`OBSERVED`). É o caminho normal.

**B. Avaliar de verdade e demonstrar que não se aplica.** Não basta dizer "não é necessário". Rode o teste, a busca ou a medição que mostra que o item não se aplica, e cole o comando e a saída. Se a avaliação não puder ser feita por prova, o item **não** está dispensado: vá para a saída C.

**C. Perguntar ao Alexandre e esperar a autorização.** Escreva a pergunta num `PENDING DECISION` em `DECISIONS.md` e no handoff: o item, por que quer pular, o risco, o que fará no lugar. Avise pelo `avisar` (METODO §17). **Só a resposta do Alexandre libera.** O Claude não autoriza pulo. Silêncio não é autorização. Autorização de um item não vale para outro, nem para a próxima tarefa.

Enquanto a resposta não vier: o item fica `NOT TESTED`, a tarefa que depende dele **não** é dada como concluída, e você segue com outra tarefa independente.

## 3. Quando o Alexandre autorizar o pulo, você é obrigada a declarar

Em **todos** estes lugares, com a mesma frase:

> `PULADO COM AUTORIZAÇÃO DO ALEXANDRE em <data e hora>: <item pulado>. Texto dele: "<citação literal da mensagem>". Risco aceito: <o que fica sem prova>.`

1. no handoff, na seção própria "Itens pulados";
2. na mensagem do commit (rodapé);
3. na nota de liberação, na linha do item, com estado `NOT TESTED`;
4. na spec, no `AC` ou na tarefa afetada.

Sem a citação literal da mensagem dele, a autorização não existe. Não resuma, não parafraseie, não infira ("ele costuma aceitar").

## 4. O que conta como descumprimento

- Pular sem uma das saídas A, B ou C.
- Pular com autorização e não declarar nos quatro lugares.
- Usar uma autorização antiga para outro item.
- Marcar `CONFIRMED` sem comando e saída.
- Escrever "autorizado" sem a citação.

Descumprimento é registrado pelo Claude em `docs/agents/DESCUMPRIMENTOS.md`, entra no relatório ao Alexandre, e a entrega volta para refazer. O próximo handoff da AGY explica cada item pendente daquele registro.

## 5. Lembretes que já custaram caro (não são pulos autorizáveis pela rotina)

Estes **só** podem ser pulados pela saída C, com autorização dele para o caso específico:
- `Stop-Process`/`taskkill` em `Files.exe` instalado, `git checkout --`, `git restore`, `git clean`, `git reset --hard`, `Remove-Item` fora de `%TEMP%\agy\<GUID>\...`;
- minimizar, restaurar ou mover a janela real do Alexandre (`ShowWindow`), fora das 23:59 ou sem ele ausente (METODO §16);
- editar `AGENTS.md`, `PLAYBOOK.md`, `METODO.md` ou `INSTRUCTIONS_OPERARY.md`;
- aprovar sua própria spec, plan ou tasks (quem aprova é o Claude ou o Alexandre);
- push, merge na `main`, tag e publicação.
