# F060 — TASKS

Status: **APROVADAS** (Claude, 26/10/2026). Executora: AGY. Revisor e fechamento: Claude. Uma tarefa por commit, na branch indicada. Regras: `docs/agents/ORDEM-RIGIDA-AGY.md`, R1 a R13, nenhuma entrada global de teclado/mouse, sem push, sem merge. Teste de tela só depois das 23:59 e sem roubar foco (METODO §16). Temporários em `%TEMP%\agy\<GUID>\...`. Cada tarefa termina com handoff por AC.

Branches: `feature/f060-m1-split-combine`, `feature/f060-m5-natural-search`, `feature/f060-m2-hex-editor`, `feature/f060-m3-sftp`, `feature/f060-m4-script`. Cada uma a partir da `main` atualizada, em worktree próprio (`ia work <slug>`).

## Onda A — independentes, núcleo puro (liberadas já)

### M1 Dividir/Juntar (branch `feature/f060-m1-split-combine`)
- [x] **M1-T1 — Dividir (FR-M1; M1-AC-1, AC-4).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (commits `30e83ae19` e `22c4b8f2b` em `feature/f060-m1-split-combine`; 5 testes verdes).* `SplitEngine` + `SplitManifest` em `Files.Shared/Helpers`. Leitura em blocos de buffer fixo; partes de tamanho exato; manifesto versão 1 com CRC32 por parte e do total, SHA-256 opcional. Pronto: testes de arquivo de 10 MB/4 MB (3 partes), vazio, menor que uma parte, múltiplo exato; teste de controle que reprova de propósito e é removido; `dotnet test` verde; original com hash igual antes/depois.
- [x] **M1-T2 — Juntar com validação (M1-AC-2, AC-3, AC-5, AC-6).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (`965b6fd8b` + correção `6d4037982`: manifesto adulterado não pode ler nem gravar fora da pasta; controle provou que o teste reprova sem a correção). DISCOVERY: `ScanDirectory_HardLink_AppearsOnceWithPrimaryName_AC21` falha de forma intermitente (1 em ~5 rodadas completas), arquivo não tocado por esta feature.* `CombineEngine`: valida ausente, duplicada, truncada, alterada e fora de ordem **antes** de montar; monta em `.tmp`; promove só após CRC32 do total; destino existente exige política explícita; falta de espaço (simulada por stream que falha) não deixa arquivo final. Pronto: um teste por caso; resultado idêntico ao original por comparação independente (SHA-256 externo).
- [x] **M1-T3 — Medição de memória (NFR-002).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (`c385ab145`; CSV `measure_split_combine_20261027_073241.csv`: pico de 35,69 MB com 256 MB e 35,39 MB com 4 GB, razão 0,99x; SHA-256 conferido nos dois).* Script em `tools/perf/` que divide e junta um arquivo de 4 GB gerado em `%TEMP%\agy\<GUID>\...`, medindo o pico do processo; CSV com o comando. Pronto: pico não proporcional ao tamanho; senão, registrar como bug.
- [ ] **M1-T4 — Ações e diálogo (US M1).** `SplitFileAction`, `CombineFilesAction`, diálogo de tamanho/destino, progresso e cancelar no Status Center, textos em `Strings/en-US`. Pronto: build 0 erros e sem warnings novos; dependência: T1 e T2 aprovadas pelo Claude.
- [ ] **M1-T5 — Prova de tela (AC-1..AC-6).** Depois das 23:59, sem roubar foco, UIA; `resultado.csv` por AC com controle negativo. Pronto: ver §16 do método.
- [ ] **M1-T6 — Fechamento.** `STATUS.md`, `CHANGELOG.md`, handoff por AC; texto ao usuário: "Novo: dividir um arquivo grande em partes e juntar de volta conferindo se nada se perdeu."

### M5 Busca por frase (branch `feature/f060-m5-natural-search`)
- [x] **M5-T0 — Mapa da busca (somente leitura).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (`197e4067f` em `feature/f060-m5-natural-search`; afirmações conferidas por amostragem). Achado: consulta com `:` já vai como AQS ao Windows Search; não há filtro estruturado; pós-filtro em `OnBatchReceived` é o ponto seguro para os provedores sem suporte.* Onde a busca recebe consulta e filtros de extensão, data de modificação e tamanho, em `FolderSearch.cs` e `Files.App.Search.Index`; se já existe filtro estruturado. Saída: `docs/agents/evidence/F060/m5-t0.md` com comandos. Pronto: cada afirmação CONFIRMED/INFERRED.
- [x] **M5-T1 — Tradutor (FR-M5; M5-AC-1, AC-3, AC-4, AC-5).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (`3f8c0a30f`; 14 testes verdes, tabela com 40 frases ou mais, sem relógio real). Nota: `SearchIntent` tem propriedades-apelido redundantes (SearchText/RemainingText etc.): enxugar na M5-T2.* `NaturalSearchParser` em `Files.Shared/Helpers`, PT e EN, vocabulário em tabela. Saída `SearchIntent` (filtros + trechos não entendidos). "Salvo/alterado" = modificação, "criado" = criação, "semana passada" = últimos 7 dias. Pronto: tabela com **pelo menos 40 frases** (entrada → filtros esperados), incluindo "todos os arquivos ini salvos nos últimos 12 dias", ambíguas ("recentes"), sem filtro reconhecível, extensão com e sem ponto, virada de mês/ano (data fixa injetada, nunca o relógio real). Teste de controle que reprova de propósito.
- [ ] **M5-T2 — Chips e execução (M5-AC-1, AC-2).** Campo de frase + chips editáveis; ao confirmar, aplica os filtros na busca existente (ponto definido na T0). Flag experimental desligada em `GeneralSettingsService` + interruptor na página Geral. Pronto: build 0 erros; dependência: T0 e T1.
- [ ] **M5-T3 — Prova de tela (AC-1..AC-5).** Depois das 23:59, sem roubar foco.
- [ ] **M5-T4 — Salvar a busca (M5-AC-6).** **BLOQUEADA** até a F042 estar entregue.
- [ ] **M5-T5 — Fechamento.** Texto ao usuário: "Novo (experimental): digite o que procura em uma frase, como 'arquivos ini dos últimos 12 dias'."

### M2 Editor hexadecimal (branch `feature/f060-m2-hex-editor`)
- [ ] **M2-T0 — Decisão D-F060-1 (somente texto).** Registrar em `DECISIONS.md` a recomendação (gravar só os blocos alterados no próprio arquivo, com diário de recuperação; v1 só substitui bytes). Pronto: decisão escrita com Context/Options/Reason; o Alexandre pode vetar antes da T2.
- [x] **M2-T1 — Buffer e edição (M2-AC-2, AC-4, AC-5).** *Entregue pela AGY e revisada pelo Claude em 27/10/2026 (`babe51068` em `feature/f060-m2-hex-editor`; 8 testes verdes, arquivo de 64 MB, limite do cache afirmado). RISCO para a M2-T4: cada byte editado custa um item de dicionário (dezenas de bytes) mais o desfazer; colar centenas de MB estoura a memória. A tela deve limitar o tamanho de cada colagem (ex.: 1 MB) e avisar.* `HexFileBuffer` em `Files.Shared/Helpers`: leitura por blocos, cache LRU limitado, mapa de edições, desfazer. Pronto: testes em arquivo gerado de 64 MB com deslocamentos no começo, meio e fim; sem carregar o arquivo inteiro (teste cita o limite de cache).
- [ ] **M2-T2 — Salvar com segurança (M2-AC-4, AC-6, AC-7).** Conferir identidade (tamanho e data) antes; gravar blocos alterados com diário; apagar o diário ao fim; arquivo somente leitura/em uso/sem espaço deixam o original íntegro (simular falhas por stream). Pronto: teste de queda no meio da gravação recupera pelo diário. Depende de T1 e da D-F060-1 sem veto.
- [ ] **M2-T3 — Medição (NFR-002, M2-AC-1).** Arquivo de 4 GB, abrir, ir ao fim, alterar 1 byte, salvar; CSV com tempo e pico de memória.
- [ ] **M2-T4 — Tela virtualizada (M2-AC-1..3).** Página nova com coluna hexa e coluna de texto (a de texto só mostra bytes, nunca os altera), só linhas visíveis. Depende de T1 e T2.
- [ ] **M2-T5 — Prova de tela e fechamento.** Depois das 23:59, sem roubar foco. Texto ao usuário: "Novo: editor hexadecimal para abrir e corrigir bytes de arquivos grandes."

## Onda B — com decisão ou dependência (só depois da Onda A, ou quando a dependência destravar)

### M3 SFTP (branch `feature/f060-m3-sftp`)
- [ ] **M3-T0 — Revisão FTP/FTPS e gate de biblioteca (somente leitura, liberada já).** Como o FluentFTP é usado hoje (FTPS explícito/implícito, validação de certificado, onde ficam credenciais, retomada); gate do §10.7 da SPEC original para SSH.NET (licença, versão, alvo de framework). Saída: `docs/agents/evidence/F060/m3-t0.md` e ADR-007 em `Proposed`. Pronto: cada afirmação com comando.
- [ ] **M3-T1..T4** (provedor SFTP, host key, credenciais, erros distintos): definidos depois da T0 aprovada; **T5 integração com a fila** só depois da **F036**.

### M4 Script (branch `feature/f060-m4-script`)
- [ ] **M4-T0 — ADR-008 com protótipo** (Lua/MoonSharp x JavaScript/Jint: licença, timeout, API restrita). Sem tocar em `src/` do app. Pronto: ADR `Proposed` com medição.
- [ ] **M4-T1..** definidas após a T0; ações em lote via fila só depois da **F036**.

## Ordem para a AGY (uma por vez, cada uma revisada pelo Claude antes da próxima)
1. M1-T1 → M1-T2 → M1-T3 (núcleo puro).
2. Em paralelo seguro (somente leitura): M5-T0, M3-T0.
3. M5-T1 → M2-T0 → M2-T1 → M2-T2 → M1-T4 → M5-T2 → M2-T3/T4.
4. Telas (M1-T5, M5-T3, M2-T5) agrupadas numa janela depois das 23:59.
