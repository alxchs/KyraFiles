# F070 — Login do GitHub Próprio — PLAN

Status: RASCUNHO (AGY, 31/10/2026). Só começa com a `spec.md` APROVADA. Responde "como", sem alterar o "o quê".

## Arquitetura atual (lida no código; estado de cada afirmação)

- `src/KyraFiles.App/Constants.cs` (CONFIRMED): Contém `Constants.AutomatedWorkflowInjectionKeys.GitHubClientId = "githubclientid.secret"`.
- `src/KyraFiles.App/Utils/Git/GitHelpers.cs` e `LibGit2Service.cs` (CONFIRMED):
  Definem `_clientId` como `string.Empty` em ambiente Dev ou `CLIENT_ID_SECRET` em Release.
  Ao acionar o login, executam `POST https://github.com/login/device/code?client_id={_clientId}&scope=repo`.
  Se a resposta não for bem-sucedida, simplesmente invocam `DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync()`.
- Diagnóstico do erro real (CONFIRMED):
  O endpoint do GitHub retorna HTTP 400 ou 404 quando o `client_id` é vazio ou contém o marcador de segredo (`githubclientid.secret`). A aplicação falha sem distinguir entre falta de internet, erro de servidor ou identificador inexistente, frustrando o usuário.
- Cofre de Credenciais (CONFIRMED):
  `CredentialsHelpers.cs` utiliza `Windows.Security.Credentials.PasswordVault` para persistir o token em `Files:https://github.com` com usuário `Personal Access Token`. Isso cumpre a exigência de nunca salvar senhas em arquivos de configuração locais.

## Abordagem

1. **Backend Primeiro (Núcleo Sem Tela e Testado):**
   - Criar interface e serviço dedicados `IGitHubAuthService` e `GitHubAuthService`:
     - `IsClientIdConfigured()`: valida se o identificador não é nulo, não é vazio e não é o marcador `.secret`.
     - `GetClientId()`: busca em ordem de precedência:
       1. Configuração do usuário (`UserSettingsService.GeneralSettingsService.GitHubClientId`);
       2. Variável de ambiente do sistema/usuário (`KYRAFILES_GITHUB_CLIENT_ID`);
       3. Constante do pacote.
     - `RequestDeviceCodeAsync(HttpClient client, CancellationToken ct)`:
       Se `!IsClientIdConfigured()`, retorna `DeviceCodeResult.MissingConfiguration` com motivo explícito.
       Se configurado, faz o POST e desserializa `device_code`, `user_code`, `verification_uri`, `expires_in` e `interval`.
     - `PollForAccessTokenAsync(...)`:
       Implementa o loop assíncrono respeitando o `interval` e tratando `authorization_pending`, `slow_down`, `expired_token` e `access_denied`.
     - `StoreTokenInVault(string token)` e `RemoveTokenFromVault()`: encapsulam `CredentialsHelpers`.
   - Testes unitários com mock HTTP (`MockHttpMessageHandler`) simulando todo o fluxo RFC 8628 (sucesso, pendência, erro de configuração e expiração).

2. **Interface e Apresentação (Tela por Último):**
   - Atualizar `GitHelpers.cs` e `GitPropertiesPageViewModel.cs` para consumir `IGitHubAuthService`.
   - Em caso de identificador ausente (AC-4), exibir diálogo amigável informativo:
     *"Para conectar sua conta do GitHub, é necessário configurar um Identificador de Aplicativo OAuth (Client ID). Você pode configurá-lo nas opções de desenvolvedor ou solicitar ao administrador."* com botão de ação direta ou cópia de instruções.
   - Cenários reais com o GitHub oficial (AC-1 e AC-2) são mantidos em `NOT TESTED` até o Alexandre cadastrar o app e fornecer o identificador.

## Mapa spec → código
| Requisito | Onde é atendido |
|---|---|
| FR-001, FR-002 | `GitHubAuthService.cs` (`IsClientIdConfigured`, `GetClientId`) |
| FR-003 | `GitHubAuthService.RequestDeviceCodeAsync` e `PollForAccessTokenAsync` |
| FR-004, FR-005 | `GitHubAuthService.StoreTokenInVault` e `RemoveTokenFromVault` |
| NFR-001 | Apenas o identificador público (`client_id`) é manipulado; nenhum `client_secret` é exigido no Device Flow |
| NFR-002 | `CancellationToken` repassado em todas as chamadas e respeitado no diálogo |
| NFR-003 | Diálogo com recursos visuais neutros |

## Arquivos
- **Mexer:**
  - `src/KyraFiles.App/Services/Git/IGitHubAuthService.cs` e `GitHubAuthService.cs` (novos).
  - `src/KyraFiles.App/Utils/Git/GitHelpers.cs`: integração com o novo serviço.
  - `src/KyraFiles.App/Helpers/CredentialsHelpers.cs`: garantia de métodos completos de logout.
  - `src/KyraFiles.App/Strings/en-US/Resources.resw` e `pt-BR/Resources.resw`: mensagens explicativas de configuração de OAuth.
  - `tests/KyraFiles.App.Search.Index.Tests/`: testes unitários do serviço de autenticação com mock HTTP.
- **Não mexer:**
  - `src/KyraFiles.App/Services/Git/LibGit2Service.cs` (ações de commit/push via LibGit2Sharp continuam consumindo a credencial padrão).

## Riscos e regressões a vigiar
- **Loop de polling sem fim:** Se o servidor GitHub não responder erro ou o usuário fechar a janela, o polling deve ser estritamente cancelado pelo token do diálogo.
- **Exposição de tokens:** Garantir que o token recebido nunca seja registrado no arquivo de log do app.

## Estratégia de teste
| Cenário | Tipo | Como se prova |
|---|---|---|
| AC-1 | Integração real | NOT TESTED até o Alexandre criar o app OAuth |
| AC-2 | Integração real | NOT TESTED até o Alexandre criar o app OAuth |
| AC-3 | Unitário | Teste mock confirma gravação em `PasswordVault` e ausência em arquivos `.json` |
| AC-4 | Unitário / Funcional | Teste com Client ID nulo/default confirma retorno `MissingConfiguration` e diálogo orientador |
