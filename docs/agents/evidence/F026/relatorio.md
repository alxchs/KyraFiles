# F026 — Resultado do experimento (20/10/2026, Claude)

Mudança: `BaseLayoutPage.cs` coalesce os efeitos colaterais da seleção (toolbar e seu re-measure de overflow, tamanho e contagem na barra de status) quando a seleção muda em rajada (< 150 ms entre mudanças); aplica 60 ms depois da última; mudança isolada continua imediata. `DetailsLayoutPage.xaml.cs` aplica o pendente antes de qualquer tecla que não seja navegação. Commit `f554f046c`. Build Release: `0 Warning(s)`, `0 Error(s)` (CONFIRMED, `dotnet build ... -c Release -m:1 --no-restore`).

## Medição: `Down` segurado 5 s, pasta de 1.600 itens (CSV: `docs/agents/evidence/F025/hold_key2.csv`)
Linha alcançada = estimada pela ordem alfabética (`backup_*` primeiro), **INFERRED**. Teto do gerador de teclas ≈ 106 (a lista nativa chegou a 105 a 107).

| Build | Linhas alcançadas (por rodada) | Mediana | CPU de UI na janela | Memória privada |
|---|---|---|---|---|
| Nosso 4.3.1 antes (C1/T/D1) | 43, 36, 52, 59, 79 | 52 | 3.797 ms (1 amostra) | 400 a 650 MB |
| Upstream puro (C2/D2b) | 60, 80, 86, 60, 69 | 69 | 2.250 e 3.219 ms | 360 a 415 MB |
| **Nosso com F026 (E1)** | **89, 96, 94, 93, 82** | **93** | **1.328 e 391 ms** (2 amostras) | 420 a 665 MB |
| Lista nativa do Windows (F, WinForms ListView, sem ícones) | 106, 105, 107, 106, 107 | 106 | n/a | ~22 a 25 MB |

- **OBSERVED:** o experimento levou a mediana de 52 para 93 linhas (+79%) e passou o upstream (69). Está a ~88% do teto do gerador.
- **OBSERVED:** o CPU do thread de UI na janela da tecla caiu de ~3,8 s para 0,4 a 1,3 s (2 amostras; é pouco).
- **Comparação com outra tecnologia:** a lista nativa mantém o ritmo todo da tecla com ~2 s de CPU e ~23 MB. o KyraFiles (WinUI 3) gasta ~11 s de CPU e 400 a 650 MB nessa mesma ação. Mas **não é comparação justa**: a lista nativa não tem ícones, miniaturas, toolbar nem painéis. Serve de teto, não de meta.
- **Ainda alto:** o CPU total do processo continua em ~10 a 12 s por 5 s (outras threads, provável render). A fila de teclas melhorou; o custo bruto não zerou.

## Estado por critério (spec F026)
| AC | Estado |
|---|---|
| AC-1 (linhas ≥ baseline e CPU de UI menor) | OBSERVED a favor; **não é A-B-A estrito**: o "antes" foi medido horas antes no mesmo build sem F026, não intercalado. 5 rodadas do novo; poucas amostras de CPU de UI. |
| AC-2 (toolbar/status atualizam ≤ 100 ms após parar) | NOT TESTED |
| AC-3 (Delete/Enter logo após a seta age no item certo) | NOT TESTED (mitigação: flush antes de teclas que não sejam navegação; risco residual de comando por outro caminho que não o `PreviewKeyDown` da lista) |
| AC-4 (Ctrl+A, Shift+setas, clique) | NOT TESTED |

## Riscos e pendências
- Regressão conhecida da F024 (Shift/Ctrl com Home/End/PgUp/PgDn) continua; não foi tratada aqui.
- Toolbar fica até ~60 ms desatualizado durante a rajada (intencional).
- Falta testar na pasta real (`D:\DriveD\Downloads`, com miniaturas) e em uso real com o teclado físico.

---

## Adendo 1 (20/10/2026 20:40) — causa provável da "tela limpa": `CacheLength="10.0"` (nosso)

Relato do Alexandre: segurando a tecla no KyraFiles, a tela fica toda limpa e se perde a referência visual da linha; no Explorer do Windows também é lento, mas a referência não se perde. (Sintoma já existia; faltou a informação.)

- **Não reproduzi a tela limpa** com teclas sintéticas (quadros em `docs/agents/evidence/F026/frames/`, script `tools\perf\f025_capture_hold.ps1`; a métrica "vazio %" não discrimina, ficou em 30 a 32% sempre). Nos quadros se vê o toolbar perdendo os rótulos durante a rajada (re-medição). **NOT TESTED:** tela limpa em uso real.
- **CONFIRMED por leitura (git):** `DetailsLayoutPage.xaml` linhas 1387 e 1496 têm `CacheLength="10.0"` desde o commit-base do fork `22fd57762` (F001 a F020). No upstream `0e3c17ca4` o atributo não existe (padrão do `ItemsStackPanel`: 4). Com 10, o app mantém ~10 telas de linhas materializadas de cada lado; cada rajada de rolagem cria e descarta centenas de contêineres. Cada linha também tem um `ItemsControl` aninhado de "colunas dinâmicas" (nosso, F013).
- **Mudança testada (1 variável):** `CacheLength` de 10.0 para 1.0 nos dois pontos. Build Release 0/0.

| Build | Linhas em 5 s (mediana) | CPU por 5 s | Memória privada após rodadas |
|---|---|---|---|
| F026 (CacheLength 10) | 93 | 10,4 a 12,0 s | 420 a 665 MB |
| **F026 + CacheLength 1.0 (H1)** | **84 a 94 (≈85)** | **7,8 a 10,7 s** | **287 a 342 MB** |
| Upstream puro | 69 | 9,9 a 13,4 s | 360 a 415 MB |

- **OBSERVED:** memória privada caiu ~40% e ficou **abaixo do upstream**; CPU caiu um pouco; a linha alcançada ficou igual (dentro do ruído).
- **INFERRED:** a tela limpa vem do volume de contêineres criados por rajada. Falta confirmar com o Alexandre.
- **Próximo suspeito, ainda não medido:** o `ItemsControl` aninhado por linha (colunas dinâmicas); `x:Phase`/adiamento de realização; animações de reposicionamento dos itens.
