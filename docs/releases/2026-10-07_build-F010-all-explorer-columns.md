# Nota de Liberação: F010 — Todas as Colunas do Windows Explorer (Arquitetura Híbrida)

Data: 2026-10-07  
Branch: `feature/all-explorer-columns`  
Responsável: Antigravity  

---

## 1. Escopo e Objetivo

Implementar a feature **F010 — Todas as Colunas do Windows Explorer** (`MASTER_SPEC.md` §10 e §46, `docs/specs/F010-all-explorer-columns/`):
- Permitir que qualquer coluna elegível do Windows Explorer (`PDEF_COLUMN`, total de 369 propriedades catalogadas) seja exibida no layout Detalhes (`DetailsLayoutPage`).
- Arquitetura Híbrida: preserva as 16 colunas padrão com alta performance tipada (`ColumnsViewModel`) e estende com colunas dinâmicas em `DynamicColumnDefinition` e armazenamento esparso `DynamicColumnValue` em `ListedItem`.
- Extração de metadados em segundo plano (`ShellViewModel.LoadExtendedItemPropertiesAsync`) via `IShellItem2.GetPropertyDescriptionList` e `IPropertyStore` com formatação nativa do Shell do Windows.
- Diálogo de seleção de colunas completo e moderno ("Mais..." / `ChooseDetailsDialog`) acessível pelo menu de contexto do cabeçalho de colunas, com busca em tempo real, agrupamento por categorias e suporte a temas (Claro/Escuro).
- Persistência híbrida por diretório no Registro do Windows (`LayoutPreferencesDatabase`) e global (`LayoutPreferencesItem`).
- Compilação Debug e Release x64 estritamente limpa com **0 Warning(s), 0 Error(s)**.
- Validação empírica no aplicativo real com imagens comprovando renderização de cabeçalhos e valores reais de dimensões (`2571 x 1686`, `2560 x 1440`).

---

## 2. Detalhes Técnicos da Implementação

| Componente | Arquivo / Descrição |
|---|---|
| **Modelo de Dados Dinâmico** | `src/Files.App/Data/Models/DynamicColumnDefinition.cs`: definição de coluna com `CanonicalName`, `DisplayName`, `Category`, `UserLength`, `TextAlignment` e anotações WinRT/JSON AOT-safe. |
| **Armazenamento de Linha** | `src/Files.App/Data/Models/DynamicColumnValue.cs` e `src/Files.App/Data/Items/ListedItem.cs`: propriedades esparsas em dicionário indexado por nome canônico com coleção observável `DynamicColumnValues`. |
| **Catálogo de Propriedades** | `src/Files.App/Data/Contracts/IExplorerPropertyService.cs` e `src/Files.App/Services/ExplorerPropertyService.cs`: catálogo de 369 propriedades `PDEF_COLUMN` e extração de valores formatados via `ShellItem`. Registrado no container DI. |
| **Extração em Segundo Plano** | `src/Files.App/ViewModels/ShellViewModel.cs`: resolução assíncrona de metadados estendidos para colunas dinâmicas ativas em thread em segundo plano antes da atualização da UI. |
| **Interface do Usuário (XAML)** | `src/Files.App/Views/Layouts/DetailsLayoutPage.xaml` e `.xaml.cs`: item de menu "Mais..." (`MoreColumnsMenuFlyoutItem`), `ItemsControl` para cabeçalhos dinâmicos com splitters redimensionáveis e `ItemsControl` no item de linha. |
| **Diálogo Seletor** | `src/Files.App/Dialogs/ChooseDetailsDialog.xaml`, `.xaml.cs` e `ChooseDetailsDialogViewModel.cs`: interface com busca em tempo real, contagem e checkboxes reativos. |
| **Localização** | `src/Files.App/Strings/en-US/Resources.resw` e `pt-BR/Resources.resw`: strings localizadas para "Mais...", títulos e busca do diálogo. |
| **Persistência** | `src/Files.App/Helpers/Layout/LayoutPreferencesItem.cs` e `LayoutPreferencesDatabase.cs`: serialização JSON em `CustomColumnsJson` salva por pasta no Registro. |

---

## 3. Evidências Empíricas de Funcionamento

1. **Adição Dinâmica de Coluna**:
   - Menu de contexto acionado sobre o cabeçalho no modo Detalhes.
   - Opção "Mais..." selecionada, abrindo o `ChooseDetailsDialog`.
   - Coluna `Dimensões` selecionada e confirmada.
   - Cabeçalho `Dimensoes` inserido dinamicamente no `HeaderGrid`.
   - Evidência gravada: `docs/releases/evidence/f010_added_column.png`.

2. **População Real de Metadados e Navegação**:
   - Navegação realizada para pasta com imagens (`docs\releases\evidence`).
   - Células da coluna `Dimensões` populadas com valores reais das imagens: `2571 x 1686`, `2560 x 1440`.
   - Evidência gravada: `docs/releases/evidence/f010_dimensions_populated.png`.

---

## 4. Status de Compilação

- **Build Debug (`mkfile d`)**: `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Build Release (`mkfile r`)**: `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Deploy**: Registrado e testado no pacote Release `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`.
