# Backlog de funcionalidades — aprovação do Alexandre

Atualizado em 24/10/2026 (Claude). Cada linha é uma spec em `docs/specs/<ID>/spec.md`, em RASCUNHO.

**Como funciona (METODO §6):** você marca a caixa da spec que aprova (e responde os `[NEEDS CLARIFICATION]`, ou aceita a "Recomendação" escrita neles). Só então o Claude escreve `plan.md` e `tasks.md`, e só com as tasks aprovadas a AGY recebe a ordem. Marcar aqui **não** inicia código.

Legenda de valor: ★★★ muito alto · ★★ alto · ★ médio. Esforço: P pequeno · M médio · G grande.

## Parte A — Specs prontas para aprovar

| Aprovo | ID | Funcionalidade | Valor | Esforço | Pontos em aberto | Depende de |
|---|---|---|---|---|---|---|
| - [ ] | [F035](F035-second-instance/spec.md) | Segunda instância e abertura direta (corrige crash real) | ★★★ | P | 1 | — |
| - [ ] | [F056](F056-copy-path-formats/spec.md) | Copiar caminho em vários formatos | ★★ | P | 0 | — |
| - [ ] | [F047](F047-build-folder-cleaner/spec.md) | Limpar pastas pesadas de build | ★★★ | P | 1 | — |
| - [ ] | [F049](F049-links-symlinks-junctions/spec.md) | Links simbólicos, junctions e hardlinks | ★★ | P | 1 | — |
| - [ ] | [F037](F037-bulk-rename-preview/spec.md) | Renomear em lote com pré-visualização | ★★★ | M | 1 | F001 |
| - [ ] | [F036](F036-copy-queue/spec.md) | Fila de cópias com pausar e retomar | ★★★ | G | 2 | — |
| - [ ] | [F044](F044-spacebar-quick-look/spec.md) | Visualização rápida com a barra de espaço | ★★★ | M | 1 | F005 |
| - [ ] | [F043](F043-operation-history/spec.md) | Histórico de operações com desfazer | ★★ | M | 1 | — |
| - [ ] | [F038](F038-named-workspaces/spec.md) | Espaços de trabalho com nome | ★★ | M | 1 | F031, F032 |
| - [ ] | [F048](F048-file-diff/spec.md) | Comparar arquivos e pastas (diff) | ★★ | P | 1 | — |
| - [ ] | [F045](F045-project-actions/spec.md) | Ações por tipo de projeto e comandos próprios | ★★★ | M | 1 | — |
| - [ ] | [F052](F052-git-repo-view/spec.md) | Visão de repositório Git (ignorados, branch, worktrees) | ★★ | M | 1 | — |
| - [ ] | [F039](F039-disk-usage-map/spec.md) | O que está ocupando meu disco | ★★★ | G | 1 | F023 (opcional) |
| - [ ] | [F040](F040-duplicate-finder/spec.md) | Localizar arquivos duplicados | ★★ | M | 1 | F043 (opcional) |
| - [ ] | [F041](F041-folder-compare-sync/spec.md) | Comparar e sincronizar duas pastas | ★★ | G | 1 | F036 (opcional) |
| - [ ] | [F042](F042-saved-searches/spec.md) | Buscas salvas na barra lateral | ★★ | M | 1 | F023 |
| - [ ] | [F046](F046-deep-network-search/spec.md) | Busca profunda na rede | ★★★ | G | 2 | `R-rede-baseline` (medição) |
| - [ ] | [F051](F051-code-search/spec.md) | Busca de código no estilo ripgrep | ★★ | M | 1 | — |
| - [ ] | [F053](F053-encoding-eol-column/spec.md) | Codificação e fim de linha (colunas e conversão) | ★ | M | 1 | — |
| - [ ] | [F054](F054-technical-preview/spec.md) | Prévia técnica (código, JSON, CSV, log) | ★★ | G | 1 | F005, F030 |
| - [ ] | [F055](F055-repo-map/spec.md) | Mapa do repositório | ★ | G | 1 | F052 (opcional) |
| - [ ] | [F050](F050-embedded-terminal/spec.md) | Terminal embutido no rodapé (candidata a adiar) | ★ | G | 1 | — |

## Parte B — Ordem proposta para a AGY (valendo só depois da aprovação e das tasks)

Regra: uma feature por branch (`feature/<nome>`), e a AGY só começa a próxima com a evidência da anterior **gerada por teste com asserção** (ver orientação de 24/10/2026). Nada disso passa na frente da fila atual (F030/F032/F033/F034, F029, F023, F026, `R-rede-baseline`).

1. **Onda 1 (pequenas, valor imediato):** F035, F056, F047, F049.
2. **Onda 2:** F037, F044, F045, F048.
3. **Onda 3:** F043, F038, F052, F036 (a maior da onda; mexe no coração da cópia, exige medição de memória).
4. **Onda 4 (dependem de índice/medição):** F042 (após F023), F046 (após a baseline de rede), F039, F040, F041.
5. **Onda 5:** F051, F053, F054.
6. **Última:** F055; F050 só se a medição de custo justificar.

## Parte C — Ideias de concorrentes ainda sem spec (para sua análise)

Pesquisa de 24/10/2026 em Total Commander, XYplorer, Directory Opus, OneCommander, FreeCommander e Q-Dir, cruzada com o código atual. **Já existem aqui e ficaram de fora:** tags coloridas, colunas Miller, painel duplo, reabrir aba fechada, integração com QuickLook/Seer, hash nas propriedades, EXIF/GPS nas propriedades.

| Marque o que quer virar spec | Ideia | Quem tem | Aqui hoje |
|---|---|---|---|
| - [ ] | Regras de cor por nome, tamanho ou idade (ex.: arquivos de mais de 1 ano em cinza) | XYplorer | não existe (busca no código: 0 ocorrências) |
| - [ ] | Filtros visuais globais ("só o que criei esta semana") que valem em todas as pastas até desligar | XYplorer | só o filtro de cabeçalho, por pasta |
| - [ ] | Dividir e juntar arquivos grandes (para e-mail, pendrive FAT32) | Total Commander, FreeCommander | não existe (0) |
| - [ ] | Gerar e verificar arquivos de checksum (`.sha256`, `.blake3`) de uma pasta inteira | Total Commander | só hash de um arquivo nas propriedades |
| - [ ] | Visão de ramo ("todos os arquivos das subpastas numa lista só") | Total Commander, Directory Opus | não existe (a ação "Achatar pasta" faz outra coisa) |
| - [ ] | Pastas favoritas com atalho numérico (Ctrl+1…9 salta para elas) | Total Commander | não existe |
| - [ ] | Quatro painéis (2×2) | Q-Dir | só 2 painéis |
| - [ ] | Modo portátil (roda de pendrive, sem instalar, guardando as configurações ao lado) | XYplorer | não existe |
| - [ ] | Apagar com sobrescrita (wipe) para dados sensíveis | FreeCommander | não existe |
| - [ ] | Árvore reduzida ("mini tree": só o ramo atual e favoritos) | XYplorer | não existe |
| - [ ] | Pesquisar dentro de arquivos compactados sem extrair | Total Commander, Directory Opus | **não verificado** no código |

Fontes das funcionalidades dos concorrentes: [Total Commander](https://www.ghisler.com/featurel.htm), [XYplorer](https://www.xyplorer.com/features.php), [Directory Opus](https://dopus.com), [OneCommander](https://onecommander.com/index.php), [FreeCommander e Q-Dir (comparativo)](https://helpdeskgeek.com/the-8-best-file-managers-for-windows-11-10-in-2022/).
