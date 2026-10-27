# F038 — Espaços de trabalho com nome — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Quem trabalha em vários projetos reabre sempre as mesmas pastas em abas e painéis. Hoje só existe a sessão única do app. Concorrentes (OneCommander "janelas nomeadas", Directory Opus "listers") salvam esse conjunto com nome.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero salvar as abas e painéis abertos como "Contas do condomínio" e reabrir tudo com um clique.
- **US2 (P2)** Como usuário, quero atualizar, renomear e apagar espaços salvos.
- **US3 (P3)** Como usuário, quero abrir um espaço direto de um atalho do Windows.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | 4 abas abertas, 2 fixadas, painel duplo | "Salvar espaço de trabalho" com nome | o espaço aparece na lista com o nome |
| AC-2 | espaço salvo | "Abrir espaço" | as pastas, a ordem, as abas fixadas, o layout de cada aba e o painel duplo voltam como salvos |
| AC-3 | espaço com uma pasta que não existe mais | abro | as demais abrem; a ausente aparece marcada e não derruba a abertura |
| AC-4 | espaço aberto | altero abas e escolho "Atualizar espaço" | o espaço passa a refletir o estado atual |
| AC-5 | dois espaços com o mesmo nome | tento salvar | o app pede outro nome ou confirma a substituição |
| AC-6 | abrir um espaço | escolho "Substituir abas atuais" ou "Adicionar às abas atuais" | acontece exatamente o escolhido |

## 4. Requisitos
- **FR-001** O espaço guarda: lista de abas (caminho, fixada, layout, ordenação) e painéis.
- **FR-002** Lista acessível por menu e por busca de comandos.
- **FR-003** Persistir entre execuções e sobreviver a atualizações do app.
- **NFR-001** Abrir um espaço de 10 abas leva menos tempo que abrir as 10 pastas à mão (medido com F031).

## 5. Casos de borda
- Pasta de rede offline; unidade removível ausente; nome com caracteres inválidos; espaço vazio.

## 6. Fora de escopo
- Sincronizar espaços entre computadores; guardar posição da janela no monitor (decidir abaixo).

## 7. Critérios de sucesso
- Trocar de projeto: um clique reabre o conjunto inteiro.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: guardar também posição e tamanho da janela? Recomendação: não nesta versão.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Reaproveita fixar aba (F032) e abas rápidas (F031).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
