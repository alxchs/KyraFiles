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
