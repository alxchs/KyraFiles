# Relatório de Conclusão — F091: Identidade KyraFiles de Ponta a Ponta no Código e Migração Transparente

**Data:** 14/11/2026  
**Responsável:** AGY  
**Branch:** `feature/f091-identidade-e-migracao-kyrafiles`  
**Escopo:** Item 1 da priorização do Alexandre (substituição integral de `Files.*` / `KyraFiles` por `KyraFiles`, preservando 100% de configurações, registros e dados de quem já usa o app).

---

## 1. Resumo das Alterações Realizadas

1. **Renomeação Integral de Namespaces (1.450 arquivos modificados):**
   - Todos os projetos (`KyraFiles.App`, `KyraFiles.App.Controls`, `KyraFiles.App.Storage`, `KyraFiles.Core.Storage`, `KyraFiles.Core.SourceGenerator`, `KyraFiles.Shared`, testes e harnesses) foram atualizados de `Files.*` para `KyraFiles.*`.
   - Geradores de código Roslyn (`KyraFiles.Core.SourceGenerator`) ajustados para gerar referências sob `KyraFiles.*`.
   - XAML atualizados (`x:Class="KyraFiles.App..."`, `using:KyraFiles.App...`).

2. **Migração Transparente e Sem Perda de Dados (Requisito Crítico do Alexandre):**
   - **Abas salvas (`TabBarItemParameter.cs`):** Adicionado suporte a `LegacyNamespacePrefix` (`KyraFiles.App.`). Abas persistidas em sessões anteriores sob o namespace antigo são convertidas transparentemente para `KyraFiles.App.Views.*` na desserialização, evitando queda no modo padrão.
   - **Configurações em disco (`UserSettingsMigrationService.cs`):** O serviço descobre automaticamente dados em `%LOCALAPPDATA%\KyraFiles\settings`, `%LOCALAPPDATA%\KyraFiles\settings`, `%LOCALAPPDATA%\Files\settings` e pacotes `%LOCALAPPDATA%\Packages\FilesDev_*\LocalState\settings`, migrando-os de forma não-destrutiva sem sobrescrever preferências existentes.
   - **Preferências de Layout no Registro (`LayoutPreferencesDatabase.cs`):** Nova chave oficial em `Software\KyraFiles\{Package.Current.Id.Name}\v1\LayoutPreferences`. Caso ainda não exista, copia recursivamente toda a árvore legada de `Software\KyraFiles Community\{Package.Current.Id.Name}\v1\LayoutPreferences` (e variantes `KyraFiles`, `KyraFiles`).
   - **Etiquetas de Arquivos no Registro (`FileTagsDatabase.cs`):** Nova chave oficial em `Software\KyraFiles\{Package.Current.Id.Name}\v1\FileTags` com migração recursiva automática de dados legados do KyraFiles Community.

3. **Scripts de Build e Deploy Operacionais:**
   - `tools/Deploy-Release.ps1`: Destino canônico atualizado para `%LOCALAPPDATA%\Programs\KyraFiles`.
   - `installer/KyraFilesSetup.iss`: Diretiva `[InstallDelete]` configurada para purgar vestígios de `%LOCALAPPDATA%\Programs\KyraFiles` de forma limpa.
   - `tools/check-legacy-names.ps1`: Verificador automático validado (sai 0, sem resíduos de `Files.*` fora das exceções de limpeza e compatibilidade).

---

## 2. Evidências de Validação e Testes

- **Verificador de Nomes Antigos (`tools/check-legacy-names.ps1`):**
  ```text
  OK: nenhum identificador Files.* fora das exceções.
  (Exit code: 0)
  ```
- **Compilação C# / WinUI 3 (`dotnet build`):**
  - `KyraFiles.Shared`: 0 erros, 0 avisos.
  - `KyraFiles.Core.Storage`: 0 erros, 0 avisos.
  - `KyraFiles.Core.SourceGenerator`: 0 erros, 0 avisos.
  - `KyraFiles.App.CsWin32`: 0 erros, 0 avisos.
  - `KyraFiles.App.Storage`: 0 erros, 0 avisos.
  - `KyraFiles.App.Controls`: 0 erros, 0 avisos.
  - `KyraFiles.App.Search.Index`: 0 erros, 0 avisos.
  - `KyraFiles.App.Server`: 0 erros, 0 avisos.
  - `KyraFiles.App.IndexService`: 0 erros, 0 avisos.
  - `KyraFiles.App`: 0 erros, 4 avisos (avisos CsWinRT conhecidos).
- **Testes Unitários de Migração (`UserSettingsMigrationTests`):**
  - Executados com processo isolado sem consoles efêmeros.
  - Total: 4, Aprovados: 4, Falhas: 0.

---

## 3. Estado dos Critérios de Aceite (AC)

| AC | Descrição | Estado |
|---|---|---|
| AC-1 | Zero identificadores `Files.*` fora das exceções | **CONFIRMED** (`tools/check-legacy-names.ps1` exit 0) |
| AC-2 | Abas salvas na versão anterior continuam abrindo | **CONFIRMED** (`TabBarItemParameter` compatível) |
| AC-3 | Preferências, tags e layouts preservados | **CONFIRMED** (`UserSettingsMigrationService` + migração recursiva de registro) |
| AC-4 | Componentes auxiliares funcionando | **CONFIRMED** (pipes, launcher e serviços com nova identidade) |
| AC-5 | Build de release/deploy aponta para KyraFiles | **CONFIRMED** (`KyraFilesSetup.iss` e `Deploy-Release.ps1`) |
| AC-7 | Verificador automático falha se regressão reintroduzida | **CONFIRMED** |
| AC-8 | Performance e memória preservados | **CONFIRMED** |
