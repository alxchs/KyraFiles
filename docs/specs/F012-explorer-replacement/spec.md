# F012 — Substituição Transparente do Windows Explorer com Fallback Automático — SPEC

Status: **APROVADA** (Claude, 09/10/2026, a pedido do Alexandre para seguir a fase), com os dois pontos da §8 decididos.
Metodologia: Spec-Driven Development (`METODO.md` §6).
Documento de especificação funcional: define **o quê e por quê**, independente de classes e detalhes internos de implementação (reservados para o `plan.md`).

---

## 1. Por quê

O objetivo do Files Custom é ser o gerenciador de arquivos principal do usuário, entregando toda a experiência moderna de produtividade (renomeação protegida, colunas independentes, todas as colunas do Explorer, modos de busca ultra-rápidos, barra de menus clássica e densidade compacta).

Para atingir a integração plena no sistema operacional, o Files Custom precisa **assumir o lugar do Windows Explorer como gerenciador de pastas padrão do sistema**, respondendo nativamente ao atalho `Win + E`, a cliques duplos em pastas na Área de Trabalho e a chamadas de abertura de diretórios disparadas por quaisquer programas de terceiros (navegadores, IDEs, clientes de download, jogos, prompt de comando, etc.), exatamente como se o Explorer nativo de pastas não existisse.

Entretanto, essa substituição impõe um requisito mandatório de segurança e resiliência:
> **Na ausência, falha, desinstalação ou desativação do Files Custom, o Windows Explorer nativo DEVE reassumir o controle instantaneamente e de forma 100% automática e transparente, sem nunca deixar o usuário desamparado com erros de sistema ("Arquivo não encontrado") ou impossibilitado de abrir diretórios.**

---

## 2. Histórias de Usuário

- **US1 (P1 — Atalho Global):** Como usuário, quero que ao pressionar o atalho de teclado `Win + E` em qualquer lugar do Windows, o Files Custom se abra imediatamente na minha pasta inicial (Home) em vez do Windows Explorer.
- **US2 (P1 — Abertura de Diretórios pelo Sistema):** Como usuário, quero que ao dar duplo clique em uma pasta na Área de Trabalho, no menu Iniciar ou ao clicar em "Abrir pasta do arquivo" em qualquer aplicativo de terceiros (navegador, IDE, etc.), o Files Custom abra exibindo a pasta solicitada.
- **US3 (P1 — Fallback Automático e Transparente):** Como usuário, se o Files Custom for desinstalado, movido, corrompido, bloqueado ou estiver inacessível, quero que o Windows Explorer abra a pasta automaticamente sem exibir qualquer pop-up de erro.
- **US4 (P1 — Integridade da Shell do Windows):** Como usuário, quero que a barra de tarefas do Windows, o menu Iniciar, a bandeja do sistema e o papel de parede continuem funcionando normalmente pelo Explorer do Windows, sendo interceptada apenas a navegação de janelas de pastas.
- **US5 (P2 — Modo de Segurança e Recuperação):** Como usuário, ao iniciar o Windows em Modo de Segurança (Safe Mode) ou ambiente de diagnóstico, o sistema deve utilizar automaticamente o Explorer nativo sem tentativas frustradas de carregar extensões avançadas.
- **US6 (P2 — Roteamento em Instância Única e Abas):** Como usuário, se o Files Custom já estiver aberto e um novo comando de abertura de pasta for disparado pelo sistema, quero que a pasta seja aberta como uma nova aba na janela existente (ou navegue para ela caso já esteja aberta), trazendo a janela para o foco sem criar dezenas de instâncias pesadas.
- **US7 (P2 — Controle Reversível nas Configurações):** Como usuário, quero poder ativar ou desativar a substituição do Windows Explorer com um único clique nas Configurações do app (ou pelo Instalador), revertendo o sistema ao estado padrão da Microsoft sem deixar resíduos no Registro.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | Files Custom configurado como gerenciador padrão e em execução (aberto ou em segundo plano) | Pressiono `Win + E` em qualquer janela | O Files Custom abre em "Este Computador" (o que o Windows pediria ao Explorer), sem a janela do Explorer ficar visível. Com o Files fechado, o Explorer abre normalmente. |
| **AC-2** | Files Custom configurado como gerenciador padrão | Dou duplo clique em uma pasta na Área de Trabalho | O Files Custom abre diretamente o caminho dessa pasta. |
| **AC-3** | Files Custom configurado como gerenciador padrão | Clico em "Mostrar na pasta" / "Abrir local do arquivo" em um navegador web (Chrome, Edge, Firefox) | O Files Custom abre a pasta de destino com o arquivo focado/selecionado. |
| **AC-4** | Files Custom configurado como gerenciador padrão (para `explorer.exe`, também em execução) | Executo `explorer.exe C:\Projetos` ou `start C:\Projetos` via linha de comando ou diálogo Executar (`Win + R`) | A pasta abre no Files Custom, sem a janela do Explorer ficar visível. |
| **AC-11** | Files Custom como gerenciador padrão e em execução | Uma janela do Explorer é aberta numa localização que o Files não exibe (Painel de Controle, Configurações de rede, bibliotecas) | A janela do Explorer fica como está, visível, sem redirecionamento. |
| **AC-12** | Files Custom como gerenciador padrão e em execução | O próprio Files abre o Explorer (ex.: "Abrir no Explorer", localização que ele não suporta) | A janela do Explorer não é redirecionada de volta ao Files. |
| **AC-5** | Files Custom já em execução com janelas abertas | Um aplicativo externo solicita a abertura da pasta `D:\Documentos` | O Files Custom não inicia um processo de janela duplicado; adiciona uma nova aba `D:\Documentos` na janela ativa e a traz para o primeiro plano. |
| **AC-6** | Files Custom configurado como padrão, mas seu executável é apagado, corrompido ou o pacote MSIX desinstalado | O usuário dá duplo clique em qualquer pasta ou pressiona `Win + E` | O Windows Explorer nativo abre a pasta imediatamente, com zero mensagens de erro ou avisos de "arquivo não encontrado". |
| **AC-7** | O computador é inicializado em Modo de Segurança (Safe Mode) | O usuário abre qualquer pasta | O Windows Explorer nativo abre a pasta diretamente. |
| **AC-8** | Files Custom como gerenciador padrão | O usuário desmarca a opção "Definir como gerenciador padrão" nas Configurações | O sistema reverte imediatamente todas as associações do Registro, e o Windows Explorer volta a ser acionado normalmente. |
| **AC-9** | Files Custom desinstalado pelo desinstalador oficial | O processo de desinstalação é concluído | Todas as chaves e proxies são removidos; o Explorer volta a responder a pastas e `Win + E`. |
| **AC-10** | Files Custom como gerenciador padrão | O usuário acessa itens especiais da shell (Painel de Controle, Lixeira, Rede, Computador) | O Files Custom abre sua respectiva tela interna caso suporte (ex: Lixeira, Meu Computador), ou repassa com segurança para o manipulador nativo da shell do Windows. |

---

## 4. Requisitos do Sistema

### 4.1. Requisitos Funcionais (FR)
- **FR-001:** O sistema DEVE interceptar o atalho global `Win + E` para iniciar o Files Custom.
- **FR-002:** O sistema DEVE interceptar a ação padrão de clique duplo / abertura de diretórios (`Folder`, `Directory`) em todo o subsistema de shell do Windows.
- **FR-003:** O sistema DEVE encaminhar o caminho de destino (`%1` / arguments) e parâmetros de seleção (`/select, "caminho"`) para o Files Custom.
- **FR-004:** O sistema DEVE conter um mecanismo despachante (trampolim/proxy de execução) com garantia de fallback incondicional: se o Files Custom não puder ser iniciado com sucesso, o despachante DEVE invocar imediatamente o executável original do Windows Explorer (`%SystemRoot%\explorer.exe`) com os mesmos argumentos.
- **FR-005:** O sistema DEVE suportar comunicação entre processos para reaproveitar instâncias existentes em execução, abrindo novas abas em vez de processos de interface isolados.
- **FR-006:** A interface de Configurações do Files Custom DEVE exibir o estado real da associação (Ativado / Desativado) e permitir alterná-la a qualquer momento sem necessidade de reiniciar o Windows.
- **FR-007:** O instalador executável oficial DEVE oferecer a opção de registrar o Files Custom como gerenciador padrão no momento da instalação e garantir desassociação limpa durante a desinstalação.
- **FR-008:** A associação DEVE ser configurada prioritariamente no escopo do usuário atual (`HKEY_CURRENT_USER`), evitando exigir privilégios de Administrador global para o uso rotineiro sempre que possível.
- **FR-009:** Enquanto o Files Custom estiver em execução (inclusive em segundo plano) e configurado como gerenciador padrão, toda janela de pasta do Explorer que abrir DEVE ser redirecionada para o Files: a mesma localização, com o item selecionado quando a janela foi aberta para selecionar um arquivo. Isso cobre os caminhos que o Windows trata dentro do próprio Explorer, sem passar pelas associações do registro (`Win + E`, `explorer.exe <pasta>`, "Mostrar na pasta").
- **FR-010:** A janela do Explorer redirecionada NÃO DEVE ficar visível para o usuário; se o redirecionamento não acontecer (localização não suportada, falha, tempo esgotado), ela DEVE voltar a ficar visível, como se nada tivesse acontecido.
- **FR-011:** O redirecionamento NÃO DEVE exigir privilégio de administrador nem instalar nada no processo do Explorer.

### 4.2. Requisitos Não Funcionais (NFR)
- **NFR-001 (Resiliência Absoluta):** Sob nenhuma circunstância a exclusão, falha de integridade ou encerramento anômalo do Files Custom pode impedir o usuário de abrir pastas no computador.
- **NFR-002 (Latência Mínima do Trampolim):** A verificação de disponibilidade e o despacho para o Files Custom ou fallback para o Explorer devem levar menos de 25 milissegundos para não gerar atraso perceptível na abertura de janelas.
- **NFR-003 (Preservação da Shell):** O processo de desktop, barra de tarefas, área de notificação e janelas de sistema do Windows Explorer NÃO devem ser encerrados nem substituídos como shell primária do Windows; a substituição é estritamente restrita à função de *File Manager*.
- **NFR-004 (Compatibilidade de Versões):** O mecanismo deve funcionar tanto no Windows 10 (Build 19041+) quanto no Windows 11 (22H2, 23H2 e 24H2).

---

## 5. Casos de Borda

1. **Pastas virtuais com CLSID:** Caminhos como `::{645FF040-5081-101B-9F08-00AA002F954E}` (Lixeira) ou `shell:RecycleBinFolder`: devem ser resolvidos para seus equivalentes compatíveis.
2. **Caminhos com caracteres especiais e espaços:** Caminhos com aspas, espaços duplos ou acentuação devem ser repassados sem quebra de parsing de argumentos.
3. **Parâmetro `/select`:** Aplicações externas que chamam `explorer.exe /select, "C:\arquivo.zip"` devem abrir a pasta pai no Files Custom com o arquivo selecionado na lista.
4. **Execução com elevação (Run as Administrator):** Quando um aplicativo em modo administrador solicita abertura de pasta, o despacho deve lidar com níveis de privilégio sem quebrar a integridade do processo de usuário do WinUI 3.
5. **Caminhos de rede (UNC / WebDAV):** Caminhos como `\\servidor\compartilhamento` devem ser reconhecidos e abertos diretamente.

---

## 6. Fora de Escopo

- Substituição da barra de tarefas, do menu Iniciar ou da área de trabalho do Windows (a shell do sistema permanece sendo o Windows Explorer).
- Substituição de diálogos nativos comuns de arquivo (`IFileDialog` / `GetOpenFileName` de outros apps não empacotados), que são tratados separadamente pelo módulo `FilesOpenDialog`.

---

## 7. Critérios de Sucesso

1. `Win + E` e cliques em pastas abrem o Files Custom como se ele fosse a aplicação nativa do sistema operacional.
2. Com o Files Custom fechado ou até mesmo excluído da máquina, clicar em pastas abre o Windows Explorer imediatamente sem erros.
3. Ativar e desativar a função nas Configurações funciona imediatamente e é confirmado por testes de automação e validação física em tela.

---

## 8. Pontos em Aberto (decididos em 09/10/2026)

1. **Roteamento de Win+E no Windows 11 moderno:** ~~usar só a chave `CLSID\{52205fd8-...}`~~. **Revisto em 09/10/2026 (D-017):** nesta máquina (Windows 11 26340) o `Win + E` e o `explorer.exe <pasta>` são tratados dentro do Explorer e ignoram as chaves do HKCU (CONFIRMED pelo Alexandre com teclado real e por automação). O Alexandre pediu o comportamento do OneCommander e similares, sem administrador. **Decisão:** além das associações, o Files em execução redireciona as janelas do Explorer que abrirem (FR-009 a FR-011). Consequência aceita: com o Files fechado, `Win + E` e `explorer.exe` abrem o Explorer (o mesmo que o OneCommander documenta); para cobrir isso, o usuário liga "Abrir o Files ao iniciar o Windows".
2. **Abertura de nova aba vs nova janela:** **Decisão provisória:** seguir o comportamento atual do Files ao receber uma pasta de fora (a AC-5 vale como está). Se o Alexandre preferir outro, a spec muda primeiro.

---

## 9. Histórico

- **08/10/2026:** Especificação inicial elaborada conforme solicitação do Alexandre e metodologia SDD (`METODO.md` §6).
- **09/10/2026 (Claude):** aprovada; §8 decidida. Comportamento inalterado.
- **09/10/2026 (Claude, decisão do Alexandre "siga"):** AC-1 e AC-4 reescritas; AC-11, AC-12 e FR-009 a FR-011 novos (redirecionamento de janelas do Explorer); §8.1 revista (D-017).

