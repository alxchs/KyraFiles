# F037 — Renomear em lote com pré-visualização — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
O renomear em lote atual é simples. Quem organiza fotos, notas fiscais ou arquivos de projeto precisa de localizar/substituir, regex, numeração, caixa alta/baixa e data, vendo o resultado **antes** de aplicar e podendo desfazer tudo.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero ver a lista "nome atual → novo nome" enquanto defino as regras, para não errar.
- **US2 (P1)** Como usuário, quero desfazer o lote inteiro com um comando.
- **US3 (P2)** Como usuário, quero regras: localizar/substituir (com regex), numeração com zeros, maiúsculas/minúsculas, inserir data (do arquivo ou da foto), remover trecho, trocar espaços.
- **US4 (P3)** Como usuário, quero editar os nomes como texto numa só tela (um por linha) e aplicar.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | 10 arquivos selecionados | abro "Renomear em lote" e digito uma regra | vejo, ao vivo, os 10 novos nomes sem nada ter sido renomeado |
| AC-2 | regra que gera nomes duplicados | observo a lista | as linhas em conflito ficam marcadas e "Aplicar" fica bloqueado |
| AC-3 | regra aplicada | clico "Aplicar" | todos renomeiam; `Ctrl+Z` devolve todos os nomes de uma vez |
| AC-4 | arquivos `a.tar.gz` e `.gitignore` | aplico regra "trocar espaços/caixa" | extensões duplas e arquivos que começam com ponto são preservados (F001) |
| AC-5 | regex inválida | digito | a tela explica o erro e não aplica nada |
| AC-6 | um dos arquivos está em uso | aplico | os demais renomeiam, o que falhou é listado, e o desfazer cobre só o que mudou |
| AC-7 | numeração `Foto_001..` em ordem | seleciono por data | a numeração segue a ordem escolhida (nome, data ou ordem atual) |

## 4. Requisitos
- **FR-001** Pré-visualização obrigatória antes de aplicar.
- **FR-002** Detectar conflitos (entre si e com arquivos já existentes) antes de aplicar.
- **FR-003** Desfazer em lote atômico.
- **FR-004** Predefinições salvas pelo usuário (nome + regras).
- **NFR-001** 5.000 arquivos: a pré-visualização atualiza em até 500 ms após cada tecla (medido).

## 5. Casos de borda
- Troca de nomes entre dois arquivos (A→B e B→A). Nomes inválidos no Windows (`:`, `?`, terminar com ponto). Caminho acima do limite.

## 6. Fora de escopo
- Renomear conteúdo de arquivos; renomear por IA.

## 7. Critérios de sucesso
- Renomear 200 fotos pelo padrão `AAAA-MM-DD_NNN` em menos de 30 s de trabalho do usuário, com desfazer funcionando.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: incluir a edição como texto (US4) agora? Recomendação: sim, é barata e muito pedida.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Depende de F001 (blindagem de extensões).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
