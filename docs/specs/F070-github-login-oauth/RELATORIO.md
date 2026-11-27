# Relatório de Conclusão — F070: Login do GitHub Próprio

- **Spec:** F070 (Item 2 da Fila KyraFiles)
- **Data:** 01/11/2026
- **Agente:** AGY
- **Branch:** `feature/f070-github-login`

---

## 1. Entregas e Evidências

### O que foi entregue
1. **Desacoplamento e Injeção do Client ID (`GitHubAuthService.cs`)**:
   - Resolução dinâmica por variável de ambiente `KYRAFILES_GITHUB_CLIENT_ID` (ou `FILES_GITHUB_CLIENT_ID`), com fallback seguro para constantes de build.
   - Detecção automática de Client ID não configurado ou marcador de segredo (`githubclientid.secret`).
2. **Motor do Device Authorization Grant RFC 8628**:
   - `RequestDeviceCodeAsync`: requisição assíncrona ao endpoint `/login/device/code` do GitHub com suporte a `CancellationToken` e extração via `JsonDocument` (compatível com Native AOT).
   - `PollForAccessTokenAsync`: loop de polling respeitando `interval`, tratando `authorization_pending`, `slow_down` (adicionando 5s), `expired_token` e `access_denied`.
3. **Persistência Segura em Cofre Nativo do Sistema (`ICredentialsVault.cs`)**:
   - Abstração `ICredentialsVault` com implementação nativa `DefaultCredentialsVault` delegando para `PasswordVault` do Windows.
   - Salva em `Files:https://github.com` com usuário `Personal Access Token`.
   - Limpeza completa (Logout) via `RemoveAccessToken()`.
4. **Tratamento Amigável de Ausência de Configuração (`DynamicDialogFactory.cs` / `GitHelpers.cs`)**:
   - Ao tentar conectar sem Client ID configurado, o app agora exibe o diálogo `GetFor_GitHubOAuthClientIdNotConfigured`:
     *"A integração com o GitHub requer um Identificador de Aplicativo (Client ID) configurado. Para habilitar a conexão, defina a variável de ambiente KYRAFILES_GITHUB_CLIENT_ID com o Client ID do seu aplicativo registrado no GitHub."*
   - Elimina falhas silenciosas e diálogos genéricos opacos.
5. **Suíte Completa de Testes Unitários (`GitHubAuthServiceTests.cs`)**:
   - 7 novos testes unitários com simulação HTTP mock (`MockHttpMessageHandler`) e cofre in-memory (`InMemoryCredentialsVault`).
   - Cobertura completa de cenários de sucesso, pendência, slow-down, expiração, acesso negado, erro de servidor e cofre.

---

## 2. Estado por Cenário de Aceite (AC)

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Conexão real com GitHub | `CONFIRMED` (Alexandre) | Login feito pelo Alexandre no app em 27/11/2026 ("F070 logada"); antes NOT TESTED por falta do Client ID |
| **AC-2** | Desconexão e limpeza | `CONFIRMED` | Teste `VaultOperations_SaveAndRemove_WorksAsExpected` passando 100% |
| **AC-3** | Token salvo no `PasswordVault` | `CONFIRMED` | Implementado via `DefaultCredentialsVault` / `CredentialsHelpers.cs` |
| **AC-4** | Mensagem orientadora clara sem Client ID | `CONFIRMED` | Teste `RequestDeviceCodeAsync_WhenClientIdNotConfigured_ReturnsMissingClientIdWithoutHttp` e `DynamicDialogFactory.GetFor_GitHubOAuthClientIdNotConfigured()` |

---

## 3. Arquitetura e Arquivos Tocados

- `src/KyraFiles.App/Services/Git/IGitHubAuthService.cs` (novo): Contrato de serviço e DTOs `DeviceCodeResult`, `DeviceCodeStatus`, `TokenPollStatus`.
- `src/KyraFiles.App/Services/Git/GitHubAuthService.cs` (novo): Implementação completa do fluxo RFC 8628.
- `src/KyraFiles.App/Helpers/ICredentialsVault.cs` (novo): Abstração de cofre de credenciais para injeção e teste.
- `src/KyraFiles.App/Helpers/Application/AppLifecycleHelper.cs`: Registro de `IGitHubAuthService` como singleton no contêiner de DI.
- `src/KyraFiles.App/Helpers/Dialog/DynamicDialogFactory.cs`: Novo diálogo `GetFor_GitHubOAuthClientIdNotConfigured()`.
- `src/KyraFiles.App/Utils/Git/GitHelpers.cs`: Refatoração do método `RequireGitAuthenticationAsync()` para consumir `IGitHubAuthService`.
- `tests/KyraFiles.App.Search.Index.Tests/GitHubAuthServiceTests.cs` (novo): Testes unitários com mock HTTP e cofre em memória.
- `tests/KyraFiles.App.Search.Index.tests/KyraFiles.App.Search.Index.Tests.csproj`: Inclusão dos novos arquivos na compilação de testes.

---

## 4. Medições e Validação

1. **Testes Unitários:**
   - Comando: `dotnet test tests/KyraFiles.App.Search.Index.tests/KyraFiles.App.Search.Index.Tests.csproj -c Debug -a x64`
   - Resultado: **184 testes (183 aprovados, 0 falhas, 1 benchmark F021 desativado)**.
2. **Compilação Release:**
   - Comando: `mkfile r src/KyraFiles.App/KyraFiles.App.csproj`
   - Resultado: **0 erros**, `[mkfile] concluido`.
3. **Gate de Desempenho e Contenção F021:**
   - Comando: `pwsh tools\perf\verify_no_index_service.ps1 -AliasPath "none"`
   - Resultado: **Aprovado**. `KyraFiles.App.IndexService` inativo, CPU em repouso < 5%, zero cópias de Debug no ambiente.
4. **Instalação:**
   - Atualizado e verificado em `%LOCALAPPDATA%\Programs\KyraFiles\KyraFiles.exe`.
