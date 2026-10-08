# Build 2026-10-07 22:50 — F010 Todas as Colunas do Explorer + F001 a F011 Integrados

- **Executável:** `src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\Files.exe`, aberto por `.\Open-FilesDev.ps1 -Configuration Release` (pacote `FilesDev` 4.2.37.0, x64, Release).
- **Código:** commit `ac1444277` na branch `feature/all-explorer-columns`.
- **Build:** `mkfile r` / `dotnet publish src/Files.App/Files.App.csproj -c Release -p:Platform=x64` = **0 Warning(s)**, **0 Error(s)** (CONFIRMED).
- **Spec:** `docs/specs/F010-all-explorer-columns/` (Fase 1 e Fase 2 concluídas com validação empírica).

---

## O que foi liberado neste executável

### 1. F010 — Todas as Colunas do Windows Explorer (Arquitetura Híbrida)
| Item | O que muda para o usuário | Estado |
|---|---|---|
| Menu "Mais..." no Cabeçalho | Clique com botão direito em qualquer cabeçalho de coluna no modo Detalhes exibe a nova opção **"Mais..."** / **"More..."**. | **CONFIRMED** |
| Diálogo Seletor Completo | Abre o `ChooseDetailsDialog` com 369 propriedades elegíveis (`PDEF_COLUMN`) do Windows Property System catalogadas, com busca em tempo real, agrupamento por categorias (Imagens, Mídia, Documentos, Sistema, etc.) e contadores. | **CONFIRMED** |
| Inserção Dinâmica no Grid | Ao marcar qualquer propriedade (ex: `Dimensões`) e clicar em OK, a nova coluna surge instantaneamente à direita das colunas padrão, com splitter redimensionável próprio. | **CONFIRMED** |
| Extração Assíncrona via Shell | As células extraem valores nativos em segundo plano (`IShellItem2` / `IPropertyStore`) formatados exatamente como no Explorer do Windows, sem travar a rolagem da lista. | **CONFIRMED** |
| Persistência Híbrida por Pasta | As colunas dinâmicas ativas e suas larguras em pixels são gravadas no Registro do Windows por caminho de pasta (`LayoutPreferencesDatabase`), preservando-se após navegações e reinicializações. | **CONFIRMED** |

### 2. Conjunto Completo de Features OneCommander Já Integradas
| Feature | Descrição | Estado |
|---|---|---|
| **F001 — Rename UX** | `F2` seleciona apenas o nome do arquivo; extensão blindada contra apagamento acidental; `Tab`/`Shift+Tab` alterna entre nome e extensão; suporte a dotfiles e diálogo de segurança para `.exe`. | **CONFIRMED** |
| **F002 — Colunas Independentes** | Arrastar a borda de uma coluna altera somente a coluna manipulada em pixels absolutos, sem empurrar ou achatar colunas vizinhas. | **CONFIRMED** |
| **F003 — Nova Aba vs Duplicar** | Botão `+` e `Ctrl+T` abrem a pasta padrão inicial; `Ctrl+Shift+T` ou menu da aba duplica a pasta da aba atual. | **CONFIRMED** |
| **F004 — Barra de Menus Tradicional** | Menu clássico completo (Arquivo, Editar, Exibir, etc.) com aceleradores de teclado `Alt+F`, `Alt+E` e toggle em *Configurações > Aparência*. | **CONFIRMED** |
| **F005 — Atalhos Preview / Details** | `Alt+P` abre/fecha diretamente em Preview; `Alt+Shift+P` abre/fecha diretamente em Details; foco devolvido à lista ao fechar. | **CONFIRMED** |
| **F006 / F011 — Motores de Busca Plugáveis** | Escolha em *Configurações > Pastas* entre busca Nativa, Agent Ransack (CLI silencioso) e Everything (IPC), com fallback automático. | **CONFIRMED** |
| **F007-B — Compositor Idle** | Otimizações de renderização e animações reduzindo uso de GPU/CPU quando o app está em segundo plano ou ocioso. | **CONFIRMED** |
| **F008 — Modo Compacto** | Densidade ultracompacta inspirada no OneCommander disponível em *Configurações > Aparência*. | **CONFIRMED** |
| **F009 — Fontes Separadas** | Configuração independente de tipografia e tamanho de fontes para barra de navegação, listas e menus. | **CONFIRMED** |

---

## Como Testar a F010 no App

1. Abra o aplicativo pelo script oficial:
   ```powershell
   .\Open-FilesDev.ps1 -Configuration Release
   ```
2. No modo de visualização **Detalhes** (caso esteja em grade, pressione `Ctrl+Shift+6` ou selecione Detalhes no canto inferior direito):
   - Clique com o botão direito do mouse sobre qualquer cabeçalho de coluna (ex.: sobre "Nome" ou "Data de modificação").
   - No menu que abrir, clique na última opção: **"Mais..."** (ou **"More..."**).
3. No diálogo **"Escolher Detalhes"** que abrir:
   - Digite `dim` no campo de busca para filtrar instantaneamente por "Dimensões".
   - Marque a caixa de seleção ao lado de **"Dimensões"**.
   - Clique no botão **OK**.
4. Observe a coluna **"Dimensões"** adicionada ao cabeçalho.
5. Navegue até qualquer pasta contendo imagens (por exemplo, `c:\desenv\utils\FilesApp\docs\releases\evidence`):
   - As dimensões de cada imagem serão lidas em segundo plano e exibidas na coluna (ex.: `2571 x 1686`, `2560 x 1440`).
6. Navegue para outra pasta e volte: a coluna permanece configurada e visível.

---

## Evidências Empíricas Registradas

- [`f010_added_column.png`](file:///c:/desenv/utils/FilesApp/docs/releases/evidence/f010_added_column.png) — Adição dinâmica da coluna no cabeçalho do `DetailsLayoutPage`.
- [`f010_dimensions_populated.png`](file:///c:/desenv/utils/FilesApp/docs/releases/evidence/f010_dimensions_populated.png) — Valores reais de resolução extraídos e exibidos nas linhas de arquivos de imagem.

---

## Status de Compilação e Qualidade

- **Compilador**: .NET 10.0.301 / Windows App SDK 2.5.1
- **Resultado Release**: `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Resultado Debug**: `Build succeeded. 0 Warning(s), 0 Error(s)`
