# F028 — Resultado (20/10/2026, Claude)

Build Release `0 Warning(s)`, `0 Error(s)` em cada etapa (CONFIRMED). Pasta `C:\FilesUXLab\perf\1600`. Linha alcançada estimada pela ordem alfabética (INFERRED).

## Trace do thread de UI, `Down` segurado (`f025_speedscope_top.ps1`)
| | F027 (`trace_down_f027`) | F028 etapa 1 (`trace_down_f028`) |
|---|---|---|
| CPU gerenciado no thread de UI | 2.970 ms | 2.065 ms |
| `IsVSCodeInstalled` / registro | 737 ms | some |
| `PopulateToolbarItems` | 1.261 ms | 395 ms |
| Detecção QuickLook/Peek | 477 ms | 302 ms (corrigido na etapa 2: cache de 5 s no caminho de troca) |
| Código nativo (WinUI) | 73% | 81% |

## Medição numérica (`hold_key2.csv`; 5 rodadas, 5 s)
| Build | `Down` linhas (mediana) | `Down` latência máx. da UI | `PgDn` | Memória privada |
|---|---|---|---|---|
| F027 | 79 | 53 a 422 ms | 36 a 66 linhas, ~2 teclas/s | 392 a 460 MB |
| F028 etapa 2 (sem espera do quadro) | 96 | 69 a 143 ms | foco escapava para a barra lateral | 314 a 334 MB |
| **F028 final (P1)** | **86 (45 a 95)** | **20 a 35 ms** (uma rodada 174) | **~500 a 900 linhas, quase sem sobra ao soltar** | 427 a 449 MB |

## Imagem (o critério do Alexandre)
- `contato_f028d_PageDown.png` / `contato_f028e_PageDown.png`: o `PgDn` anda **só para frente**, uma página por quadro, realce na base em 6 de 8 quadros. Antes (traço por UI Automation), o foco ia e voltava.
- `contato_f028d_Down.png` (com espera de quadro e efeitos a 120 ms): realce sempre visível, mas só ~15 linhas em 4 s.
- `contato_f028e_Down.png` (final): **realce em todos os quadros, andando só para frente, lista rolando junto**; ~14 linhas/s na captura (a captura de tela também consome CPU), ~7 linhas a mais nos 800 ms depois de soltar.

## Estado por AC
| AC | Estado |
|---|---|
| AC-1 | OBSERVED: `Down` 86 contra 79 (F027), latência da UI caiu de centenas de ms para ~30 ms; `PgDn` muito maior. Registro e QuickLook fora do trace na etapa 1/2. CPU por 5 s ainda 7 a 14 s. |
| AC-2 (Abrir no IDE) | NOT TESTED |
| AC-3 (preview acompanha) | NOT TESTED |
| AC-4 (PgDn mantém item visível) | OBSERVED em 6 de 8 quadros |
| AC-5 (imagem anda para frente) | OBSERVED (folhas de contato acima) |

## Limites
- Teclas sintéticas, pasta de laboratório; a sensação real só o Alexandre confirma.
- A memória voltou para o nível do upstream (cache padrão) em troca de não esvaziar a tela ao rolar com o mouse.
- CPU bruto continua alto: ~80% do tempo do thread de UI é código nativo do WinUI (layout e desenho das linhas). É o próximo alvo (modelo da linha: colunas dinâmicas aninhadas, ícones, tags).

---

## Adendo 1 (20/10/2026 22:45) — carregamento de propriedades durante a tecla segurada

- **Diagnóstico descartado:** sem o ícone de nuvem e sem as colunas dinâmicas por linha, nada mudou (`Down` 84 contra 86; CPU de `PgDn` igual). Revertido.
- **Trace `trace_pgdn.speedscope.json`:** `ShellViewModel.LoadExtendedItemPropertiesAsync` 16% do thread de UI (`GetFileFromPathAsync` 10%, nuvem 4,4%); `StatusBar.QueueRemeasure` com `UpdateLayout` síncrono 3,8% (upstream, não mexido); tooltip por linha 3,6%.
- **Achado:** `KeyStatus.WasKeyDown` não funciona no WinUI 3. `RepeatProbe` (WinForms, bit 30 do `WM_KEYDOWN`) mostrou que o Windows marca 29/30 teclas sintéticas como repetição, ou seja, a espera de quadro da F028 **não estava ativa** nas medições anteriores; a melhora visual veio do atraso de 350 ms dos efeitos.
- **Instrumentação temporária** (removida, `C:\FilesUXLab\f028_diag.log`): antes da correção, 169 cargas iniciadas e 0 adiadas, e o timer de 200 ms encerrava o adiamento entre páginas; depois, 292 adiadas e um único encerramento após soltar.

| Build (Y1, final) | `Down` linhas (mediana) | `PgDn` CPU por 5 s (mediana) |
|---|---|---|
| F028 P1 | 86 | 13,4 s |
| **F028 final** | **83** | **10,3 s** |

- `contato_f028h_Down.png`: realce visível em todos os quadros, andando só para frente, lista rolando junto. `contato_f028h_PageDown.png` gerado.
- **Ainda alto:** ~80% do thread de UI é código nativo do WinUI criando as linhas de cada página.

---

## Adendo 2 (21/10/2026 09:15, Claude) — contorno de foco "pintado no nada" (FR-3 revisada, FR-8, AC-6)

- **Relato do Alexandre** (build de 21/10 00:55, commit `e59007015`): PgUp/PgDn "absurdamente melhor, mas ainda pinta o retângulo no nada".
- **Causa (CONFIRMED por captura):** `foco_antes_PageDown_quadro02.png` mostra o contorno desenhado **sobre o cabeçalho das colunas**. O PgDn rolava `página × RowHeight` e escolhia `atual + página`, e com isso o destino ficava na primeira linha do viewport, coberta pelo cabeçalho fixo (`HeaderGrid`, 40 px).
- **Instrumentação temporária** (removida, `C:\FilesUXLab\f028_diag.log`): linha 0 em y=38 do conteúdo, altura real 24 px, cabeçalho fixo em y=0 (às vezes lido em −24 no meio do reposicionamento). Primeira versão da correção: depois do 1º toque, o foco nativo rolava mais ~7 px e expunha uma linha a mais embaixo; a regra "ir para a última linha visível" passava a andar 1 linha por toque. Corrigido: toques seguidos na mesma direção sempre viram uma página (45 linhas nesta janela).
- **Correção** (`DetailsLayoutPage.TryMovePage`): geometria lida dos contêineres realizados; área útil começa abaixo do cabeçalho; 1º toque vai à linha da borda, toques seguintes viram página; deslocamento mínimo para o destino ficar inteiro na área útil. Lista agrupada: nativo. Mais 3 avisos `CsWin32 CsWinRT1034` resolvidos (2 vinham de `e59007015`).
- **Quadros depois** (`tools/perf/f028_focus_frames.ps1`, janela encaixada na tela, foco por clique): `foco_f028v4_PageDown.png` e `foco_f028v4_PageUp.png`. O contorno nunca aparece sobre o cabeçalho nem numa linha vazia; com PgDn fica na linha de baixo, com PgUp na de cima. Em 2 quadros do PgUp a lista parou 6 linhas abaixo do pedido (item visível, com conteúdo; causa não investigada).

| Build (5 rodadas, 5 s, `f025_hold_key.ps1 -NoNav`) | `PgDn` linhas (mediana) | `PgDn` CPU (mediana) | `Down` linhas (mediana) | Memória privada |
|---|---|---|---|---|
| F028 Y1 (20/10) | 643 (270 a 812) | 10,3 s | 82 | 408 a 438 MB |
| **F028 v4 (21/10)** | **765 (585 a 855)** | 11,5 s | 74 (65 a 83) | 512 a 549 MB |

- `Down` não foi alterado (nativo); a diferença está dentro do ruído das rodadas. A memória privada maior foi **observada**, não investigada: esta sessão tinha duas abas abertas (1600 e Downloads); não atribuída à correção, que não guarda nada por linha.

| AC | Estado |
|---|---|
| AC-4 (item visível) | CONFIRMED pelos quadros acima |
| AC-6 (contorno nunca sobre o cabeçalho ou linha vazia) | OBSERVED em todos os quadros estáveis de PgDn e PgUp; teclas sintéticas |
| Shift+PgUp/PgDn | NOT TESTED |
| Lista agrupada (nativo) | NOT TESTED |
