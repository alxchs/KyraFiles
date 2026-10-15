# F018 — "Sobre" e "Verificar atualizações" no menu Ajuda — SPEC

Estado: APROVADA (Claude, 12/10/2026). Pedido do Alexandre: "não tem about" e "não vi checar novas versões".

## Por que
O menu Ajuda só tinha Ajuda, Notas de versão e arquivos de log. A página Sobre existia só dentro das Configurações, sem atalho, e nenhum lugar permitia conferir se há versão nova (o serviço de atualização do build de desenvolvimento não faz nada).

## Histórias
- **US1 (P1)** Como usuário, abro Ajuda e vejo "Sobre"; ao clicar vejo a página Sobre (nome e versão do aplicativo).
- **US2 (P1)** Como usuário, abro Ajuda e clico em "Verificar atualizações"; o aplicativo diz se estou na última versão, se há uma versão nova (e me leva à página de download) ou por que não conseguiu verificar.
- **US3 (P2)** Na página Sobre há o mesmo botão "Verificar atualizações", com a versão atual ao lado.

## Cenários de aceite
- **AC-1** Dado o aplicativo aberto, Quando abro o menu Ajuda, Então existem os itens "Sobre" e "Verificar atualizações".
- **AC-2** Dado o menu Ajuda, Quando clico em "Sobre", Então abre a página Sobre das Configurações, mostrando a versão atual.
- **AC-3** Dado que a versão publicada mais recente é igual à instalada, Quando verifico, Então vejo "Você está usando a versão mais recente (X)".
- **AC-4** Dado que há versão publicada maior que a instalada, Quando verifico, Então vejo a versão nova e a atual, e um botão que abre a página de download no navegador.
- **AC-5** Dado que nenhuma versão foi publicada, Quando verifico, Então vejo "Nenhuma versão foi publicada ainda".
- **AC-6** Dado que não há internet ou o servidor falha, Quando verifico, Então vejo uma mensagem de falha, sem travar o aplicativo e sem demorar mais de 15 segundos.
- **AC-7** A verificação só acontece quando o usuário pede (nenhuma chamada de rede automática).

## Requisitos
- FR-001 Item "Sobre" no menu Ajuda. FR-002 Item "Verificar atualizações" no menu Ajuda. FR-003 Botão equivalente na página Sobre. FR-004 Textos em inglês e português do Brasil.
- NFR-001 A verificação não bloqueia a interface. NFR-002 Nenhum dado do usuário é enviado além do pedido HTTP de leitura.

## Fora de escopo
Baixar e instalar a atualização (é a F017 T4). Verificação automática em segundo plano.

## Pontos em aberto
- [RESOLVIDO por decisão em `DECISIONS.md` D-022] De onde vem a "versão publicada": releases do GitHub do repositório do projeto.
