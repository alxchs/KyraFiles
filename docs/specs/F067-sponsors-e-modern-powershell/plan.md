# Plano de Implementação - F067

## Arquitetura e Decisões Técnicas

1. **Localização de `PowerShellHelpers`**:
   - `Files.Shared` é o projeto compartilhado central referenciado por `Files.App`, `Files.App.Storage` e os projetos de teste.
   - Definiremos `Files.Shared.Helpers.PowerShellHelpers` com a propriedade estática `PreferredExecutable`.
   - Adicionaremos resolução com cache, buscando primeiro diretórios comuns conhecidos e o `PATH`.
   - Em caso de exceção ou ausência, fallback para `"powershell.exe"`.

2. **Garantia de sintaxe de execução (`-Command`)**:
   - No `powershell.exe` clássico (5.1), parâmetros sem flag podiam ser interpretados como comando.
   - No `pwsh.exe` (7+), argumentos sem switch são interpretados por padrão como `-File`, falhando se receberem código inline (ex: `& 'notepad.exe' ...`).
   - Portanto, em `Win32Helper.Storage.cs:CreatePowershellProcess`, se o comando a ser executado não começar com `-` (como `-command`, `-c`, `-EncodedCommand` ou `-NoProfile`), o prefixo `-NoProfile -Command ` será adicionado automaticamente, garantindo compatibilidade universal e sem sobrecarga de perfil.

3. **Substituição de URLs de Patrocínio**:
   - `.github/FUNDING.yml`: `github: [alxchs]`
   - `src/Files.App/Constants.cs`: `SupportUsUrl = @"https://github.com/alxchs";`

4. **Testes e Validação**:
   - Teste unitário em `tests/Files.App.Search.Index.Tests/PowerShellHelpersTests.cs` verificando a detecção e execução de `PreferredExecutable`.
   - Execução do conjunto completo de testes (159+ testes).
   - Verificação F021 (`verify_no_index_service.ps1`).
   - Compilação do pacote Release e substituição no diretório de instalação local.
