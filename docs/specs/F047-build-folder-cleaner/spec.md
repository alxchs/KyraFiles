# F047 — Limpar pastas pesadas de build — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Projetos acumulam `node_modules`, `bin`/`obj`, `.venv`, `target`, `.gradle`, `build`, `.dart_tool`: gigabytes recriáveis. Em 24/10/2026 duas pastas `bin` somaram cerca de 740 MB, apagadas à mão. Falta uma ação que ache, meça e limpe com segurança.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero listar as pastas de build recriáveis dentro de uma pasta, com tamanho e data de último uso.
- **US2 (P1)** Como desenvolvedor, quero apagar as escolhidas (para a Lixeira) sabendo que o projeto continua íntegro.
- **US3 (P2)** Como desenvolvedor, quero definir quais nomes de pasta contam, por tipo de projeto.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta com 3 projetos (.NET, Node, Flutter) | "Limpar pastas de build" | lista `bin`, `obj`, `node_modules`, `build`, `.dart_tool` etc., cada uma com tamanho e a quem pertence |
| AC-2 | item cujo projeto **não** tem o arquivo marcador (ex.: `bin` sem `.csproj` vizinho) | listo | não é oferecido por padrão (evita apagar `bin` de outra coisa) |
| AC-3 | itens marcados | "Mover para a Lixeira" | vão para a Lixeira com confirmação mostrando o total a liberar |
| AC-4 | pasta em uso (build rodando) | tento apagar | o item é pulado e listado como "em uso" |
| AC-5 | worktree do git com alterações não salvas | listo | a pasta de build é oferecida; os arquivos rastreados nunca são tocados |
| AC-6 | total liberado | olho o resumo | o resumo bate com o tamanho medido antes (diferença explicada) |

## 4. Requisitos
- **FR-001** Cada nome só vale com o arquivo marcador do tipo (ex.: `package.json` para `node_modules`).
- **FR-002** Nunca apagar nada rastreado pelo git.
- **FR-003** Lista editável de nomes e marcadores.
- **NFR-001** Varredura de 200 projetos em menos de 30 s (medido), sem entrar dentro das pastas encontradas.

## 5. Casos de borda
- Pastas aninhadas (`node_modules` dentro de `node_modules`); links simbólicos; caminhos longos do Windows.

## 6. Fora de escopo
- Limpeza de caches globais (`~/.nuget`, `~/.gradle`).

## 7. Critérios de sucesso
- Liberar o espaço de `bin/obj/node_modules` de uma pasta de projetos em menos de 1 minuto, sem quebrar nenhum projeto (`mkfile` volta a compilar).

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: apagar direto para a Lixeira, ou apagar definitivo (mais rápido, pastas enormes)? Recomendação: Lixeira por padrão, com opção de definitivo.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
