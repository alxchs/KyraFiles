# F017 — Interceptador Global Ultraleve, Persistente do Win+E e Instalador Modular — SPEC

Status: **APROVADA PELO ALEXANDRE** (12/10/2026).
Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Por quê

O atalho `Win + E` é a forma primária de navegação de pastas no Windows. No Windows 11 moderno, esse atalho é tratado hardcoded diretamente na thread da shell do `explorer.exe`.
Quando o Files Custom está fechado:
1. Abrir o processo completo do Files no boot do Windows custa ~2 a 3 segundos de CPU e consome ~180 MB de memória em segundo plano.
2. Com o Files fechado, o `Win + E` cai no Windows Explorer nativo.
3. As instalações tradicionais por MSIX/AppX ou despejo em pastas do Windows podem poluir o sistema e recarregar dependências desnecessárias a cada atualização.

Para garantir que o Files seja **sempre** acionado pelo `Win + E` (mesmo desligado), com custo de boot praticamente nulo, sem tolerância a encerramento do processo e com uma entrega modular e limpa via instalador inteligente, implementa-se o subsistema F017.

---

## 2. Histórias de Usuário

- **US1 (Interceptação Transparente):** Como usuário, quero que ao pressionar `Win + E`, o Files Custom se abra instantaneamente, mesmo que nenhuma janela do Files esteja em execução.
- **US2 (Custo Mínimo de Sistema):** Como usuário, não quero que o interceptador torne o boot do Windows lento nem gaste memória apreciável (teto de 5 ms de inicialização e < 3 MB de RAM).
- **US3 (Alta Resiliência e Persistência):** Como usuário, se o processo interceptador for derrubado ou finalizado intencionalmente (via Gerenciador de Tarefas ou `Stop-Process`), ele deve se recuperar e voltar à execução no segundo seguinte.
- **US4 (Roteamento Inteligente):** Se o Files já estiver aberto, o interceptador deve apenas trazer a janela existente para o primeiro plano (ou abrir nova aba via protocolo), sem subir outro processo pesado.
- **US5 (Instalação e Dependências Modulares):** Como usuário, quero um instalador executável (.exe / Inno Setup) que isole os binários e dependências em pasta própria (`%LocalAppData%\Programs\FilesCustom\`), verificando via hash SHA256/manifesto se as dependências já existem para baixar/instalar apenas o que mudou (atualização delta leve).
- **US6 (Atualização Automática Inteligente):** Ao verificar atualizações (por notificação ou clique do usuário), o atualizador deve baixar apenas o instalador reduzido se o pacote de dependências já estiver presente na máquina, gerenciando com segurança o fechamento dos processos para a troca limpa de binários.
- **US7 (Desinstalação 100% Limpa e Reversão do Win+E):** Se o usuário desinstalar o Files Custom, tudo deve sumir (pastas, registros e serviços) e o atalho `Win + E` deve voltar imediatamente a abrir o Windows Explorer nativo sem deixar rastros.
- **US8 (Garantia de Desempenho):** O Files Custom e seus subsistemas devem manter latência ultra-baixa de navegação, abertura e carregamento de diretórios, com regressões estritamente monitoradas e corrigidas.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | O Files Custom está completamente fechado (nenhum processo `Files.exe` ativo) | Pressiono `Win + E` no teclado | O Files Custom abre na tela; o Windows Explorer **nunca** aparece. |
| **AC-2** | O Files Custom já está aberto na tela | Pressiono `Win + E` | A janela existente do Files ganha foco e navega/abre aba em "Este Computador", sem piscar a tela. |
| **AC-3** | O processo interceptador `Files.HotkeyService.exe` está em execução | O processo é encerrado à força (`taskkill /F` ou via Task Manager) | Em até 1 segundo, o processo volta a rodar e a interceptar o `Win + E`. |
| **AC-4** | O Windows é reiniciado | O usuário faz login | O interceptador sobe em menos de 10 ms e consome menos de 3 MB de RAM. |
| **AC-5** | O usuário desmarca "Substituir Explorer" nas Configurações do Files | A opção é desmarcada | O interceptador e seu supervisor encerram a vigilância de forma limpa, devolvendo o `Win + E` padrão ao Explorer. |
| **AC-6** | Uma nova versão está disponível mas as dependências pesadas não mudaram | O usuário clica em "Verificar atualizações" ou recebe notificação | O sistema baixa um pacote reduzido (delta), encerra os processos em execução, substitui os binários atualizados e reabre o app sem travar arquivos. |
| **AC-7** | O usuário executa a desinstalação pelo Painel de Controle / Configurações | A desinstalação é concluída | A pasta `%LocalAppData%\Programs\FilesCustom\` e chaves do Registro são removidas por completo; o `Win + E` volta imediatamente a abrir o Windows Explorer. |
| **AC-8** | Medição de desempenho realizada após a instalação do pacote | O Files é aberto e navega em pastas locais | Métricas de abertura e rolagem ficam dentro do orçamento (inferiores ao Explorer/OneCommander). |

---

## 4. Requisitos do Sistema

### 4.1. Requisitos Funcionais (FR)
- **FR-001:** O executável principal (`Files.HotkeyService.exe`) deve ser um executável nativo Win32 compilado em C++ (ou C# Native AOT puro sem dependências pesadas de UI).
- **FR-002:** Deve registrar o atalho global `Win + E` (via `RegisterHotKey` com `MOD_WIN | MOD_NOREPEAT` e tecla `0x45`).
- **FR-003:** Ao ser acionado, deve verificar se existe a instância ativa do Files. Se existir, ativa via protocolo `files-dev:`; se não existir, inicia o processo principal.
- **FR-004:** Deve implementar o padrão de **Supervisão Cruzada (Watchdog Pareado)**:
  - Dois processos mínimos que monitoram o handle um do outro via `WaitForSingleObject(hProcess, INFINITE)`. Se o Processo A morrer, o Processo B o reinicia imediatamente em < 1 segundo e vice-versa.
- **FR-005:** O instalador executável (.exe baseado em Inno Setup) deve instalar em `%LocalAppData%\Programs\FilesCustom\` com subpasta isolada para dependências.
- **FR-006:** O instalador e o atualizador devem calcular hashes SHA256 contra o `manifest.json` para realizar download e cópia diferencial (apenas dos arquivos alterados).
- **FR-007:** O desinstalador deve remover a chave do `Run`, desregistrar associações e devolver incondicionalmente o `Win + E` e abertura de diretórios ao `explorer.exe`.

### 4.2. Requisitos Não Funcionais (NFR)
- **NFR-001 (Boot time):** O executável deve inicializar e registrar o hotkey em **menos de 10 milissegundos**.
- **NFR-002 (Consumo de RAM):** O conjunto em segundo plano não pode exceder **3 MB de Working Set**.
- **NFR-003 (Recuperação):** O tempo entre a finalização do processo e a sua restauração ativa deve ser de **≤ 1 segundo**.
- **NFR-004 (Tamanho do Atualizador Delta):** Atualizações regulares de código sem alteração de dependências devem gerar pacotes de download reduzidos (< 20 MB).
- **NFR-005 (Desempenho de UI):** Tempo de resposta e navegação de diretórios deve ser auditado com os testes de performance (`tools/perf/`) para garantir zero degradação.
