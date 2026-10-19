# Registro de Artefatos de Teste — F024

Este arquivo registra OBRIGATORIAMENTE cada pasta, arquivo temporário, script ou binário auxiliar criado durante a bateria de testes de performance da F024, para garantir remoção cuidadosa e 100% comprovada ao final da tarefa, evitando qualquer poluição do ambiente ou confusão de arquivos.

## Tabela de Artefatos de Teste

| ID | Caminho Absoluto | Propósito / Descrição | Data Criação | Status | Comando de Remoção |
|---|---|---|---|---|---|
| ART-001 | `C:\FilesUXLab\perf\1600` | Pasta de teste com 1.600 arquivos reais | 2026-10-19 | Ativo | `Remove-Item -Recurse -Force C:\FilesUXLab\perf\1600` |
| ART-002 | `C:\desenv\utils\FilesApp\tools\perf\setup_1600_lab.ps1` | Script gerador de 1.600 arquivos | 2026-10-19 | Ativo | `Remove-Item -Force tools\perf\setup_1600_lab.ps1` |
| ART-003 | `C:\desenv\utils\FilesApp\tools\perf\f024_keyboard_nav_test.ps1` | Script automatizado de teste de teclado rápido | 2026-10-19 | Ativo | `Remove-Item -Force tools\perf\f024_keyboard_nav_test.ps1` |
| ART-004 | `C:\desenv\utils\FilesApp\tools\perf\f024_watcher_sync_test.ps1` | Script automatizado de teste do Watcher sob concorrência | 2026-10-19 | Ativo | `Remove-Item -Force tools\perf\f024_watcher_sync_test.ps1` |
| ART-005 | `C:\desenv\utils\FilesApp\tools\perf\cleanup_f024_lab.ps1` | Script mestre de limpeza de todos os artefatos | 2026-10-19 | Ativo | `Remove-Item -Force tools\perf\cleanup_f024_lab.ps1` |
| ART-006 | `%LOCALAPPDATA%\Programs\FilesCustom_v4.3.0_backup` | Cópia de segurança permanente do executável funcional v4.3.0 | 2026-10-19 | Preservado (Backup funcional) | Manter como garantia |

---

## Regras de Higiene Estrita (Alexandre)
1. Nenhum arquivo de teste pode ser deixado em `%TEMP%`, `C:\`, Desktop ou no repositório após o encerramento da fase.
2. Todo script descartável deve ser executado, validado e limpo ao final.
3. A remoção deve ser confirmada por verificação exaustiva (`Test-Path`).
