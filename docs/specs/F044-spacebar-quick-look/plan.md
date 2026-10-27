# F044 — Visualização rápida com a barra de espaço — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** já existe prévia por **programa externo**: `Actions/Content/PreviewPopup/LaunchPreviewPopupAction.cs` pede `IPreviewPopupService.GetProviderAsync()`, que devolve, nesta ordem, `QuickLookProvider`, `SeerProProvider`, `PowerToysPeekProvider` (`Services/PreviewPopupProviders/PreviewPopupService.cs`); sem nenhum instalado devolve `null` e **nada acontece**.
- **CONFIRMED** o contrato é pequeno (`Data/Contracts/IPreviewPopupProvider.cs`): `TogglePreviewPopupAsync(path)`, `SwitchPreviewAsync(path)`, `DetectAvailability()`. A ação já usa `SwitchPreviewAsync` quando a seleção muda (linhas ~51-67).
- **CONFIRMED** os três layouts engolem a tecla Espaço (`DetailsLayoutPage.xaml.cs:707`, `GridLayoutPage.xaml.cs:633`, `ColumnLayoutPage.xaml.cs:484`: `e.Handled = true`); `BaseShellPage.cs:419` trata Ctrl+Espaço para tocar mídia. **INFERRED**: o atalho Espaço→`LaunchPreviewPopup` vem do sistema de comandos/atalhos (hotkey); T0 localiza a associação exata.
- **CONFIRMED** os leitores de prévia já existem (`UserControls/FilePreviews/*`, hospedados em `UserControls/Pane/InfoPane.xaml` pelo `InfoPaneViewModel.PreviewPaneContent`).

## 2. Abordagem
**Um quarto provedor, embutido, no fim da fila** (decisão da spec: sobreposição própria, reaproveitando os leitores): `BuiltInPreviewProvider : IPreviewPopupProvider`. `PreviewPopupService` tenta QuickLook, Seer e Peek **antes** dele (AC-6: o externo escolhido continua mandando) e só usa o embutido se nenhum estiver disponível. Isso é mudança mínima: nenhuma ação nova, nenhuma tecla nova.
- **Sobreposição:** um controle por cima da área da lista (na página do shell), com fundo translúcido, mostrando o leitor adequado ao tipo (imagem, texto/código, PDF, mídia, Markdown) e uma barra com nome, tamanho e data. Esc e Espaço fecham e devolvem o foco ao item (AC-3).
- **Navegação (AC-2):** enquanto aberta, ↑/↓ movem a seleção da lista por baixo e a ação existente chama `SwitchPreviewAsync`; a sobreposição só captura as teclas que fecha ou navega.
- **Sem prévia (AC-4):** cartão com ícone, tipo, tamanho e o motivo ("binário", "bloqueado"), nunca erro.
- **Pasta (AC-5):** resumo (itens e tamanho) calculado em segundo plano, cancelável.
- **Espaço segurado (AC-7):** só reage ao primeiro `KeyDown` (ignorar repetição automática).
- **Texto nos campos:** em renomear e caixas de texto o Espaço continua sendo espaço (FR-003): a sobreposição só reage quando o foco está na lista.
- **Arquivo nunca bloqueado depois de fechar (FR-002):** abrir com compartilhamento de leitura e liberar ao fechar; teste de controle abre o arquivo para escrita depois.
Opções descartadas: (a) reaproveitar o painel lateral (F005): não é "por cima da lista" e some quando o painel está fechado; (b) criar um segundo atalho: o Espaço já é o esperado.

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 (tipos) | leitores de `FilePreviews/*` hospedados na sobreposição; T0 lista o que o painel já cobre |
| FR-002 | abertura com compartilhamento e liberação ao fechar |
| FR-003 | condição de foco no tratamento do Espaço |
| AC-1, AC-2, AC-3, AC-7 | `BuiltInPreviewProvider` + sobreposição + `LaunchPreviewPopupAction` (já chama `SwitchPreviewAsync`) |
| AC-6 | ordem em `PreviewPopupService.GetProviderAsync` |
| NFR-001 | cache do último leitor e pré-carga do vizinho; medição em T6 |

**Arquivos a mexer:** novo `BuiltInPreviewProvider` e o controle de sobreposição; `PreviewPopupService` (acrescentar o último provedor); a página do shell que hospeda a sobreposição; os três layouts **só** se T0 mostrar que o `e.Handled = true` impede o atalho; `Strings/en-US`. **A NÃO mexer:** `QuickLookProvider`, `SeerProProvider`, `PowerToysPeekProvider`, `InfoPane` (só reaproveitar os leitores).

## 4. Riscos
- **Foco e teclado:** a sobreposição não pode roubar teclas da lista nem prender o foco (testar abrir, fechar e voltar).
- **Memória:** imagem enorme ou vídeo na sobreposição; limite de tamanho e liberação ao fechar; medir (`Get-Process`) abrindo 50 imagens em sequência.
- **Arquivos "só na nuvem":** não baixar sem pedir (mostrar cartão com aviso).
- **Mesma tecla em outro lugar:** conferir o conflito com `Ctrl+Espaço` e com a seleção por Espaço em caixas.

## 5. Estratégia de teste (por AC, com asserção)
Testes unitários do provedor (escolha por tipo, limites, liberação do arquivo) e de `PreviewPopupService` (ordem: externo antes do embutido, AC-6). Interface (abre em ≤ 300 ms, setas, Esc, foco volta, Espaço segurado abre uma vez) por UI Automation com o helper revisado ou Sandbox; **medições de tempo no host sem enviar teclas** (acionar a ação por comando, não por tecla). Controle negativo: com QuickLook "disponível" (simulado), o embutido **não** abre. `resultado.csv` em `docs/agents/evidence/F044/`.
