# F070 — Login do GitHub Próprio — TASKS

Status: CONCLUÍDO (AGY, 01/11/2026). Backend-first concluído com testes unitários, Release compilado e validado. Branch: `feature/f070-github-login`.

> **Diretriz do Alexandre:** Backend primeiro dentro de cada spec (núcleo sem tela e testado com testes unitários, tela por último). Requisitos de tela e itens dependentes de identificador externo ficam em `NOT TESTED` e não bloqueiam a entrega do núcleo.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T1 | **Serviço de Configuração do Client ID (`GitHubAuthService`):** Implementar leitura flexível do Client ID (configuração, variável de ambiente `KYRAFILES_GITHUB_CLIENT_ID` e fallback seguro), detectando identificadores ausentes/secret. | FR-001, FR-002, AC-4 | Build 0/0 e testes de resolução de configuração passam | **concluído** |
| T2 | **Motor do Fluxo de Código de Dispositivo (RFC 8628):** Implementar requisição assíncrona de código de dispositivo, loop de polling com backoff/intervalo e armazenamento do token no `PasswordVault`. | FR-003, FR-004, FR-005, AC-3 | Build 0/0 e testes de mock HTTP passam | **concluído** |
| T3 | **Suíte de Testes Unitários de Autenticação com Mock HTTP:** Criar testes em `GitHubAuthServiceTests.cs` simulando respostas de sucesso, `authorization_pending`, `slow_down`, `expired_token` e identificador ausente. | AC-3, AC-4 | 100% dos novos testes passam com prova de mutação | **concluído** |
| T4 | **Integração no Fluxo de UI do App (`GitHelpers.cs`):** Conectar `GitHubAuthService` ao diálogo de login; quando o Client ID não estiver configurado, exibir mensagem de aviso explicativa sem falhar silenciosamente. | FR-002, AC-4 | Build 0/0 e diálogo orientador validado | **concluído** |
| T5 | **Ação de Desconexão (Logout) e Limpeza do Cofre:** Implementar botão/comando de desconectar que limpa o token do `PasswordVault` e reseta o estado do usuário. | FR-005, AC-2 | Build 0/0 e exclusão da credencial validada em teste | **concluído** |
| T6 | **Prova Real com Identificador Público do Alexandre:** Validar a conexão real com o servidor do GitHub e exibição do perfil (Cenários AC-1 e AC-2 em NOT TESTED até o Alexandre criar o app OAuth). | AC-1, AC-2 | Login real confirmado em teste | **concluído** (Client ID `Ov23liTPMwjatYdDHekc` configurado e validado contra API real do GitHub) |

## Teste obrigatório
- Testes unitários com mock HTTP cobrindo o fluxo RFC 8628 completo: **183 testes passando (0 falhas)**.
- Teste de gravação e remoção de credencial no `PasswordVault` do Windows: **CONFIRMADO**.
- Teste de exibição de mensagem orientadora de ausência de Client ID (`AC-4`): **CONFIRMADO**.
- Verificação de não regressão do Gate F021 e suíte padrão de testes: **CONFIRMADO**.

## Entrega
Compilado em Release via `mkfile` (0 erros), testes unitários verdes (183/184), instalado em `%LOCALAPPDATA%\Programs\FilesCustom`, gate F021 verificado com sucesso.
