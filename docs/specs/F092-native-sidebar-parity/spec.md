# F092 — Paridade da Barra Lateral com o Explorador Nativo — SPEC

Status: APROVADA PELO ALEXANDRE (10/11/2026, respostas dadas à Claude). Submetida para validação final da Claude. Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [ ] **APROVAÇÃO DA CLAUDE** (data: pendente)
- [x] **APROVAÇÃO DO ALEXANDRE** (10/11/2026, respostas dadas à Claude)

## 1. Por quê
O Windows Explorer nativo exibe uma árvore completa e familiar no painel de navegação, contendo:
1. O nó navegável **Este Computador (This PC)** que agrupa tanto as unidades locais quanto os locais e dispositivos de rede do usuário.
2. Os **Locais de Rede e Dispositivos** anexados a Este Computador (compartilhamentos SMB de roteadores, servidores de mídia DLNA/UPnP desta máquina e de outras, e dispositivos portáteis como celulares/TVs), como `alxchs@gmail.com (lenovo)` e `hd-roteador`.
3. A pasta de **Bibliotecas (Libraries)** acessível diretamente no painel.
4. O atalho para o **Painel de Controle (Control Panel)**.

No KyraFiles atual, esses elementos vitais de navegação diária estão ausentes ou ocultos por padrão, gerando quebra de fluxo para o usuário acostumado à paridade e completude do Explorador nativo.

## 2. Histórias de Usuário
- **US1 (P1 - Este Computador):** Como usuário, quero ver e poder clicar no nó "Este Computador (This PC)" na barra lateral, navegando diretamente para a visão geral do sistema e suas unidades.
- **US2 (P1 - Locais de Rede e Dispositivos em Este Computador):** Como usuário, quero que todo local que o Explorador do Windows mostra em "Este Computador" e em "Rede" (HD do roteador, servidor de mídia desta máquina e de outras, TV, celular, notebook compartilhando fotos e vídeos) apareça na barra lateral E tenha seu conteúdo aberto e navegável no KyraFiles, como no Explorador.
- **US3 (P1 - Bibliotecas):** Como usuário, quero que a seção de "Bibliotecas (Libraries)" esteja habilitada e visível por padrão na barra lateral em instalação nova E em atualização, respeitando quem desligou a opção de propósito, facilitando o acesso imediato a Documentos, Imagens, Vídeos e Músicas.
- **US4 (P2 - Painel de Controle):** Como usuário, quero um atalho do "Painel de Controle (Control Panel)" na barra lateral para abrir o painel de gerenciamento do sistema com um clique.

## 3. Cenários de Aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| **AC-1** | App recém-instalado ou em atualização de versão | Olho a barra lateral | A seção "Bibliotecas" (Libraries) está visível por padrão tanto em instalação nova quanto em atualização, listando as bibliotecas do Windows (Documentos, Imagens, Músicas, Vídeos); quem desligou a opção de propósito continua com ela desligada. |
| **AC-2** | Barra lateral aberta | Olho os itens de navegação | Existe o nó "Este Computador" (This PC) com seu ícone característico do sistema; ao clicar nele, a página de unidades e visão geral do computador é aberta. |
| **AC-3** | Computador com locais de rede, mídias ou storage (HD do roteador, servidor de mídia desta máquina e de outras, TV, celular, notebook) | Exibo Este Computador ou a seção de unidades/rede | Todo local que o Explorador do Windows mostra em "Este Computador" e em "Rede" aparece na barra lateral E tem seu conteúdo de pastas e arquivos aberto e navegável diretamente no KyraFiles, como no Explorador. |
| **AC-4** | Barra lateral aberta | Clico no item "Painel de Controle" (Control Panel) | O Painel de Controle clássico do Windows abre com um clique. |
| **AC-5** | Usuário deseja personalizar sua barra lateral | Abre as configurações de Barra Lateral (Settings > Folders / Sidebar) | Opções para exibir/ocultar "Este Computador", "Bibliotecas" e "Painel de Controle" estão disponíveis e respeitam a escolha do usuário. |

## 4. Requisitos
- **FR-001:** O nó "Este Computador" deve navegar para a visualização central de unidades de disco e dispositivos do computador.
- **FR-002:** Enumeração de dispositivos e locais de rede anexados ao computador deve ocorrer em segundo plano, sem travar a renderização inicial da barra lateral.
- **FR-003:** O atalho do Painel de Controle deve abrir a interface clássica de gerenciamento de configurações do sistema.
- **FR-004:** As Bibliotecas devem vir ativadas por padrão para instalações novas e atualizações, preservando a escolha de usuários que as desativaram manualmente e mantendo os atalhos de restauração existentes.
- **FR-005:** As preferências do usuário devem permitir alternar a visibilidade de "Este Computador", "Bibliotecas" e "Painel de Controle".
- **NFR-001:** A adição dos nós não pode aumentar o tempo de inicialização ou abertura da barra lateral em mais de 50ms (carregamento estritamente assíncrono).
- **NFR-002:** Ausência de qualquer bloqueio de thread de UI (responsividade 100%).

## 5. Casos de Borda
- Dispositivo ou storage de rede desconectado ou roteador desligado: exibir ícone com timeout gracioso em segundo plano, sem congelar a interface.
- Item que o KyraFiles ainda não consegue abrir: mostra um aviso claro e oferece abrir no Explorador do Windows (nunca tela vazia nem travamento).
- Sistema sem bibliotecas padrão configuradas: permitir restauração pelo menu de contexto existente.
- Falha na abertura do Painel de Controle clássico: abrir as configurações do sistema como alternativa segura.

## 6. Fora de Escopo
- Modificar o explorador nativo do Windows.
- Alterar compartilhamentos ou permissões de rede do roteador.

## 7. Histórico e Decisões
- 2026-11-10 — Criação do rascunho inicial (AGY).
- 2026-11-10 — Revisão e correções estruturais solicitadas pela Claude: renomeação de F091 para F092, remoção de referências de tecnologia dos requisitos funcionais, inclusão de T0 de baseline e T5 de AC-5, e levantamento das dúvidas de clarification.
- 2026-11-10 — **Decisões do Alexandre (confirmadas à Claude):**
  1. *Bibliotecas (AC-1):* Ativadas por padrão para todos (novos e atualizações), respeitando quem desligou de propósito.
  2. *Locais de Rede / Dispositivos (AC-3):* Todo local que o Explorador nativo mostra em "Este Computador" e em "Rede" (HD do roteador, servidores de mídia locais/remotos, celular, TV) deve aparecer e ter conteúdo navegável no KyraFiles.
  3. *Caso de borda:* Aviso claro com opção "Abrir no Explorador" caso um item shell não seja diretamente navegável.
