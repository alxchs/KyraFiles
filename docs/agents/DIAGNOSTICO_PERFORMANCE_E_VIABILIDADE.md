# Diagnóstico de Performance e Avaliação de Viabilidade — KyraFiles

> [!IMPORTANT]
> **Data:** 19 de Outubro de 2026  
> **Objetivo:** Registro consolidado para **Alexandre, Claude e AGY (Antigravity)** sobre os testes de choque de performance da F024, o teste real com o usuário, os dados de telemetria capturados e o diagnóstico arquitetural definitivo sobre a viabilidade do KyraFiles.

---

## 1. Contexto da Sessão

O usuário Alexandre vinha reportando lentidão severa no uso do **KyraFiles (KyraFiles)**, tornando o uso inviável em cenários cotidianos. Foi acordada uma **última tentativa estruturada (F024)** para diagnosticar e atacar os gargalos mais críticos:
1. **Lentidão extrema em pastas grandes (~1.600 arquivos)** com navegação por teclado (`Home`, `End`, `PgUp`, `PgDn`, `Up`, `Down`).
2. **Sincronismo concorrente do FileSystemWatcher** (refletir adições e remoções externas em tempo real).
3. **Higiene estrita de binários** (eliminação de backups/lixo solto, deixando apenas o executável Release oficial único instalado em `%LOCALAPPDATA%\Programs\KyraFiles`).

---

## 2. O que foi Implementado na F024

Na branch `feature/f024-choque-performance-navegacao`:
- **Remoção de Resets Destrutivos (`ShellViewModel.cs`):** Eliminado o ciclo que disparava múltiplos `FilesAndFolders.Clear()` e `NotifyCollectionChangedAction.Reset` a cada lote intermediário de 500 ms (que destruía e remontava a árvore visual 5 vezes a cada abertura de pasta).
- **Watcher em Tempo Real:** Ajustado o queue flush para sincronizar adições e deleções concorrentes em ~20 a 30 ms.
- **Instalação Limpa Release:** Recompilado em `Release x64`, empacotado via Inno Setup e registrado com identidade AppX correta no Windows. Validado com o gate F021 (CPU em repouso < 5% e IndexService bloqueado).

---

## 3. Teste Real do Usuário (Achados Críticos)

Ao abrir o executável Release oficial e navegar na pasta de laboratório com os **1.600 arquivos reais** (`C:\KyraFilesUXLab\perf\1600`), o Alexandre observou imediatamente:

1. **Troca de abas:** Demasiadamente lenta (sensação de travamento a cada clique entre abas).
2. **Segurar teclas de navegação (`Home` / `End` / `Up` / `Down`):** Impossível de usar (congelamento total da interface).
3. **Tempo de abertura do executável:** "Parece que não vai abrir" (atraso perceptível e cliques repetidos não abrem nova janela).

---

## 4. Telemetria e Diagnóstico de Causa Raiz do Processo

A inspeção do processo `KyraFiles.exe` (`PID 37060`) durante a interação revelou números alarmantes:
- **CPU Acumulada:** **1.068 segundos de CPU** (núcleos em 100% contínuos).
- **Consumo de Memória:** Escalou para **1.041 MB (mais de 1 GB de RAM)**.
- **Thread Principal de UI (`TID 34416`):** Presa em `Wait / Running` alternados, enfileirando centenas de mensagens no `DispatcherQueue`.

### Causas Raiz Identificadas no Código:

#### A. Repetição Contínua de Teclado (Key Repeat a 30 Hz)
* Quando o usuário **segura** qualquer tecla de movimentação, o driver do teclado dispara ~30 eventos `KeyDown` por segundo.
* O `ListView` do WinUI 3 dispara para cada pulo:
  - `SelectionChanged` e `CurrentItemChanged`;
  - Atualização síncrona dos bindings de tamanho e contagem no `StatusBarViewModel`;
  - Atualização dos comandos habilitados na `NavigationToolbar`;
  - Cálculo de virtualização de contêineres e animação de retângulo de foco (`FocusState.Keyboard`).
* **Consequência:** A thread de UI fica 100% sobrecarregada processando layouts intermediários que o olho humano nem consegue ver. A fila de mensagens colapsa e o aplicativo congela.

#### B. Troca de Abas e o Motor de Layout do WinUI 3
* No KyraFiles, cada aba não é um painel leve: é uma árvore XAML hierárquica pesada (`ShellPanesPage -> ModernShellPage -> DetailsLayoutPage -> ListView -> dezenas de controles WinUI`).
* Ao trocar de aba, o `ContentPresenter` da `MainPage` desanexa a árvore da aba anterior e anexa a nova.
* Isso força o DirectUI do WinUI 3 a disparar um **Measure & Arrange pass síncrono e recursivo** de toda a nova árvore na thread de UI.
* Paralelamente, `SidebarViewModel.TabExpansion` percorre todos os nós do menu lateral e `NavigationHelpers.UpdateInstancePropertiesAsync` reavalia títulos e propriedades, gerando atraso perceptível de segundos.

#### C. Tempo de Abertura e Instância Única Travada
* O `Program.cs` utiliza `INSTANCE_ACTIVE` para redirecionar novas ativações para o processo existente (`RedirectActivationTo`).
* Quando o processo em execução fica sobrecarregado (consumindo 1 GB de RAM e 100% de CPU), qualquer clique para abrir o KyraFiles não abre um processo novo: tenta mandar IPC para a instância zumbi que não responde, dando a impressão de que o executável "morreu".
* Mesmo em inicialização a frio, o startup carrega dezenas de singletons, providers de nuvem (o Google Drive gerou exceções de timeout no log `debug.log`), verificação de unidades e parsing de dicionários XAML antes de renderizar a interface.

---

## 5. Análise de Viabilidade Técnica (Realista)

Perguntado sobre a viabilidade de contornar esses problemas com novas cirurgias no código:

| Área | Possível Contorno Técnico | Consequência Colateral Inevitável |
|---|---|---|
| **Segurar Teclas** | Throttling de repetição (descartar eventos intermediários a cada 100 ms) | A navegação deixa de travar a CPU, mas fica "truncada" e aos saltos. Nunca terá a fluidez suave (60 FPS) de um app nativo Win32/C++. |
| **Troca de Abas** | Manter árvores na memória via `Visibility.Collapsed` | A troca de abas fica rápida, mas o **consumo de RAM explodiria para 1,5 GB a 2,5 GB** ao abrir 3 ou 4 abas, o que é inaceitável para um gerenciador de arquivos. |
| **Tempo de Abertura** | Desacoplar serviços para pós-startup | O cold start do WinUI 3 + .NET 10 + XAML Desktop nunca será menor do que 2 a 3 segundos. Nunca abrirá em 200 ms como o Explorer ou ferramentas em C++/Delphi. |

---

## 6. Conclusão e Diretriz Definitiva

> [!CAUTION]
> **Veredito:** O **KyraFiles (KyraFiles contributors)** tem uma dívida arquitetural congênita decorrente do framework WinUI 3 / XAML Desktop. O projeto foi desenhado primariamente para estética visual (Fluent design, cantos arredondados, efeitos Mica), sendo **estruturalmente incompatível com as exigências de alta velocidade por teclado, baixo consumo e resposta instantânea do Alexandre**.
>
> **Diretriz para Claude e AGY:** Não insistir em ciclos adicionais de refatoração de performance profunda no KyraFiles. O projeto atinge o limite do próprio framework da Microsoft. Qualquer tentativa adicional continuará caindo no dilema: *ou trava a CPU por recalcular layout, ou consome gigabytes de RAM para manter árvores cacheadas*.

---

## 7. Status do Ambiente

- **Artefatos de Laboratório:** Todos removidos (`C:\KyraFilesUXLab` completamente limpa).
- **Processos:** Todos os processos `KyraFiles.exe` foram encerrados.
- **Executável Oficial:** Preservado no destino único e limpo `%LOCALAPPDATA%\Programs\KyraFiles\KyraFiles.exe`.

