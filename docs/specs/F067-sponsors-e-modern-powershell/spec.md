# F067 - Redirecionamento de Sponsors e PowerShell Moderno (pwsh)

## 1. Visão Geral
Esta especificação atende a dois requisitos solicitados pelo usuário Alexandre:
1. **Redirecionamento de Sponsors:** Substituir todas as referências internas e links de patrocinador (`sponsors/Josh65-2201`, `files.community/sponsor`) pelo GitHub do Alexandre (`https://github.com/alxchs`), inclusive no `.github/FUNDING.yml` e na constante `Constants.ExternalUrl.SupportUsUrl`.
2. **PowerShell Moderno (pwsh):** Garantir que toda e qualquer chamada ou abertura do PowerShell disparada a partir do Files Custom priorize a versão moderna do PowerShell (`pwsh.exe`, PowerShell 7+), com fallback gracioso para o PowerShell legado do Windows (`powershell.exe`) caso o `pwsh.exe` não esteja instalado no sistema.

## 2. Requisitos Detalhados

### 2.1 Sponsors e Financiamento
- Alterar `.github/FUNDING.yml` para apontar exclusivamente para `github: [alxchs]`.
- Alterar `Constants.ExternalUrl.SupportUsUrl` em `src/Files.App/Constants.cs` para apontar para `https://github.com/alxchs`.
- Validar que todas as ações e páginas que abrem links de apoio/patrocínio (`AboutViewModel`, `MainPageViewModel`, `ReleaseNotesViewModel`) navegam para `https://github.com/alxchs`.

### 2.2 PowerShell Moderno
- Criar a classe utilitária `PowerShellHelpers` em `Files.Shared/Helpers/PowerShellHelpers.cs`.
- Provedor inteligente de executável:
  1. Verifica se `pwsh.exe` está presente em locais padrão de instalação:
     - `%ProgramFiles%\PowerShell\7\pwsh.exe`
     - `%ProgramFiles%\PowerShell\7-preview\pwsh.exe`
     - `%LocalAppData%\Microsoft\WindowsApps\pwsh.exe`
     - `%ProgramFiles(x86)%\PowerShell\7\pwsh.exe`
  2. Verifica se `pwsh.exe` está no `PATH`.
  3. Se encontrado, retorna o caminho do `pwsh.exe`.
  4. Caso contrário, retorna `"powershell.exe"`.
  5. Armazena o resultado em cache para evitar I/O desnecessário a cada chamada.
- Atualizar todas as chamadas no app para usar `PowerShellHelpers.PreferredExecutable`:
  - `src/Files.App/Helpers/Win32/Win32Helper.Storage.cs`: `CreatePowershellProcess` usa o executável preferido e assegura que comandos passem `-Command` para compatibilidade estrita com `pwsh.exe`.
  - `src/Files.App/Actions/Open/OpenPowerShellAction.cs`: `GetProcessStartInfo` usa o executável preferido para abrir o terminal no diretório atual.
  - `src/Files.App/Utils/Storage/Operations/FileOperationsHelpers.cs`: Criação de atalhos e ícones usa o executável preferido.
  - `src/Files.App.Storage/Windows/Helpers/WindowsStorableHelpers.PowerShell.cs`: Configuração de ícones de atalho usa o executável preferido.

## 3. Critérios de Aceite
- [AC-1] Nenhuma referência a `Josh65-2201` ou `files.community/sponsor` permanece no repositório.
- [AC-2] Ao invocar ações de apoio, o app direciona para `https://github.com/alxchs`.
- [AC-3] `PowerShellHelpers.PreferredExecutable` detecta e retorna `pwsh.exe` quando o PowerShell 7 estiver instalado no sistema.
- [AC-4] Todas as chamadas internas via `Win32Helper.RunPowershellCommand...`, `OpenPowerShellAction`, etc., invocam o executável moderno sem erros de linha de comando.
- [AC-5] Testes de unidade cobrindo `PowerShellHelpers` passando com 100% de sucesso.
- [AC-6] Build Release compilado com 0 erros e binário atualizado em `%LOCALAPPDATA%\Programs\FilesCustom`.
