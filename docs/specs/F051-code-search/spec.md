# F051 — Busca de código no estilo ripgrep — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Procurar um texto dentro de arquivos do projeto é diário. A busca de conteúdo atual depende de um programa externo (Agent Ransack, F013). Falta uma busca interna que entenda `.gitignore`, aceite regex e abra a IDE na linha achada.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero buscar texto dentro da pasta atual e ver arquivo, linha e trecho.
- **US2 (P1)** Como desenvolvedor, quero ignorar o que o `.gitignore` ignora (`node_modules`, `bin`).
- **US3 (P1)** Como desenvolvedor, quero abrir o resultado na minha IDE direto na linha.
- **US4 (P2)** Como desenvolvedor, quero regex, diferenciar maiúsculas e filtro por extensão.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | repositório com 10 mil arquivos | busco `TODO` | resultados aparecem enquanto a busca anda, com arquivo, linha e trecho |
| AC-2 | `.gitignore` com `bin/` | busco | nada dentro de `bin/` é lido nem listado (com opção para incluir) |
| AC-3 | resultado selecionado | "Abrir na IDE" | a IDE abre o arquivo na linha do resultado |
| AC-4 | regex inválida | busco | explica o erro; não busca |
| AC-5 | arquivos binários e enormes | busco | são pulados e contados como "ignorados" |
| AC-6 | busca em andamento | "Cancelar" | para em até 1 s |
| AC-7 | codificações UTF-8 (com e sem BOM) e UTF-16 | busco texto com acento | encontra nos três |

## 4. Requisitos
- **FR-001** Resultados incrementais; limite de resultados e de tamanho de arquivo configuráveis.
- **FR-002** Não bloquear os arquivos lidos nem alterar a data de acesso de forma visível.
- **FR-003** IDE configurável (`code -g arquivo:linha`, Visual Studio, Rider, Notepad++).
- **NFR-001** Memória estável; 10 mil arquivos de código em até 3 s numa busca simples (medido).

## 5. Casos de borda
- Linhas gigantes (minificados); links simbólicos; pastas de rede (lento, com tempo limite); sem repositório git.

## 6. Fora de escopo
- Substituir em massa (localizar e substituir); busca semântica.

## 7. Critérios de sucesso
- De "preciso achar onde uso `Foo`" ao arquivo na linha certa em menos de 15 s.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: usar o `rg` (ripgrep) instalado se existir, ou sempre o motor próprio? Recomendação: motor próprio, `rg` só como acelerador opcional.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
