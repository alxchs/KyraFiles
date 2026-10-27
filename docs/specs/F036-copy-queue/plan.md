# F036 — Fila de cópias com pausar e retomar — PLAN

Status: APROVADO (Claude, 24/10/2026) **nas fases A e B**; a fase C e a mudança do AC-1 dependem da decisão `D-F036-1` (ver §2). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`). É a feature de maior risco da Onda 3: mexe no caminho que copia arquivos do usuário.

## 1. Arquitetura atual (lida no código em 24/10/2026)

- **CONFIRMED** `Utils/Storage/Operations/FilesystemHelpers.cs:304` (`CopyItemsAsync`) e `:447` (`MoveItemsAsync`), mais `CopyItemsFromClipboard` (`:373`): cada um cria o cartão (`StatusCenterHelper.AddCard_Copy/Move`), resolve conflitos (`GetCollisions`), e chama `ShellFilesystemOperations.CopyItemsAsync`. O cartão traz o `CancellationToken` (`banner.CancellationToken`) e o `ProgressEventSource`.
- **CONFIRMED** `ShellFilesystemOperations.cs`: quem copia de fato é o **motor de cópia do shell do Windows** (resultados por item via `CopyEngineResult`, tratamento de `InUse`, `NameTooLong`, `AlreadyExists`, `PropertyLoss`), com recuo para a API `StorageFile` (`FilesystemOperations`, ex.: `file.CopyAsync`, linhas 271/277) quando o caminho é longo.
- **CONFIRMED** `StatusCenterItem` tem `IsCancelable`, `Progress`, `IsSpeedAndProgressAvailable` e o token; **não há** estado "na fila" nem "pausado".
- **INFERRED** o motor do shell **não expõe pausa** ao aplicativo: a operação roda inteira e só aceita cancelar. T0/T7 confirmam por leitura da interface e por spike.
- **INFERRED** `CopyFile2` do Windows aceita pausar pelo callback de progresso (`COPYFILE2_PROGRESS_PAUSE`) e retomar com `COPY_FILE_RESUME_FROM_PAUSE`. É conhecimento de documentação **não verificado aqui**: o spike T7 tem de provar, inclusive entre volumes e em rede.
- **CONFIRMED** o projeto já gera chamadas nativas por CsWin32 (`Files.App.CsWin32/NativeMethods.txt`).

## 2. O problema de fundo e a decisão necessária (`D-F036-1`)

O AC-1 da spec ("pausar uma cópia de 5 GB e a transferência para em até 2 s, retomando do ponto em que parou") **não é alcançável com o motor atual**, porque o motor copia o arquivo inteiro numa chamada. Opções:

- **(a) Só fila + cancelar + ETA + tentar de novo (fase A)**, sem pausa. Baixo risco, entrega AC-2 a AC-6.
- **(b) Pausa entre arquivos (fase B):** a fila alimenta o motor **um item por vez**; "Pausar" vale depois do arquivo corrente. Entrega o AC-1 só para lotes de muitos arquivos; para **um** arquivo grande, a pausa só atua ao fim dele. Custo: uma chamada ao motor por item (medir a sobrecarga).
- **(c) Pausa no meio do arquivo (fase C):** substituir o motor por `CopyFile2` para arquivos grandes. Atende o AC-1 completo, mas troca o motor que cuida de ACL, fluxos alternativos, propriedades, nomes longos e diálogos. Risco alto de regressão e de perda de metadados.

**Recomendação do Claude:** fazer **A**, depois **B**, e deixar **C** só como spike de medição (T7) sem entrar no produto, até haver prova. Isso exige reescrever o AC-1 da spec em dois níveis (pausa entre arquivos garantida; pausa no meio do arquivo só se o spike passar). Mudança de comportamento prometido: **precisa do OK do Alexandre** (registrado como `PENDING DECISION` em `DECISIONS.md` na T1). As tasks T2 a T5 (fase A) **não dependem** dessa decisão.

## 3. Abordagem (fase A)

1. `OperationQueue`: serviço puro e testável. Chave = volume físico de destino (para rede, o servidor); cada chave tem um semáforo de capacidade 1. `WaitAsync(chave, token)` devolve quando for a vez; `Release`. Origem e destino no mesmo disco serializam (FR-002). Discos diferentes rodam juntos (AC-3).
2. Ponto único de entrada: logo depois do cartão ser criado e **antes** de `GetCollisions` ou de chamar o motor, o fluxo faz `await queue.WaitAsync(...)`. Enquanto espera, o cartão mostra "Na fila" (novo estado no `StatusCenterItem`) com o botão Cancelar. Cancelar na fila sai sem copiar e sem tocar a origem (AC-5).
3. Velocidade e tempo restante (FR-003): conferir o que `IsSpeedAndProgressAvailable` já calcula; só acrescentar o que faltar, com média móvel dos últimos segundos.
4. "Tentar de novo só os que falharam" (AC-4): o motor devolve o resultado por item (`copyResult.Items`); ao terminar com falhas, o cartão final guarda a lista e oferece o botão, que reenfileira só esses itens.
5. Fechar o app com operação ativa (AC-6): no fechamento da janela, se houver cartão em andamento ou na fila, mostrar diálogo (esperar, cancelar, ou fechar mesmo).

Fase B (após a decisão): trocar a chamada em lote por um laço de um item por vez, com um `PauseGate` verificado entre itens; o botão "Pausar/Retomar" no cartão. Mover continua apagando a origem **somente** depois de cada item copiado e verificado (FR-004: já é do motor; não pode mudar).

## 4. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001 (pausar/retomar/cancelar/reordenar) | fase B + `StatusCenterItem` (novo estado) + cartão em `StatusCenter` XAML |
| FR-002 (fila por disco) | novo `OperationQueue` (`Services/` ou `Utils/StatusCenter/`) + 3 pontos em `FilesystemHelpers` |
| FR-003 | `StatusCenterItem` / `StatusCenterItemProgressModel` |
| FR-004 | **não mexer** na lógica de apagar a origem do motor |
| NFR-001 | medições em T0 e T6 |
| AC-2, AC-3, AC-5 | `OperationQueue` + cartão "Na fila" |
| AC-4 | resultado por item de `ShellFilesystemOperations` |
| AC-6 | fechamento da `MainWindow` |

**Arquivos a mexer:** `FilesystemHelpers.cs` (só inserir a espera no começo de copiar/mover/colar), `StatusCenterItem.cs`, `StatusCenterHelper.cs`, o XAML do cartão, novo `OperationQueue`, `MainWindow` (fechamento), `Strings/en-US`. **A NÃO mexer:** a lógica interna de `ShellFilesystemOperations`/`FilesystemOperations` (fase A), o apagar-origem do mover, `Directory.Build.props`.

## 5. Riscos
- **Deadlock/travamento da fila** (a operação que segura a vaga morre): `Release` em `finally`, vaga ligada ao token, teste de morte no meio.
- **Operações encadeadas internamente** (renomear de conflito, desfazer) chamam o motor de novo e **não** podem esperar na mesma fila (auto-bloqueio): só os pontos de entrada do usuário esperam.
- **Mesmo disco "lógico", discos físicos diferentes** (partições): começar por volume e medir; refinar por disco físico só se a medição mostrar ganho.
- **Cópia para rede lenta bloqueia a fila de outros destinos:** a chave inclui o servidor, não o volume local.
- **Regressão de memória:** o app não pode segurar arquivo em memória (regra mobile/globais vale também aqui); medir o processo antes e depois.

## 6. Estratégia de teste (por AC, com asserção)

- **Sem entrada global:** a fila e a lógica são testadas por teste unitário e por teste de integração que chamam a camada de operação diretamente (pasta de teste em `%TEMP%\agy\<GUID>\...`, arquivos gerados pelo teste). AC-2, AC-3, AC-5 e AC-4 saem daí, com asserção por tempo e por conteúdo (hash).
- **Interface** (cartão "Na fila", botões, diálogo de fechamento): UI Automation, helper revisado e, quando existir, Sandbox (`~/.dev-method/SANDBOX.md`).
- **Medição de desempenho (NFR-001):** 2 arquivos de 2 GB em sequência e em paralelo no mesmo disco, sem o app (baseline com `robocopy`) e com o app, 3 rodadas, CSV. Memória do processo por `Get-Process` durante a operação.
- **Controle negativo:** com a fila desligada, as duas cópias do mesmo disco **têm** de rodar juntas (o teste prova que mede a diferença).

## 7. Atualização de 25/10/2026 (spike da pausa no meio do arquivo)

O Alexandre pediu para testar uma cópia por conjunto de bytes e, se não desse certo, usar a opção (b). Resultado completo em docs/agents/evidence/F036/spike-copia-por-blocos.md:
- **Cópia por blocos:** pausa em 0,5 a 7,4 ms, memória de 29 MiB, hash igual, retoma até após a morte do processo. **Mas perde** o atributo oculto, o fluxo Zone.Identifier (a marca de baixado da internet, com efeito de segurança) e a data de gravação, e ficou 19% a 42% mais lenta que File.Copy (mediana, ruído alto). **Não passou** a régua de fidelidade.
- **CopyFile2:** pausa em 28 a 35 ms, retomada correta (hash igual em 3 de 3), **paridade com o motor do shell** nos metadados testados, desempenho equivalente. Só foi testado no mesmo volume NVMe e localmente.
- **Decisão (D-F036-1):** **opção (b)** é o compromisso desta versão (instrução do Alexandre). A fase C passa a ser **CopyFile2**, não cópia por blocos, e só entra no produto depois do spike 2 (T8) provar paridade em outro disco, rede, nome longo e propriedades.
- A spec (AC-1) foi reescrita em dois níveis em 25/10/2026.

## 8. Resultado do spike 2 (25/10/2026)
Detalhes em docs/agents/evidence/F036/spike2-copyfile2-paridade.md. CopyFile2 pausa e retoma entre **discos físicos diferentes** (hash igual), **pausa duas vezes**, **cancela na pausa sem deixar arquivo preso**, trata **nome longo** com \\?\ (o motor do shell falha aí) e lê por SMB; esparso e compactado são perdidos por **todos** os métodos, inclusive o shell (paridade). Retomada estável (1,14 a 1,34 s em 6 rodadas). **Sem teste:** destino por SMB, rede real, EFS, propriedades do shell, FAT/exFAT.
**Escopo recomendado da fase C (a decidir pelo Alexandre depois de A e B publicadas):** só arquivos grandes (a partir de ~256 MiB), destino local ou SMB, com recuo automático para o motor atual em qualquer erro; feature separada (F036-C). Custo de integração: percorrer pastas por item, reproduzir o tratamento de resultado por item (InUse, AlreadyExists, PropertyLoss, NameTooLong), registrar histórico de desfazer e apagar a origem do mover só após cada item verificado.

## 9. Fase C aprovada (25/10/2026): desenho da pausa no meio de arquivo grande

Decisão D-F036-2. Implementar **depois** de A e B revisadas.

### 9.1 Onde se encaixa (CONFIRMED no código)
FileOperationsHelpers.CopyItemAsync(fileToCopyPath[], copyDestination[], overwriteOnCopy, ownerHwnd, asAdmin, operationID, progress, progressHandler) (Utils/Storage/Operations/FileOperationsHelpers.cs:534) recebe os caminhos **já resolvidos** (o nome final vem de GetIncrementalName quando não é para substituir), enfileira cada item em ShellFileOperations2 e devolve (bool, ShellOperationResult) com um ShellOperationItemResult { Succeeded, HResult, Source, Destination } por item. O restante do app (ShellFilesystemOperations) trata InUse, NameTooLong, AlreadyExists, PropertyLoss e NotFound **a partir desses HResult**. O mesmo vale para MoveItemAsync (:396). O cancelamento chega ao motor por cancellationToken.Register(CancelOperation, operationID) (ShellFilesystemOperations.cs:69).

### 9.2 Abordagem
1. **Divisão por item, antes do motor:** em CopyItemAsync/MoveItemAsync, separar os itens em (a) **arquivo grande elegível** e (b) o resto. Elegível = é arquivo (não pasta), tamanho ≥ limite (256 MiB por padrão, configurável), destino em volume **NTFS ou ReFS** (checado por GetVolumeInformation, inclusive UNC), sAdmin == false, e, para **mover**, origem e destino em **volumes diferentes** (no mesmo volume o motor renomeia instantaneamente: não passa pela fase C). O resto segue **exatamente** pelo motor atual.
2. **LargeFileCopier (novo, isolado):** copia um arquivo com CopyFile2 (chamada gerada por CsWin32, acrescentando CopyFile2 e as estruturas ao NativeMethods.txt) para um **nome temporário** (<nome final>.<guid>.filespart) na pasta de destino e, ao terminar, **renomeia** para o nome final (MoveFileEx com substituição quando overwriteOnCopy). Isso cumpre o FR-006: nunca aparece arquivo incompleto com o nome final, cancelar apaga o temporário (o spike provou que o parcial não fica preso), e substituir não destrói o arquivo anterior se a cópia for cancelada.
3. **Pausa e retomada:** o callback de progresso devolve COPYFILE2_PROGRESS_PAUSE quando o PauseGate pede; a rotina espera (de forma assíncrona, sem bloquear a thread de interface) e chama CopyFile2 de novo com COPY_FILE_RESUME_FROM_PAUSE. O contador do callback é cumulativo entre retomadas (spike 2).
4. **Resultado no formato existente:** devolver ShellOperationItemResult com os **mesmos códigos** do motor (CopyEngineResult): sucesso, SHARING_VIOLATION_SRC/DEST (para o diálogo "arquivo em uso"), PATH_NOT_FOUND_SRC, ALREADY_EXISTS, acesso negado, disco cheio. Erro **inesperado** devolve HResult = -1 (convenção que o código atual já usa para "tentar de novo pela outra API"), e o item recua para o motor.
5. **Progresso:** bytes processados pelo callback alimentam o mesmo StatusCenterItemProgressModel (T10 confirma os campos); a velocidade e o tempo restante da fase A passam a ter dado real por arquivo.
6. **Mover entre volumes:** só apagar a origem **depois** de o renomear final ter dado certo e de conferir o tamanho; nunca antes (FR-004).

### 9.3 Mapa requisito → código (fase C)
| Requisito | Onde |
|---|---|
| FR-005 | LargeFileCopier + PauseGate (fase B) |
| FR-006 | nome temporário e renomear no fim |
| FR-007 | critérios de elegibilidade na divisão por item |
| AC-1 nível 2 | LargeFileCopier + botão Pausar habilitado para arquivo grande |

**Arquivos a mexer (fase C):** FileOperationsHelpers.cs (só a divisão no início de CopyItemAsync/MoveItemAsync), novo LargeFileCopier, NativeMethods.txt (CopyFile2), configuração do limite, cartão (estado de pausa), Strings/en-US. **A NÃO mexer:** o laço do motor ShellFileOperations2, o tratamento de diálogos de ShellFilesystemOperations, a lógica de colisão (GetCollisions).

### 9.4 Riscos específicos da fase C
- **Dois caminhos de cópia** com comportamento diferente: por isso o teste de **paridade** (T16) compara os dois para cada atributo medido no spike e para o resultado por item.
- **Temporário órfão** se o app morrer no meio: não limpar sozinho nesta versão (apagar automaticamente um arquivo do usuário é pior); registrar em log e documentar; avaliar limpeza segura depois.
- **Atalhos de segurança:** Zone.Identifier, atributos e data de gravação são preservados (spike); data de criação e ACL própria **não** são, igual ao motor.
- **Antivírus** varrendo o temporário: pode deixar a retomada lenta; medir.
- **Destino em SMB:** só a **leitura** foi testada. Até a T16 provar gravação por SMB, tratar SMB como **não elegível** (recuo ao motor); ampliar depois com prova.
- **Pasta inteira:** continua pelo motor (FR-007).

### 9.5 Estratégia de teste (por AC, com asserção)
Sem entrada global: testes de integração chamam LargeFileCopier e o par CopyItemAsync/MoveItemAsync diretamente em pasta de teste em %TEMP%\agy\<GUID>\... com arquivos gerados (1 e 2 GiB), conferindo **SHA-256**, estado do destino em cada instante (nada com o nome final até o fim), e o ShellOperationResult devolvido. Casos: pausa simples, pausa dupla, cancelar na pausa (temporário some, anterior intacto), retomar após pausa longa, arquivo em uso, origem sumida, elevação (recua), destino FAT (recua), mover entre volumes (origem só some no fim). Interface (botão Pausar habilitado para arquivo grande, estados) por UI Automation com o helper revisado ou Sandbox. Desempenho: 3 rodadas contra o motor atual (T0), com memória por Get-Process. Controle negativo: arquivo abaixo do limite **não** passa pelo caminho C.
## 10. Atualização de 25/10/2026 (resultado da T0, revisão do Claude)
Baseline `robocopy` (2 arquivos de 2 GiB, mesmo NVMe, 3 rodadas): sequencial 9,19 / 9,84 / 7,86 s; paralelo 9,70 / 8,27 / 26,14 s. **OBSERVED, n = 3:** em duas rodadas empatam; o ganho médio de 39% vem de uma única rodada atípica. O ganho da fila em NVMe local **não está provado**; para disco rotativo e rede precisa de ≥ 10 rodadas. A T2 da fila **mede antes de afirmar ganho** e a medição em rede (NAS) está em `docs/agents/evidence/F036/results3-*.csv`.

## 11. Resultado do spike 3 na pasta de rede (25/10/2026)

Detalhes em docs/agents/evidence/F036/spike3-nas.md. Pasta de teste: \\hd-roteador\sda1\temp (NAS do roteador; volume reporta **NTFS**, ACL sim, **fluxos alternativos não**, 173 GiB livres).
- **Envio do computador para o NAS (782 MiB, origem local somente leitura):** CopyFile2 direto, File.Copy, pausa dupla (aos 200 e 500 MiB), cancelar na pausa, nome temporário e renomear, e renomear substituindo: **todos com SHA-256 idêntico** (9 de 9). Velocidade igual entre CopyFile2 e File.Copy (22 a 25 MiB/s, o limite do NAS).
- **NAS para NAS, 50 MB:** CopyFile2 funciona (2 de 2). **256 MiB:** File.Copy terminou; CopyFile2 falhou **uma vez** com  x8007003B (erro de rede inesperado) aos 65 s, deixando arquivo incompleto. **n = 1: intermitente, não caracterizado.** Por isso o recuo ao motor em qualquer erro do CopyFile2 (já previsto) é obrigatório, e o temporário .filespart evita o arquivo incompleto com o nome final.
- **NOVO risco, achado pelo teste:** copiar um arquivo **com** Zone.Identifier para o NAS **não dá erro** em CopyFile2 nem em File.Copy: o fluxo é descartado em silêncio. O motor do shell, em compensação, mostra o aviso de propriedades que não serão copiadas.
- **Nova regra de elegibilidade (FR-008):** antes de usar a fase C, FindFirstStreamW na origem: se houver fluxo além do principal **e** o destino não suportar fluxos (GetVolumeInformation sem FILE_NAMED_STREAMS, 0x40000), o item recua para o motor. Origem sem fluxos extras segue pela fase C normalmente (é o caso comum de vídeo, imagem de disco, arquivo compactado gerado localmente). **SMB deixa de ser "não elegível por padrão" e passa a ser elegível com esta condição**, porque o envio por SMB foi provado.