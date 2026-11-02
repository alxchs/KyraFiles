# F086 - Instancia unica do aplicativo

Status: APROVADA (Alexandre, 02/11/2026: "preciso que ele seja only instance")

## Por que
Cada processo novo do aplicativo carrega a interface inteira na memoria. Abrir o app duas vezes duplica o consumo, em vez de duplicar so a janela. O aparelho de referencia e fraco; memoria e requisito.

## Historias
- H1 (P1): como usuario, ao abrir o app de novo (atalho, Win+E, menu Iniciar, linha de comando) quero que a janela que ja esta aberta venha para a frente, sem criar outro processo.
- H2 (P1): quando abro uma pasta ou arquivo pelo app ja aberto, quero que ele abra em nova aba na janela existente.
- H3 (P2): se duas aberturas acontecem quase juntas (ex.: Win+E duas vezes), quero ainda um unico processo.

## Cenarios de aceite
- AC-1: Dado o app aberto, Quando o executavel e iniciado sem argumentos, Entao nenhum processo novo permanece e a janela existente vem para a frente.
- AC-2: Dado o app aberto, Quando o executavel e iniciado com um caminho, Entao esse caminho abre em nova aba na janela existente.
- AC-3: Dado o app ainda iniciando, Quando uma segunda abertura chega dentro de 8 s, Entao ela espera o primeiro ficar pronto e redireciona; fica um so processo principal.
- AC-4: Dado a opcao "abrir aba em instancia existente" desligada, Quando o app e aberto de novo, Entao continua sem criar processo novo (a opcao nao cria mais processo).
- AC-5: Dado o processo principal travado, Quando uma nova abertura nao consegue redirecionar em 8 s, Entao o novo processo assume (nunca deixar o usuario sem app).
- AC-6: Dado o dialogo de abrir/salvar do sistema (processo proprio por desenho), Entao ele continua independente e nao e redirecionado.

## Requisitos
- FR-1: no maximo um processo principal do app por sessao do Windows.
- FR-2: toda abertura nao-dialogo redireciona para o principal; vale para as aberturas por linha de comando, protocolo, arquivo e inicio simples.
- FR-3: a opcao "abrir aba em instancia existente" nao decide mais se um processo novo nasce.
- NFR-1: o tempo de abertura do primeiro processo nao piora.

## Fora de escopo
- Processos auxiliares (servidor, indexador, watchdog, atalho de teclado) e o dialogo de abrir/salvar.
- Janelas extras dentro do mesmo processo.
