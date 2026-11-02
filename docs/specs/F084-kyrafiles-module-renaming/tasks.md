# F084 — Tarefas de Execução: Refatoração de Módulos e Executáveis para KyraFiles

| Task | Descrição | Requisitos | Status |
| :--- | :--- | :--- | :--- |
| **T1** | Configurar `AssemblyName` e `TargetName` como `KyraFiles` no app principal e `AppxManifest.xml` | FR-001, FR-004 | CONCLUÍDO |
| **T2** | Atualizar serviços nativos C++ (`KyraFiles.HotkeyService`, `KyraFiles.Watchdog`, `KyraFiles.App.Launcher`) | FR-003, FR-005 | CONCLUÍDO |
| **T3** | Renomear arquivos de projeto `.csproj`, `.vcxproj` e criar `KyraFiles.slnx` | FR-002 | CONCLUÍDO |
| **T4** | Atualizar scripts de build, testes e ferramenta de deploy (`Deploy-Release.ps1`), gerando ícones KyraPet | FR-007 | CONCLUÍDO |
| **T5** | Compilar Release (`mkfile r`), rodar testes unitários e validar gates de qualidade | NFR-001, NFR-002, NFR-003 | CONCLUÍDO |
| **T6** | Produzir relatório formal de conclusão `RELATORIO.md` e integrar na branch `colab` | AC-1 a AC-5 | CONCLUÍDO |
