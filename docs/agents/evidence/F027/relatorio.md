# F027 — Resultado do experimento do checkbox (20/10/2026, Claude)

Mudança (`DetailsLayoutPage.xaml.cs`): sem pintura do checkbox por linha em `OnSelectionChanged` e em `FileList_ContainerContentChanging`; eventos ligados uma vez por checkbox; estado do checkbox pintado só ao passar o ponteiro (`SelectionCheckbox_PointerEntered`). Build Release `0 Warning(s)`, `0 Error(s)` (CONFIRMED).

A primeira rodada (J1) foi **invalida**: o foco caiu em outras janelas e o Files fechou no meio (itens finais "Message input", "Tab actions menu"). Removida do CSV. Rodada valida: J2, o computador sem uso durante a medicao.

## Medicao (CSV: `docs/agents/evidence/F025/hold_key2.csv`, rotulo `J2-f027`; 5 rodadas, 5 s)
| Teste | Linhas alcancadas | Mediana | CPU por 5 s | Memoria privada |
|---|---|---|---|---|
| `Down` com F027 | 81, 77, 91, 79, 72 | **79** | 7,9 a 9,8 s | 392 a 424 MB |
| `Down` F026 + cache 1.0 (H1, sem F027) | 84 a 94 | ~85 | 7,8 a 10,7 s | 287 a 342 MB |
| `PageDown` com F027 | 66, 39, 61, 36, 61 | 61 | 10,5 a 12,0 s | 442 a 460 MB |

(Linha alcancada = estimada pela ordem alfabetica, INFERRED; `PageDown` anda 25 linhas por tecla no handler da F024.)

- **OBSERVED:** o checkbox lazy **nao mudou** a vazao de `Down` de forma mensuravel (79 contra ~85, dentro do ruido; CPU igual). Por essa metrica, **nao era o gargalo da seta**. O efeito visual (menos atualizacao pendente na tela) so o Alexandre pode julgar: NOT TESTED por mim.
- **OBSERVED:** a memoria privada desta rodada ficou mais alta (392 a 460 MB contra 287 a 342) mas a sessao iniciou com outra aba ("Fontes - Files") restaurada; **nao e comparavel**.
- **OBSERVED, achado novo:** `PageDown` processa so ~1,5 a 2,6 teclas por segundo (36 a 66 linhas = 1 a 3 paginas de 25 em 5 s, de ~150 teclas enviadas) gastando ~11 s de CPU. Cada tecla custa ~450 ms. O handler `PageUp/PageDown` da F024 (`DetailsLayoutPage.xaml.cs`) faz `SetSelectedItem` + `ScrollIntoView` (que chama `ContentScroller.ChangeView`) + `FocusSelectedItems` (de novo `ScrollIntoView` + `ChangeView` + `Focus(Keyboard)`): **dois saltos de rolagem e um foco por tecla**, e o `ChangeView(index*RowHeight)` alinha o item ao **topo** da janela, o que explica a referencia visual fraca relatada. INFERRED (por leitura), falta A/B.
- **Hipotese proxima (nao implementada):** remover os ramos `PageUp/PageDown` da F024 e deixar o comportamento nativo da lista; manter `Home/End` (o Alexandre disse que estao resolvidos).
