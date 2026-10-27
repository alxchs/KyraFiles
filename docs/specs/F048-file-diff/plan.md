# F048 — Comparar arquivos e pastas (diff) — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026 (ponto em aberto decidido: só delegar a ferramenta externa para arquivos). **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** padrão de "ferramenta externa configurável": `IDevToolsSettingsService` (`IDEPath`, `IDEName`) e `OpenInIDEAction`, que monta o comando com `Win32Helper.ToPowerShellStringLiteral` e chama `Win32Helper.RunPowershellCommandAsync(..., PowerShellExecutionOptions.Hidden)` (devolve `bool`); em falha mostra `DynamicDialogFactory.ShowFor_IDEErrorDialog`.
- **CONFIRMED** a página de configuração `DevToolsPage` + `DevToolsViewModel` existe.
- **CONFIRMED** ações de seleção usam `IContentPageContext.SelectedItems`/`HasSelection` e se registram com `[GeneratedRichCommand]` (ex.: `BaseCopyMoveToFolderAction`); o menu de contexto é montado em `ContentPageContextFlyoutFactory`.
- **INFERRED** o conteúdo da área de transferência com um caminho copiado pode ser lido por `Clipboard.GetContent()`; T0 confirma o formato que "Copiar" do app deixa.
- **CONFIRMED** não existe nenhuma ação de comparar (busca por `DiffTool|CompareFiles|WinMerge` em `src/`: 0 ocorrências).

## 2. Abordagem
1. **Configuração de ferramentas** (FR-001): lista de `{nome, comando, argumentos com {esquerda} e {direita}}` ao lado de `IDEPath`, na `DevToolsPage`. **Detecção automática** (FR-002) das instaladas: WinMerge (registro e caminhos usuais), Beyond Compare, `code` no PATH (`code --diff {esquerda} {direita}`), Meld; mostrar as achadas como sugestão, nunca executar na detecção.
2. **`CompareItemsAction`:** habilitada com exatamente **2 itens** selecionados, ou **1 item + um caminho na área de transferência** ("Comparar com o copiado", AC-2); com 3 ou mais, desabilitada com dica (AC-3). Os dois caminhos vão como **lista de argumentos** (`ProcessStartInfo.ArgumentList`), não por texto montado: resolve espaços e acentos (AC-7) e é mais seguro que o PowerShell textual.
3. **Binário (AC-6):** detectar (byte nulo nos primeiros KB); avisar e oferecer comparação por tamanho e resumo criptográfico, em vez de abrir a ferramenta de texto.
4. **Ferramenta ausente (AC-5):** mensagem clara com opção de escolher outra; nenhuma exceção.
5. **Comparar pastas (AC-4):** diálogo próprio simples com a lista "só à esquerda / só à direita / diferentes / iguais" (tamanho e data; conteúdo opcional); duplo clique abre o diff do par pela ferramenta escolhida. O **motor de comparação é compartilhado com a F041** (`FolderComparer`, serviço puro): quem implementar primeiro deixa a interface estável e a outra reaproveita.
Opções descartadas: (a) tela de diff embutida (decidido: fora desta versão); (b) usar `RunPowershellCommandAsync` com texto montado (mais frágil com caminhos estranhos).

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001, FR-002 | `IDevToolsSettingsService` (novos campos/lista) + `DevToolsPage`/`DevToolsViewModel` + detector de ferramentas |
| FR-003 (embutida simples) | **adiado** (decisão da spec: só delegar); registrar como não feito |
| AC-1, AC-2, AC-3, AC-5, AC-7 | `CompareItemsAction` + item no menu de contexto |
| AC-4 | `FolderComparer` + diálogo |
| AC-6 | detecção de binário |

**Arquivos a mexer:** novos `CompareItemsAction`, `ToolDetector`, `FolderComparer`, diálogo de pastas; `IDevToolsSettingsService`/serviço; `DevToolsPage`/`DevToolsViewModel`; `ContentPageContextFlyoutFactory` (um item); `Strings/en-US`. **A NÃO mexer:** `OpenInIDEAction`, `RunPowershellCommandAsync`, a F033.

## 4. Riscos
- **Argumentos com aspas** e caminhos com `'`/`$`/acentos: `ArgumentList` e teste adversarial.
- **Ferramenta que não termina** ou pede interação: não esperar o fim do processo; iniciar e soltar.
- **Pastas enormes** na comparação: varredura em segundo plano, cancelável, memória estável (sem listar tudo de uma vez).
- **Duplicação com a F041:** definir o contrato do `FolderComparer` cedo para não implementar duas vezes.

## 5. Estratégia de teste (por AC, com asserção)
Uma "ferramenta de teste" (executável pequeno que só grava os argumentos recebidos num arquivo) faz o papel de WinMerge/VS Code: assim os testes **não abrem nenhum programa real**. Testes de integração: AC-1, AC-2, AC-3 (estado habilitado), AC-5, AC-6 e AC-7 (caminhos com espaços, acentos, `'`, `$`) conferindo o arquivo gravado; `FolderComparer` por tabela (AC-4) em árvore de teste em `%TEMP%\agy\<GUID>\...`. Interface (menu e diálogo) por UI Automation com o helper revisado ou Sandbox. Controle negativo: 3 itens selecionados **não** habilitam. `resultado.csv` em `docs/agents/evidence/F048/`.
