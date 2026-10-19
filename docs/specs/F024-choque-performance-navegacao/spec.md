# F024 — Choque de Performance e Navegação Fluida (Última Tentativa)

## 1. Contexto e Motivação
Na avaliação de 18/10/2026, o Alexandre testou o Files Custom em pastas reais do cotidiano e identificou um cenário inviável para uso diário:
- **Lentidão extrema** na navegação em pastas com cerca de **1.600 arquivos**.
- **Travamento e lentidão ao pressionar teclas de navegação rápida:** `Home`, `End`, `PgUp`, `PgDn` (associadas ou não com `Ctrl`).
- Necessidade de **leitura paginada/sob demanda** durante a navegação para não sobrecarregar a UI.
- Necessidade de **sincronização imediata** com mudanças no sistema de arquivos: scripts externos, backends ou outras janelas criando, removendo ou editando arquivos/pastas na pasta exibida devem refletir em tempo real sem engasgar o aplicativo nem exigir F5 manual.

Esta é a **última tentativa** de viabilização do projeto antes do cancelamento definitivo.

---

## 2. Requisitos Funcionais (FR)
- **FR-001 (Navegação Instantânea em Pastas Grandes):** Em pastas com 1.600+ arquivos, a rolagem e os comandos de teclado rápido (`Home`, `End`, `PgUp`, `PgDn`, `Ctrl+Home`, `Ctrl+End`) devem responder com latência perceptível menor que 100 ms, sem travar a interface.
- **FR-002 (Carga Rápida / Paginada):** A leitura de pastas grandes deve priorizar a exibição imediata do lote inicial visível (viewport), processando o restante sem bloquear o dispatcher da UI e sem emitir resets repetitivos de coleção (`NotifyCollectionChangedAction.Reset`).
- **FR-003 (Sincronismo Imediato do FileSystemWatcher):** Alterações externas (criação, exclusão, renomeação de arquivos) feitas por processos concorrentes devem ser refletidas na exibição em tempo real (< 200 ms) pontualmente no item afetado, sem reiniciar a enumeração de todos os 1.600 itens.
- **FR-004 (Contenção Radical de CPU e RAM):** 
  - CPU parado em repouso absoluto ≤ 2% de 1 núcleo (eliminando os 15% residuais atuais).
  - Remoção definitiva de loops contínuos de composição na Home/Widgets e abas inativas.
  - Working set contido sem vazamentos progressivos durante navegação rápida.
- **FR-005 (Higiene Absoluta de Testes):** Todo e qualquer arquivo, pasta ou script criado para testes automatizados deve ser registrado em `test_artifacts_registry.md` e destruído com verificação comprobatória ao fim.

---

## 3. Critérios de Aceite (AC)
- **AC-001 (Bateria de Navegação por Teclado):** Em pasta de laboratório com 1.600 itens (`C:\FilesUXLab\perf\1600`), pressionar `Home`, `End`, `PgUp`, `PgDn`, `Ctrl+Home`, `Ctrl+End` navega até o item de destino em ≤ 150 ms sem congelamento de UI.
- **AC-002 (Sincronismo em Tempo Real sob Carga Externa):** Com a pasta de 1.600 itens aberta na tela, a criação de 5 arquivos e a exclusão de 5 arquivos por um script externo reflete na tela em tempo real sem travar a janela e sem disparar reload total da lista.
- **AC-003 (CPU Parado):** O script `tools\perf\idle.ps1 -WaitS 10` mede consumo de CPU ≤ 200 ms em 10 s (≤ 2,0% de um núcleo).
- **AC-004 (Limpeza Estrita):** A pasta de teste `C:\FilesUXLab\perf\1600` e todos os scripts temporários são removidos sem deixar qualquer rastro no disco.

