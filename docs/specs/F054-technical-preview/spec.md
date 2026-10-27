# F054 — Prévia técnica — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Quem desenvolve olha código, JSON, XML, YAML, CSV e logs o dia todo. O painel de prévia atual cobre texto e Markdown; falta realce de sintaxe, estrutura recolhível e acompanhar logs que crescem.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero ver código com realce de sintaxe e número de linha na prévia.
- **US2 (P1)** Como desenvolvedor, quero ver JSON/XML/YAML como árvore recolhível.
- **US3 (P2)** Como desenvolvedor, quero ver CSV como tabela.
- **US4 (P2)** Como desenvolvedor, quero acompanhar um log que cresce (como `tail -f`).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | arquivo `.cs`, `.ps1`, `.json` | seleciono com a prévia aberta | aparece com realce e números de linha |
| AC-2 | JSON com 1.000 chaves | seleciono | vejo a árvore, recolho e expando nós |
| AC-3 | JSON inválido | seleciono | mostra o texto com o ponto do erro (linha/coluna) |
| AC-4 | CSV de 100 mil linhas | seleciono | tabela rola fluida, sem carregar tudo de uma vez |
| AC-5 | log sendo gravado por outro programa | ligo "Acompanhar" | novas linhas aparecem ao final; desligar para |
| AC-6 | arquivo de 200 MB | seleciono | mostra só o começo, avisa do limite e não trava a lista |
| AC-7 | seleciono rapidamente vários arquivos | observo | a prévia pula os intermediários (como F030) e carrega só o último |

## 4. Requisitos
- **FR-001** Sem bloquear o arquivo para escrita por outros programas.
- **FR-002** Limites de tamanho e de linhas configuráveis.
- **FR-003** Temas claro e escuro.
- **NFR-001** Primeira exibição de arquivo de até 1 MB em até 300 ms (medido).

## 5. Casos de borda
- Codificações; linhas gigantes; arquivos protegidos; extensões desconhecidas (decidir por conteúdo).

## 6. Fora de escopo
- Edição na prévia; execução de código.

## 7. Critérios de sucesso
- Conferir 20 arquivos de configuração só com a prévia, sem abrir editor.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: medir antes o que a prévia atual já cobre (pode reduzir o escopo). Recomendação: tarefa 0 da spec é essa medição.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Depende de F005 e F030.
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
