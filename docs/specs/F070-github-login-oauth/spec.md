# F070 — Login do GitHub Próprio — SPEC

Status: **APROVADA pelo Alexandre em 12/11/2026** ("Specs aprovadas"). Estado anterior: RASCUNHO (AGY, 31/10/2026). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui: isso é do `plan.md`.

## 1. Por quê
O recurso de integração com repositórios e branches Git possui a funcionalidade de "Conectar com GitHub". No entanto, a tentativa de login falha silenciosamente ou exibe erro genérico de conexão sem explicar a causa para o usuário. Isso ocorre porque o aplicativo não possui um identificador de aplicativo OAuth público configurado para autenticação por fluxo de dispositivo (Device Flow). O sistema precisa:
1. Tornar o identificador de cliente OAuth do GitHub configurável e auditável, sem embutir segredos privados no código.
2. Fornecer fluxo de login por código de dispositivo seguro, gravando o token exclusivamente no cofre seguro de credenciais do Windows.
3. Explicar claramente ao usuário o que falta caso nenhum identificador esteja configurado, em vez de falhar calado.
4. Permitir desconexão completa (logout) com exclusão imediata da credencial salva.

## 2. Histórias de usuário (por prioridade)
- **US1 (P1 - Autenticação Transparente e Segura):** Como desenvolvedor que utiliza o KyraFiles para gerenciar pastas versionadas, quero autenticar minha conta do GitHub via fluxo de código de dispositivo no navegador e ter meu token guardado com segurança no cofre do sistema, para clonar, buscar branches e sincronizar repositórios com facilidade.
- **US2 (P2 - Diagnóstico e Clareza de Configuração):** Como usuário ou administrador, quero que o aplicativo informe com clareza quando a integração do GitHub não estiver com o aplicativo OAuth configurado, explicando o passo necessário em vez de exibir um erro opaco de conexão.
- **US3 (P3 - Desconexão e Privacidade):** Como usuário, quero poder desconectar minha conta do GitHub a qualquer momento, garantindo que o token de acesso seja inteiramente removido do computador.

## 3. Cenários de aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | Identificador OAuth do GitHub configurado | O usuário clica em "Conectar GitHub" na interface | O sistema solicita o código de dispositivo ao GitHub, exibe o código amigável de 8 caracteres, abre a página de ativação no navegador padrão e, após confirmação, exibe o nome e avatar do usuário conectado. *(Fica NOT TESTED até fornecimento do identificador pelo Alexandre)* |
| AC-2 | Usuário autenticado com credencial ativa | O usuário clica em "Desconectar" (Logout) | A credencial e o token são apagados do cofre de senhas do sistema e a interface volta ao estado "Não conectado". *(Fica NOT TESTED até fornecimento do identificador)* |
| AC-3 | O fluxo de autorização obtém o token de acesso | O salvamento do token é executado | O token é armazenado estritamente no cofre seguro do sistema operacional (Credential Locker / PasswordVault), nunca em arquivos de configuração (`.json`, `.xml`, registro puro ou texto plano). |
| AC-4 | Nenhum identificador OAuth está configurado (ou valor padrão nulo/marcador de segredo) | O usuário clica em "Conectar GitHub" | O sistema exibe mensagem orientadora clara: *"A integração com o GitHub requer a configuração do Identificador de Aplicativo (Client ID). Configure o identificador nas opções de desenvolvedor ou registre o aplicativo no GitHub."*, sem falhar de forma opaca nem travar. |

## 4. Requisitos
- **FR-001:** O sistema DEVE desacoplar o Identificador de Cliente OAuth (`ClientId`) de constantes rígidas compiladas, permitindo injeção por configuração local ou variável de ambiente.
- **FR-002:** O sistema DEVE validar se o `ClientId` configurado é válido e não nulo antes de disparar requisições para o endpoint `/login/device/code`.
- **FR-003:** O sistema DEVE implementar o fluxo Device Authorization Grant (RFC 8628), com polling em intervalo respeitado e cancelamento a qualquer momento.
- **FR-004:** O sistema DEVE gravar o token obtido exclusivamente no cofre de credenciais nativo do Windows associado ao recurso do repositório.
- **FR-005:** O sistema DEVE apagar incondicionalmente a credencial do cofre quando a ação de logout for solicitada.
- **NFR-001 (Segurança de Segredos):** O código-fonte NUNCA deve conter o segredo de cliente (Client Secret), utilizando exclusivamente o fluxo público de dispositivos OAuth sem segredo.
- **NFR-002 (Resiliência de Rede):** O processo de polling do token deve implementar timeout configurável (máximo 15 minutos) e parar imediatamente se a janela de diálogo for fechada pelo usuário.
- **NFR-003 (Conformidade Visual):** Telas e diálogos de autenticação não devem empregar cor vermelha ou combinações de vermelho com amarelo.

## 5. Casos de borda
- O usuário fecha o diálogo de código de dispositivo antes de autorizar no navegador: o polling é cancelado imediatamente sem manter conexões abertas em segundo plano.
- O código de dispositivo expira no servidor antes da autorização do usuário: o sistema avisa que o código expirou e oferece gerar um novo.
- A máquina está sem conexão com a internet: aviso claro de falha de conexão sem travamento de interface.
- O usuário já possui uma credencial prévia no cofre: a nova conexão substitui a credencial anterior de forma atômica.

## 6. Fora de escopo
- Autenticação via Personal Access Token (PAT) digitado manualmente (já coberto pelo Git clássico).
- Criação e registro automático de aplicativo OAuth no painel do GitHub via API (responsabilidade do proprietário da conta).

## 7. Critérios de sucesso
- Sem `ClientId` definido, a tentativa de login exibe a mensagem amigável explicativa (AC-4).
- Em testes com servidor OAuth simulado localmente, o fluxo completo de código de dispositivo gera e grava a credencial no cofre e o logout a remove perfeitamente.

## 8. Pontos em aberto
- [NEEDS CLARIFICATION: Criação do aplicativo OAuth no GitHub pelo Alexandre e fornecimento do Client ID público. Enquanto isso, AC-1 e AC-2 permanecem NOT TESTED em ambiente real do GitHub, validados através de servidor mock em testes unitários].

## 9. Histórico
- 31/10/2026 — Criação inicial da spec em RASCUNHO pela AGY conforme `FILA-KYRAFILES.md` Item 2 e decisão 1b.4.
