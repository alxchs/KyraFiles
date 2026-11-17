# F083 — Identidade KyraFiles: Marca, Ícones do Windows e Imagem no About

## 1. Visão Geral e Motivação

O projeto foi renomeado oficialmente de "Files" para **KyraFiles** por decisão do Alexandre. Na etapa anterior (F081), o backend de migração não-destrutiva e a harmonização inicial foram preparados.

Agora, o Alexandre solicitou a substituição visual e textual completa e definitiva:
1. **Nenhuma mensagem voltada ao usuário pode se referenciar ao app como "Files"**: Todas as mensagens, títulos, diálogos, notificações toast e rótulos devem se referir ao aplicativo como **KyraFiles**.
2. **Ícone do Pet no About**: A tela "Sobre" (`AboutPage.xaml`) deve exibir com destaque o ícone do pet (a mascote Kyra) compartilhado pelo usuário (`D:\DriveD\Downloads\KyraFiles.png`).
3. **Substituição dos Ícones Rosa no Windows**: Eliminar definitivamente o ícone antigo de pastas rosa/magenta do Windows (ícone da barra de tarefas, ícone do executável `.exe`, ícone de janela e tiles do sistema), substituindo-os pelos ícones oficiais do **KyraFiles** gerados a partir da arte da mascote.

---

## 2. Histórias de Usuário & Critérios de Aceite (Given / When / Then)

### Cenário 1: Mensagens e Textos ao Usuário
- **Given** que o usuário está utilizando o aplicativo em qualquer idioma suportado (pt-BR, en-US, de-DE, es-ES)
- **When** um diálogo de erro não tratado, notificação em segundo plano, aviso de administrador, opção de integração de sistema ou mensagem informativa for exibida
- **Then** o nome exibido deve ser estritamente **KyraFiles** (ou correspondente traduzido com o nome KyraFiles), nunca "Files".

### Cenário 2: Ícone do Pet na Tela Sobre (About)
- **Given** que o usuário acessa as Configurações e clica na seção "Sobre" (About)
- **When** a página `AboutPage` é carregada
- **Then** a área de identificação do app exibe o ícone em alta definição do pet KyraFiles (em vez do glifo genérico de informação), acompanhado do nome "KyraFiles" e da versão com data/hora de compilação.

### Cenário 3: Ícones no Windows (Executável, Barra de Tarefas, Janela e Bandeja)
- **Given** que o binário é gerado ou instalado no Windows
- **When** o usuário visualiza o aplicativo na Barra de Tarefas, no Explorador de Arquivos (`.exe`), na barra de título da janela ou na bandeja do sistema
- **Then** o ícone visível é o novo ícone do **KyraFiles** baseado na mascote Kyra, com fundos limpos e sem nenhuma das antigas "pastinhas rosa".

---

## 3. Restrições e Segurança

- **Arte e Cores**: Respeitar a regra de arte do Alexandre: nunca usar vermelho, nunca vermelho com amarelo, nem vermelho com estrela amarela. A paleta do KyraFiles é azul ciano, azul elétrico, branco e grafite/preto, 100% conforme a arte fornecida.
- **Licença**: Preservar todos os avisos de licença e copyright do KyraFiles Community nos cabeçalhos de código fonte (`Copyright (c) KyraFiles Community`).

