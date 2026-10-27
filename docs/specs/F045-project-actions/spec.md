# F045 — Ações por tipo de projeto e comandos próprios — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Ao entrar numa pasta de projeto o desenvolvedor sempre faz as mesmas coisas: compilar, abrir a solução, rodar testes, abrir no terminal. Hoje cada uma é um caminho diferente. O XYplorer e o Directory Opus permitem comandos personalizados; aqui eles podem se adaptar ao projeto.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero uma barra de ações que aparece quando a pasta é um projeto reconhecido (`.sln`, `.dpr`, `pubspec.yaml`, `package.json`).
- **US2 (P1)** Como desenvolvedor, quero criar comandos meus com marcadores (`{caminho}`, `{seleção}`, `{raiz do repositório}`) e vê-los no menu e na busca de comandos.
- **US3 (P2)** Como desenvolvedor, quero ver a saída do comando e se terminou bem ou mal.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta com `.sln` | entro nela | a barra mostra "Compilar", "Abrir solução" e "Testes" |
| AC-2 | pasta sem marcador de projeto | entro nela | a barra não aparece |
| AC-3 | comando próprio `code "{caminho}"` | executo numa pasta | abre o VS Code naquela pasta |
| AC-4 | comando com `{seleção}` e 3 arquivos | executo | cada arquivo é passado conforme a opção (um por vez ou todos juntos) |
| AC-5 | comando com falha | executo | mostro o código de saída e a última parte da saída, sem travar o gerenciador |
| AC-6 | comando demorado | executo | roda em segundo plano, com "Cancelar" |
| AC-7 | caminho com espaços e acentos | executo | chega correto ao comando |

## 4. Requisitos
- **FR-001** Marcadores: caminho atual, itens selecionados, raiz do repositório git, nome, extensão.
- **FR-002** Comandos por tipo de projeto, editáveis e exportáveis (arquivo de configuração versionável).
- **FR-003** Nunca executar nada sem o usuário ter definido/ativado o comando (sem execução automática).
- **NFR-001** Detectar o tipo de projeto não atrasa a abertura da pasta (medido).

## 5. Casos de borda
- Pasta com vários tipos de projeto; comando que precisa de administrador; saída enorme; comando sem arquivo marcador.

## 6. Fora de escopo
- Substituir a IDE; plugins de terceiros.

## 7. Critérios de sucesso
- Compilar o projeto aberto sem sair do gerenciador, em 1 clique.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: integrar com o `mkfile` como padrão para os tipos que ele reconhece? Recomendação: sim.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
