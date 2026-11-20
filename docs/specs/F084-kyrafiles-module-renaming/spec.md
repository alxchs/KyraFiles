# F084 — Refatoração de Módulos, Assemblies e Executáveis para KyraFiles

## 1. Visão Geral e Motivação

Com a aprovação da identidade KyraFiles (F081 / F083) para os elementos voltados ao usuário (branding, mascote, ícones do sistema e strings localizadas), o projeto precisa agora realizar a refatoração técnica de **código, namespaces, projetos, assemblies e binários executáveis**, eliminando o nome legado `Files` da infraestrutura interna e dos processos em execução no Windows:

- O executável principal passa a ser `KyraFiles.exe` (e `KyraFiles.dll`).
- Os serviços de atalho e supervisão passam a ser `KyraFiles.HotkeyService.exe` e `KyraFiles.Watchdog.exe`.
- O lançador passa a ser `KyraFiles.App.Launcher.exe`.
- A solução principal passa a ser `KyraFiles.slnx`.
- O protocolo de ativação passa a ser `kyrafiles:`.
- Os projetos principais em `src/` e `tests/` são renomeados de `Files.*` para `KyraFiles.*`.
- A pasta de instalação de Release no usuário passa a ser `%LOCALAPPDATA%\Programs\KyraFiles`.
- A pasta de dados de usuário e configurações é unificada e preservada sem perda de dados pelo `UserSettingsMigrationService`.

---

## 2. Requisitos do Sistema

### 2.1 Requisitos Funcionais (FR)
- **FR-001 (Executável Principal):** O binário do aplicativo principal deve compilar gerando `KyraFiles.exe` e `KyraFiles.dll`.
- **FR-002 (Projetos C# e Solução):** A solução `.slnx` e os projetos sob `src/` e `tests/` devem ser renomeados para o prefixo `KyraFiles.*` (`KyraFiles.App`, `KyraFiles.Shared`, `KyraFiles.Core.Storage`, `KyraFiles.App.Controls`, `KyraFiles.App.Search.Index`, etc.).
- **FR-003 (Serviços Nativos C++):** Os projetos nativos C++ em `src/` devem compilar gerando:
  - `KyraFiles.HotkeyService.exe`
  - `KyraFiles.Watchdog.exe`
  - `KyraFiles.App.Launcher.exe`
- **FR-004 (Protocolo e Alias):** O protocolo de ativação do aplicativo passa a ser `kyrafiles:` (mantendo compatibilidade com `kyrafiles-dev:` se necessário) e o alias de execução passa a ser `kyrafiles.exe`.
- **FR-005 (Supervisão e Hotkey Win+E):** Os mutexes, eventos de encerramento e nomes de processo do interceptador de teclado e watchdog devem ser atualizados para:
  - Mutex do serviço: `Global\KyraFiles_HotkeyService_InstanceMutex`
  - Mutex do watchdog: `Global\KyraFiles_Watchdog_InstanceMutex`
  - Evento de shutdown: `Global\KyraFiles_ShutdownHotkeyService`
- **FR-006 (Preservação de Direitos e Licença):** Todos os cabeçalhos de arquivo mantêm rigorosamente o aviso `// Copyright (c) KyraFiles contributors. Licensed under the MIT License.` conforme regra do projeto.
- **FR-007 (Local de Instalação e Deploy):** O script de deploy e empacotamento deve atualizar a instalação para `%LOCALAPPDATA%\Programs\KyraFiles\`.

### 2.2 Requisitos Não Funcionais (NFR)
- **NFR-001 (Compilação Limpa):** A compilação em Debug e Release via `mkfile` deve terminar com **0 erros**.
- **NFR-002 (Suíte de Testes):** Todos os testes unitários da suíte devem continuar passando (≥ 192 testes com 0 falhas).
- **NFR-003 (Portão F021):** O portão de contenção de recursos `verify_no_index_service.ps1` deve permanecer aprovado com CPU < 5% em repouso.

---

## 3. Cenários de Aceite (AC)

- **AC-1:** Ao compilar em Release, o arquivo de saída em `bin` e na instalação é `KyraFiles.exe` (não mais `KyraFiles.exe`).
- **AC-2:** No Gerenciador de Tarefas do Windows, os processos em execução chamam-se `KyraFiles.exe`, `KyraFiles.HotkeyService.exe` e `KyraFiles.Watchdog.exe`.
- **AC-3:** A suíte de testes unitários roda em `tests/KyraFiles.App.Search.Index.Tests/` e aprova 100% dos testes.
- **AC-4:** O comando `pwsh -Command "Start-Process 'kyrafiles:'"` abre a janela do aplicativo KyraFiles.
- **AC-5:** A instalação atualiza `%LOCALAPPDATA%\Programs\KyraFiles` de forma atômica e limpa.

