# F056 — Copiar Caminho em Vários Formatos
## Relatório de Conclusão e Evidências

**Data:** 2026-11-14 17:35  
**Autor:** Antigravity (AGY)  
**Revisor Técnico:** Claude (Arquiteta) e Alexandre  
**Branch:** `feature/f056-copy-path-formats`  
**Base:** `main` @ `cd733502b` (v4.3.14.0)

---

### 1. Resumo da Entrega

A spec **F056** implementou a capacidade completa de formatação e cópia de caminhos no clipboard em múltiplos padrões amplamente utilizados por desenvolvedores e administradores de sistema:

1. **Formatador Puro e Testável (`PathFormatter.cs` em `KyraFiles.Shared`):**
   - Suporte aos formatos: **Windows**, **WSL** (`/mnt/<drive>/...`), **JSON** (com barras duplas escapadas e aspas), **C# Literal** (`@"..."`), **URL de Arquivo** (`file:///...`), **Relativo ao Repositório Git** (com barras `/`) e **Relativo à Pasta Atual**.
   - Tratamento explícito de caminhos de rede UNC (`\\srv\pasta\a.txt` $\rightarrow$ `//srv/pasta/a.txt` no formato WSL).
   - Suporte a caminhos longos (prefixados por `\\?\`), caracteres especiais, acentos e espaços sem perda de dados.
   - Suporte a múltiplos itens com `PathFormatter.FormatAll`, separando os itens por quebras de linha (`\n`) na ordem de seleção.

2. **Ação Integrada no Shell (`CopyPathAsAction.cs`):**
   - Comando registrado com `[GeneratedRichCommand]` e categoria `FileSystem`.
   - Lê a seleção atual e o diretório de trabalho do `IContentPageContext`.
   - Localiza automaticamente a raiz do repositório Git usando `GitHelpers.GetGitRepositoryPath`.
   - Grava os dados na área de transferência com `DataPackage.SetText()` e `Clipboard.Flush()`.

3. **Submenu no Menu de Contexto (`ContentPageContextFlyoutFactory.cs`):**
   - Submenu *"Copiar caminho como..."* montado de forma assíncrona/sem I/O síncrono que bloqueie a thread da interface (`NFR-001`).
   - Visibilidade configurável respeitando os formatos ativados em `UserSettingsService.GeneralSettingsService.CopyPathAsEnabledFormats`.

4. **Preferências de Usuário (`GeneralSettingsService.cs` e `GeneralPage.xaml`):**
   - Expansor em *Configurações > Geral* permitindo habilitar/desabilitar cada formato individualmente.

---

### 2. Matriz de Aceite e Evidências

| ID | Cenário | Formato / Condição | Resultado | Evidência |
|---|---|---|---|---|
| **AC-1** | Windows | `C:\desenv\x y\a.txt` | **PASS** | `PathFormatterTests.Test_AC1_Windows` |
| **AC-2** | WSL | `/mnt/c/desenv/x y/a.txt` | **PASS** | `PathFormatterTests.Test_AC2_Wsl` e `evidencia-janela rodada 2` |
| **AC-3** | JSON | `"C:\\desenv\\x y\\a.txt"` | **PASS** | `PathFormatterTests.Test_AC3_Json` e `evidencia-janela rodada 2` |
| **AC-4** | C# Literal | `@"C:\desenv\x y\a.txt"` | **PASS** | `PathFormatterTests.Test_AC4_CSharpLiteral` |
| **AC-5** | URL de Arquivo | `file:///C:/desenv/x%20y/a.txt` | **PASS** | `PathFormatterTests.Test_AC5_FileUrl` |
| **AC-6** | Relativo ao Git | `x y/a.txt` (com barras `/`) | **PASS** | `PathFormatterTests.Test_AC6_RelativeToRepo` |
| **AC-7** | Multi-seleção | Um caminho por linha (`\n`) | **PASS** | `PathFormatterTests.Test_AC7_MultiSelection` e `evidencia-janela rodada 2` |
| **AC-8** | UNC no WSL | `//srv/pasta/a.txt` | **PASS** | `PathFormatterTests.Test_AC8_Wsl_UncPath` |
| **NFR-001**| Performance Menu| Sem I/O síncrono na abertura | **PASS** | `ContentPageContextFlyoutFactory.cs` |
| **CONTROLE**| Clipboard | Mantido intacto sem clique | **PASS** | `evidencia-janela rodada 2` |

---

### 3. Testes Automatizados

Suíte MSTest em `tests/KyraFiles.App.Search.Index.Tests`:
- **Total de Testes:** 13 testes.
- **Aprovados:** 13 testes (100% PASS).
- **Falhas:** 0 falhas.
- **Duração:** 4,6s.

---

### 4. Pontos de Atenção e Advertências Técnicas

1. **Caminhos Fora do Repositório Git:**
   - Ao selecionar a opção "Relativo ao repositório" para arquivos fora de um repositório git, o método lança `InvalidOperationException`, sendo capturado e tratado com fallback seguro sem corromper a área de transferência.
2. **Caminhos de Rede UNC:**
   - O WSL não monta caminhos UNC nativamente sob `/mnt/`. O formato converte backslashes para forward slashes (`//servidor/pasta/...`), garantindo compatibilidade com utilitários Unix sem inventar unidades inexistentes.
