# F091 — Identidade KyraFiles de ponta a ponta no código — TASKS

Status: **CONCLUÍDA em 14/11/2026** por pedido do Alexandre (Item 1 da priorização).
Branch: `feature/f091-identidade-e-migracao-kyrafiles`, saindo da `main` atualizada.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| **T0** | Inventário final: reexecutar os comandos do plan §1; listar `Type.GetType`, `DynamicDependency` com texto, `FullName`/nome de tipo gravado em JSON ou registro; fechar a lista de exceções | FR-002, FR-005 | contagens fechadas e exceções justificadas | concluído |
| **T1** | Verificador `tools/check-legacy-names.ps1` (lookbehind, lista de exceções, sai 1 se achar) | FR-006, AC-7 | sai 0 na verificação oficial | concluído |
| **T2** | Corrigir defeitos de nome já existentes: `Create-SelfContained-Release.ps1`, `BuildInfo.cs`, `launchSettings.json`, comentário de `Build-Native.ps1` | FR-004, AC-5 | release gerada sem erro; `BuildInfo` acha o arquivo certo | concluído |
| **T3** | Compatibilidade de leitura das abas salvas (aceita nome antigo e novo, grava o novo) | FR-002, AC-2 | `TabBarItemParameter.cs` aceita `Files.App.` e faz mapeamento para `KyraFiles.App.` | concluído |
| **T4** | Renome `KyraFiles.Shared` + strings dos geradores (mesmo commit) | FR-001 | build verde; contagem de comandos gerados preservada | concluído |
| **T5** | Renome `Core.Storage`, `Core.SourceGenerator`, `App.Storage`, `App.Search.Index` | FR-001 | build verde | concluído |
| **T6** | Renome `App.Controls` (cs + XAML) | FR-001 | build verde; controles funcionando | concluído |
| **T7** | Renome `KyraFiles.App` (cs, XAML `x:Class`/`using:`) e `IndexService` | FR-001, AC-4 | build verde do projeto principal | concluído |
| **T8** | Renome dos projetos de teste | FR-001 | testes compilando e passando (ex: `UserSettingsMigrationTests`) | concluído |
| **T9** | Migração transparente de dados, preferências de layout e tags de registro | FR-002, AC-3 | `UserSettingsMigrationService`, `LayoutPreferencesDatabase` e `FileTagsDatabase` com migração transparente de chave de registro e pastas | concluído |
| **T10** | Validação: verificador, AC-1 a AC-8, build C# WinUI 3 com 0 erros, `RELATORIO.md` | AC-1 a AC-8 | relatório com estado por AC e comprovação técnica | concluído |
