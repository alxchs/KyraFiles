# SPEC Técnica — Diferenciais para um Gerenciador de Arquivos baseado no Files

**Versão:** 1.2  
**Status:** Proposta para evolução incremental de projeto existente  
**Stack-alvo:** C# / .NET, respeitando a stack real encontrada no repositório  
**Plataforma prioritária:** Windows  
**Execução:** Claude + Antigravity (AGY) + responsável pelo produto  
**Objetivo:** especificar diferenciais funcionais e técnicos sem reescrever nem desestabilizar o projeto já existente.

---

## 0. Contexto, objetivo e premissas

O projeto **já existe e está bem avançado**. Sua base foi obtida do repositório público do Files no GitHub; desde então, foram feitas correções, melhorias e implementações próprias. Portanto, esta SPEC não autoriza iniciar um produto novo, substituir a arquitetura existente ou reimplementar recursos que já funcionam.

O objetivo é definir e planejar diferenciais que agreguem valor ao código existente, mantendo as correções, adaptações, funcionalidades próprias e a identidade atual do aplicativo.

### 0.1. Premissas obrigatórias

1. O repositório local é a fonte primária para descobrir o estado real do projeto.
2. O código existente deve ser compreendido antes de qualquer mudança arquitetural.
3. Nenhum arquivo, componente, recurso, correção ou alteração local poderá ser descartado por presumir que a versão upstream é superior.
4. A implementação deverá reutilizar serviços, modelos, convenções, componentes e infraestrutura existentes quando forem adequados.
5. C#/.NET é a stack desejada, mas a versão do runtime, o framework de UI e as bibliotecas deverão ser confirmados no repositório antes de decisões que impliquem migração.
6. Não se deve trocar WinUI 3, WPF, Avalonia ou qualquer outra tecnologia apenas por preferência. A tecnologia já usada pelo projeto é o padrão inicial, salvo evidência concreta de que não atende a um requisito.
7. A implementação será incremental: uma tarefa delimitada por vez, com build, testes, revisão do diff e documentação.
8. Recursos já existentes total ou parcialmente deverão ser estendidos, corrigidos ou integrados — nunca duplicados por falta de investigação.
9. As sete áreas desta SPEC representam funcionalidades desejadas; sua prioridade final deverá considerar o estado atual do código, custo, dependências e valor agregado.
10. Qualquer decisão que possa afetar arquitetura, compatibilidade ou dados existentes deverá ser proposta antes de ser implementada.

### 0.2. Resultado esperado

Evoluir o gerenciador com os seguintes diferenciais:

1. Engine de sincronização e comparação de diretórios por conteúdo/hash.
2. Conectividade FTP, FTPS e SFTP integrada à navegação e à fila.
3. Engine de scripting interno para automação.
4. Renomeio em massa avançado com RegEx e metadados.
5. Gerenciador de fila de transferências em segundo plano.
6. Visualizador/editor hexadecimal para arquivos grandes.
7. Split/Combine com validação de integridade.

O sistema deve conservar estabilidade, usabilidade e recursos já entregues pelo projeto-base.

---

## 1. Método de trabalho: primeiro descobrir, depois alterar

Antes de implementar qualquer módulo, Claude deverá auditar o repositório atual e identificar exatamente o que já está pronto, o que está parcial e o que falta. A auditoria não deve presumir uma árvore de projetos nem exigir que o aplicativo seja reorganizado de imediato.

### 1.1. Inventário obrigatório

- Solução, projetos, target frameworks e versões do SDK.
- Framework de UI e arquitetura de navegação atual.
- Serviços de arquivos, abstrações, modelos e utilitários já existentes.
- Serviços assíncronos, mecanismos de cancelamento e atualização de progresso.
- Sistema de configurações, persistência, logs e tratamento de erros.
- Dependências NuGet, versões, licenças e avisos de vulnerabilidade.
- Testes existentes, cobertura relevante e modo de execução.
- Fluxo de build, empacotamento, instalação e distribuição.
- Alterações locais não commitadas, branches e estado do Git.
- Relação com o upstream: remote, branch-base, versão/commit de origem e divergências relevantes, se disponíveis.
- Funcionalidades corrigidas ou desenvolvidas localmente, identificadas por código, commits, documentação ou relato do responsável.

### 1.2. Matriz de situação por requisito

Cada requisito da SPEC deverá receber um dos seguintes estados:

- `EXISTS`: já está implementado e passou pela validação necessária.
- `PARTIAL`: existe implementação, mas faltam casos, integração, robustez ou testes.
- `MISSING`: não existe implementação identificada.
- `BLOCKED`: depende de decisão, infraestrutura ou outro módulo.
- `NEEDS_REVIEW`: parece existir, mas ainda não foi verificado.
- `NOT_APPLICABLE`: não se aplica à arquitetura atual, com justificativa documentada.

Um recurso não pode ser marcado `EXISTS` apenas porque há uma classe com nome semelhante. Deve haver evidência de comportamento, integração e testes proporcionais ao risco.

### 1.3. Auditoria do upstream e licença

Como o projeto foi derivado do Files disponível no GitHub, verificar a licença aplicável à versão efetivamente utilizada, os avisos de copyright, as exigências de distribuição e a forma correta de preservar atribuições. Não presumir que a licença atual da branch principal seja necessariamente a licença do commit originalmente utilizado.

Quando viável, documentar as diferenças entre upstream e fork sem tentar sincronizar ou sobrescrever automaticamente as alterações locais. Atualizações do upstream devem ser tratadas como trabalho independente, com análise de conflito e testes.

---

## 2. Diretrizes de arquitetura e integração

### 2.1. Princípio central

**Estender a arquitetura existente em vez de impor uma arquitetura nova.** O projeto provavelmente já possui decisões de UI, navegação, estado, configurações e serviços. Claude deverá primeiro localizar essas decisões e reutilizá-las quando forem adequadas.

A proposta lógica de separação é:

- **UI existente:** painéis, comandos, menus, diálogos, atalhos e visualização de progresso.
- **Serviços/aplicação:** casos de uso, orquestração, validação, eventos e execução das operações.
- **Domínio/modelos:** classificação de diferenças, estados, políticas e resultados independentes da UI.
- **Infraestrutura:** filesystem local, rede, hashing, persistência, credenciais e bibliotecas externas.

Esses limites são conceituais. Não é obrigatório criar novos projetos ou assemblies para cada camada. Qualquer separação física deverá ser justificada pelo acoplamento atual, testes e custo de manutenção.

### 2.2. Regras de integração

- Utilizar o padrão atual de injeção de dependência, navegação, comandos e notificações, se existente e adequado.
- Não introduzir um segundo mecanismo de eventos, logs, configurações ou DI sem justificativa.
- Não adicionar bibliotecas só porque são candidatas nesta SPEC.
- Não implementar operações pesadas na thread da UI.
- Usar APIs assíncronas e `CancellationToken` onde aplicável.
- Para operações longas, apresentar progresso, estado atual, resultado e erro compreensível.
- Manter recursos novos coerentes com o visual, acessibilidade, atalhos e comportamento geral do aplicativo.
- Evitar dependências circulares entre módulos.
- Não fazer chamadas diretas à UI a partir de serviços de background; publicar estado/eventos por mecanismos compatíveis com o projeto.
- Não persistir segredos em texto puro, em logs ou em arquivos de configuração sem proteção.
- Não marcar dados como íntegros/concluídos antes de validar os requisitos aplicáveis.

### 2.3. Interfaces de serviço: exemplos, não contratos aprovados

Uma abstração comum de filesystem pode ser útil se a arquitetura atual não oferecer algo equivalente. Antes de criar uma nova, verificar os tipos e serviços existentes.

```csharp
public interface IFileProvider
{
    string ProviderId { get; }
    Task<IReadOnlyList<FileEntry>> ListDirectoryAsync(
        string path, CancellationToken cancellationToken);
    Task<FileMetadata> GetMetadataAsync(
        string path, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(
        string path, CancellationToken cancellationToken);
}
```

Esse contrato é apenas ilustrativo. Escrita, renomeio, exclusão, movimentação, leitura aleatória e capacidades específicas de cada provedor devem ser modelados conforme os requisitos reais. Não presumir que todos os protocolos oferecem capacidades idênticas.

Da mesma forma, uma abstração de integridade poderá centralizar os hashes usados por Sync, Queue e Split/Combine:

```csharp
public interface IIntegrityService
{
    Task<HashResult> ComputeAsync(
        Stream stream,
        HashAlgorithmId algorithm,
        CancellationToken cancellationToken);
}
```

---

## 3. Módulo 01 — Engine de sincronização e comparação de diretórios

**ID:** `MOD-SYNC`  
**Prioridade preliminar:** alta, sujeita à auditoria do que já existe no projeto.

### 3.1. Objetivo

Comparar árvores de diretórios e, conforme a política escolhida, executar cópia, atualização, espelhamento ou sincronização bidirecional. A comparação deverá ir além de data de modificação e tamanho, permitindo verificar igualdade de conteúdo por hash.

### 3.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| SYNC-001 | Inventariar recursivamente arquivos e diretórios | Entradas listadas e erros de acesso reportados por caminho |
| SYNC-002 | Comparar caminhos, tipos, tamanhos e timestamps | Classificação reproduzível de diferenças |
| SYNC-003 | Calcular MD5, SHA-1 e SHA-256 | Resultado reproduzível com algoritmo registrado |
| SYNC-004 | Comparar conteúdo por hash | Detectar conteúdo diferente mesmo que tamanho/data coincidam |
| SYNC-005 | Exibir diff lado a lado | Mostrar itens iguais, diferentes, exclusivos e conflitantes |
| SYNC-006 | Atualizar em uma direção | Copiar apenas o necessário segundo a política selecionada |
| SYNC-007 | Espelhar origem no destino | Destino corresponder à origem, incluindo órfãos somente se autorizados |
| SYNC-008 | Sincronizar bidirecionalmente | Alterações em ambos os lados e conflitos identificados explicitamente |
| SYNC-009 | Aplicar filtros | Filtros por caminho, extensão, glob/padrão e tamanho |
| SYNC-010 | Gerar relatório | Registrar ações previstas/executadas, resultados e erros |
| SYNC-011 | Oferecer dry-run | Exibir as ações previstas sem alterar arquivos |
| SYNC-012 | Reutilizar hashes em cache | Reutilização apenas quando as regras de invalidação forem suficientes |
| SYNC-013 | Interromper e retomar análise | Retomada não deve assumir como válido um resultado incompleto |
| SYNC-014 | Comparar destinos remotos, se suportado | Respeitar as capacidades efetivas do provedor remoto |

### 3.3. Modelo mínimo de entrada

Cada entrada deverá conter, conforme aplicável:

- Caminho absoluto e/ou relativo à raiz.
- Tipo de item: arquivo, diretório, link simbólico/junção ou outro tipo explicitamente suportado.
- Tamanho em bytes.
- Timestamp e precisão disponível.
- Identidade do provedor e raiz de origem.
- Hash, algoritmo, origem do resultado e validade do cache.
- Estado de leitura, erros e metadados relevantes.
- Classificação de comparação e ação sugerida.

Estados sugeridos para o diff:

- `EQUAL`
- `CONTENT_DIFFERENT`
- `SOURCE_ONLY`
- `DESTINATION_ONLY`
- `TYPE_CONFLICT`
- `METADATA_DIFFERENT`
- `ACCESS_ERROR`
- `UNKNOWN`

Igualdade por conteúdo só pode ser declarada quando o hash estiver disponível e comparável em ambos os lados. Metadados iguais não provam igualdade bit a bit.

### 3.4. Requisitos de integridade e segurança

- SHA-256 será o algoritmo padrão sugerido para verificação de conteúdo.
- MD5 e SHA-1 poderão existir por compatibilidade, mas não devem ser apresentados como opções criptográficas seguras.
- Modo rápido poderá usar tamanho e metadados para triagem; não poderá afirmar igualdade do conteúdo sem evidência suficiente.
- Deve existir modo de auditoria completa, sem confiar em cache de hashes.
- Erros de acesso não podem ser interpretados como arquivos ausentes.
- O usuário deve confirmar visualmente a origem e o destino antes de um espelhamento destrutivo.
- Exclusão de órfãos deve ser desativada por padrão até que uma política seja selecionada e confirmada.
- Links simbólicos, junctions e pontos de montagem precisam de política explícita para evitar sair da raiz planejada.
- Conflitos bidirecionais não podem ser resolvidos silenciosamente por “mais recente” sem política claramente configurada.
- O plano deve ser revisável antes de aplicar mudanças.
- A gravação deve evitar que arquivo parcial seja reportado como concluído; sempre que apropriado, copiar para temporário e finalizar após validação.

### 3.5. Desempenho

Definir benchmarks após identificar o hardware-alvo e o comportamento do projeto atual. Metas iniciais para avaliação, não garantias:

- Inventário local de 100 mil entradas em até 10 segundos em SSD de referência.
- SHA-256 de 10 GB em até 120 segundos em hardware de referência.
- Interface responsiva durante inventário, comparação e hashing.
- Limite ajustável de operações concorrentes para evitar saturação de disco.

Registrar sistema operacional, CPU, RAM, dispositivo, filesystem, número e tamanho dos arquivos, cache quente/frio e configuração do app.

### 3.6. Testes de aceite

1. Alterar conteúdo preservando tamanho e data; a auditoria completa detecta a diferença.
2. Criar/remover arquivos em ambos os lados e validar o diff.
3. Simular sincronização e comprovar que nada foi modificado.
4. Interromper uma cópia; arquivo incompleto não será considerado concluído.
5. Testar conflito bidirecional e comprovar que nenhuma versão foi sobrescrita silenciosamente.
6. Simular erro de acesso e comprovar que ele não gera uma exclusão.
7. Testar filtros, Unicode, caminhos longos e links conforme a política aprovada.

---

## 4. Módulo 02 — Conectividade nativa FTP, FTPS e SFTP

**ID:** `MOD-REMOTE`

### 4.1. Objetivo

Permitir navegar em servidores remotos pelos painéis do próprio gerenciador, integrando transferências remotas ao mecanismo comum de operações, sem depender do Windows Explorer como ferramenta intermediária.

### 4.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| REM-001 | FTP | Conectar, listar, baixar, enviar e desconectar |
| REM-002 | FTPS | TLS explícito e/ou implícito, conforme capacidade aprovada |
| REM-003 | SFTP | Autenticação por senha e chave privada |
| REM-004 | Navegação remota | Caminhos, listagens, seleção e metadados possíveis no protocolo |
| REM-005 | Credenciais seguras | Segredos protegidos e não exibidos em logs |
| REM-006 | Sessões | Abrir, encerrar, identificar e recuperar sessão quando possível |
| REM-007 | Keep-alive | Configurável, sem tráfego desnecessário |
| REM-008 | Reconexão | Detectar queda e aplicar política explícita de reconexão |
| REM-009 | Resume | Retomar somente quando o servidor/protocolo e o estado do arquivo permitirem |
| REM-010 | Integração com fila | Transferências remotas aparecem no mesmo Queue Manager |
| REM-011 | Integridade | Verificar tamanho e hash quando houver acesso suficiente aos dados |
| REM-012 | Erros específicos | Distinguir timeout, autenticação, certificado, host key e perda de rede |

### 4.3. Contratos e capacidades

Cada provedor remoto deverá informar capacidades, por exemplo:

- Listagem e consulta de metadados.
- Leitura e escrita sequencial.
- Leitura aleatória, se disponível.
- Renomeio atômico, se garantido.
- Criação e exclusão de diretórios/arquivos.
- Preservação de timestamp e permissões.
- Suporte a retomada de upload/download.
- Hash remoto, se fornecido pelo servidor.
- Suporte a links e particularidades dos caminhos.

FTP, FTPS e SFTP são protocolos diferentes e não devem compartilhar pressupostos de segurança ou semântica que não sejam válidos em cada um.

### 4.4. Segurança e confiabilidade

- FTPS precisa validar certificados e nome do host; não ignorar erros silenciosamente.
- SFTP deve verificar a chave do host e alertar sobre chaves novas ou alteradas; não aceitar qualquer chave automaticamente.
- Senhas e chaves privadas não podem ser gravadas em texto puro.
- O armazenamento de segredos deve usar a proteção disponível no Windows e respeitar o escopo de usuário.
- Não registrar senhas, tokens, chaves ou conteúdo de arquivos nos logs.
- Tentativas de reconexão devem ter limites, backoff e opção de cancelamento.
- Retomada precisa validar os bytes já presentes e o comportamento real do servidor.
- Um arquivo remoto parcial não pode ser promovido a concluído sem a validação prevista.
- Falhas de rede não podem ser interpretadas como exclusão remota.

### 4.5. Testes de aceite

Usar servidores de teste controlados para autenticação por senha/chave, certificado inválido, host key alterada, timeout, desconexão durante upload/download, reconexão, retomada e cancelamento. Documentar limitações observadas em servidores reais.

---

## 5. Módulo 03 — Engine de automação e scripting interno

**ID:** `MOD-SCRIPT`

### 5.1. Objetivo

Oferecer scripts internos que automatizem operações comuns sem depender de PowerShell ou AutoHotkey para as tarefas centrais. A engine deverá acessar serviços e um contexto de UI controlado, sem manipular livremente objetos internos arbitrários.

### 5.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| SCRIPT-001 | Criar/editar scripts | Editor, validação e persistência |
| SCRIPT-002 | Filtrar arquivos | Por nome, caminho, extensão, tamanho, tipo e metadados disponíveis |
| SCRIPT-003 | Operações em lote | Renomear, copiar, mover e consultar metadados |
| SCRIPT-004 | Variáveis e contexto | Pasta atual, seleção, sessão e parâmetros documentados |
| SCRIPT-005 | Controle de fluxo | Condições, loops e funções conforme linguagem escolhida |
| SCRIPT-006 | Eventos | Registrar handlers para eventos suportados |
| SCRIPT-007 | Atalhos | Executar script por atalho configurável, respeitando conflitos |
| SCRIPT-008 | Logs | Registrar resultados por operação/arquivo sem expor segredos |
| SCRIPT-009 | Cancelamento e timeout | Interromper script e tarefas originadas por ele de modo controlado |
| SCRIPT-010 | Dry-run | Prever operações sem executar alterações |
| SCRIPT-011 | Biblioteca de scripts | Organizar, importar/exportar e habilitar/desabilitar scripts |
| SCRIPT-012 | Diagnóstico | Erros de sintaxe com posição, stack trace sanitizado e contexto |

### 5.3. Eventos candidatos

- `FolderOpened`
- `TransferCompleted`
- `TransferFailed`
- `FileCreated`
- `FileRenamed`
- `SelectionChanged`
- `HotkeyPressed`
- `ApplicationStarted`

A lista final depende dos eventos que a aplicação já possui e da facilidade de emitir notificações consistentes. O payload de cada evento deve ser documentado e estável.

### 5.4. API de script proposta

O script deverá usar serviços de domínio expostos pelo host, por exemplo:

```text
files = fs.list(currentFolder)
selected = files.filterByExtension(".log")

for file in selected:
    queue.copy(file, destinationFor(file))
```

A sintaxe é ilustrativa e não define a linguagem. API candidata:

- `fs`: listar, consultar metadados e executar operações autorizadas.
- `queue`: submeter e consultar tarefas.
- `metadata`: ler metadados suportados.
- `ui`: consultar contexto visível e solicitar ações permitidas.
- `events`: registrar handlers.
- `log`: registrar diagnóstico.
- `context`: seleção, pasta ativa, sessão e parâmetros.

### 5.5. Escolha de engine

Não desenvolver uma linguagem própria inicialmente. Comparar ao menos as seguintes opções em protótipo:

- Lua/MoonSharp: https://github.com/moonsharp-devs/moonsharp
- JavaScript/Jint: https://github.com/sebastienros/jint

Avaliar compatibilidade com o framework atual, manutenção, licença, limite de execução, facilidade para expor a API do host, diagnóstico e consumo de recursos.

### 5.6. Segurança

- Não dar acesso irrestrito ao sistema operacional por padrão.
- Scripts importados ou desconhecidos devem ser identificados e não executados silenciosamente.
- Aplicar timeout, cancelamento e limites para loops/recursos conforme a engine permitir.
- Evitar recursão infinita de eventos; limitar frequência e detectar ciclos.
- Scripts devem respeitar permissões, políticas destrutivas e confirmação de ações.
- Não assumir que um interpretador embutido, por si só, isola scripts hostis.
- Considerar processo isolado ou outro limite de segurança caso seja requisito executar scripts não confiáveis.

### 5.7. Testes de aceite

Criar script que filtre arquivos, produza plano, submeta tarefas e gere relatório. Testar sintaxe inválida, cancelamento, timeout, falta de permissões, execução repetida de evento e prevenção de ciclo de eventos.

---

## 6. Módulo 04 — Renomeio em massa com RegEx e metadados

**ID:** `MOD-RENAME`

### 6.1. Objetivo

Renomear vários arquivos por expressões regulares, grupos capturados, máscaras de substituição e metadados EXIF/ID3, com prévia completa, detecção de conflitos e rollback persistido.

### 6.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| REN-001 | RegEx | Grupos de captura e substituição documentados |
| REN-002 | Máscaras | Reordenar texto e incluir variáveis |
| REN-003 | EXIF | Consultar campos de imagem suportados |
| REN-004 | ID3 | Consultar campos de áudio suportados |
| REN-005 | Preview | Mostrar original e destino antes de executar |
| REN-006 | Colisões | Detectar nomes repetidos e destinos já ocupados |
| REN-007 | Undo/rollback | Restaurar nomes com registro persistido e validação de estado |
| REN-008 | Histórico | Registrar sucessos, falhas, itens ignorados e motivos |
| REN-009 | Execução parcial | Relatar claramente os itens que não puderam ser renomeados |

### 6.3. Máscara de exemplo

Padrão RegEx:

```regex
^IMG_(\d{8})_(\d+)\.jpg$
```

Máscara ilustrativa:

```text
${capture1}_${capture2}_${exif.DateTimeOriginal}.jpg
```

A sintaxe final deve ser aprovada e testada. Utilizar `System.Text.RegularExpressions` inicialmente, salvo necessidade comprovada de outro motor. Aplicar timeout a padrões para evitar que entradas patológicas bloqueiem a operação.

### 6.4. Campos de metadados candidatos

**Imagem:** data/hora original, fabricante, modelo da câmera, orientação, largura, altura e GPS, quando disponível e autorizado.

**Áudio:** título, artista, álbum, número da faixa, ano/data, duração, bitrate, codec e outros campos suportados pela biblioteca selecionada.

O resultado deve distinguir campo ausente, valor inválido e formato não suportado. O comportamento para variável vazia deverá ser configurável (erro, valor padrão ou ignorar item).

### 6.5. Transação e rollback

1. Construir o plano inteiro sem alterar arquivos.
2. Normalizar nomes segundo o sistema de arquivos de destino.
3. Detectar colisões entre itens e com arquivos existentes.
4. Exibir prévia e políticas de conflito.
5. Persistir o plano antes da execução.
6. Executar a sequência segura de renomeações, usando nomes temporários quando necessário para trocas cíclicas.
7. Registrar estado e erro por item.
8. Permitir rollback após reinício.
9. Antes do rollback, verificar se o caminho atual ainda corresponde ao estado esperado.
10. Se outro processo tiver alterado o arquivo, sinalizar conflito em vez de sobrescrever silenciosamente.

### 6.6. Testes de aceite

Testar Unicode, extensões múltiplas, grupos de captura, metadados ausentes, nomes duplicados, case sensitivity, nomes inválidos, diretório protegido, falha no meio e rollback após reinício.

---

## 7. Módulo 05 — Gerenciador de fila de transferências

**ID:** `MOD-QUEUE`

### 7.1. Objetivo

Centralizar cópias, movimentações e transferências locais/remotas em segundo plano, com controle de prioridades, progresso, cancelamento, retomada e limites de recursos.

### 7.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| QUEUE-001 | Centralizar operações | Operações de transferência compatíveis são geridas pela fila |
| QUEUE-002 | Executar em background | UI não bloqueia durante I/O pesado |
| QUEUE-003 | Pausar/retomar | Transições de estado verificáveis |
| QUEUE-004 | Cancelar | Cancelamento controlado e destino parcial identificado |
| QUEUE-005 | Reordenar | Reordenar tarefas ainda não iniciadas |
| QUEUE-006 | Prioridade | Prioridades influenciam o escalonamento |
| QUEUE-007 | Throttling | Limite de banda por tarefa/grupo conforme implementação |
| QUEUE-008 | Persistir fila | Recuperar tarefas pendentes após reinício |
| QUEUE-009 | Progresso | Bytes, velocidade, estado e estimativa quando calculável |
| QUEUE-010 | Integração remota | Tarefas de rede compartilham o modelo operacional |
| QUEUE-011 | Políticas de conflito | Substituir, ignorar, renomear ou consultar conforme a política |
| QUEUE-012 | Verificação final | Validar tamanho/hash quando exigido pela operação |

### 7.3. Modelo de estado

Estados candidatos:

- `QUEUED`
- `RUNNING`
- `PAUSE_REQUESTED`
- `PAUSED`
- `CANCEL_REQUESTED`
- `CANCELLED`
- `COMPLETED`
- `FAILED`
- `NEEDS_ATTENTION`

Definir transições permitidas, incluindo o que ocorre durante o encerramento do aplicativo. Não é obrigatório suportar pausa em todos os tipos de tarefa: a UI deve informar a limitação real.

### 7.4. Dados mínimos da tarefa

- ID persistente.
- Tipo de operação e parâmetros.
- Origem/destino e identificadores de provedor.
- Prioridade e limites de concorrência/banda.
- Estado atual e histórico de transições.
- Progresso, bytes, velocidade e erro.
- Política de sobrescrita, integridade e validação.
- Informações de retomada e destino parcial.
- Resultado final e timestamps relevantes.

### 7.5. Regras

- Cancelar não pode deixar destino parcial marcado como concluído.
- Retomada deve revalidar origem, destino e estado parcial.
- Prioridades não alteram tarefas em execução de forma capaz de corromper dependências.
- Throttling deve ser testável e considerar limites de precisão/latência.
- Tarefas compostas devem registrar as etapas concluídas e as que precisam ser repetidas.
- Em movimentação entre volumes/provedores, não presumir que `move` é atômico; aplicar estratégia de copiar, verificar e só depois excluir a origem, conforme política.
- A fila deve persistir estado com consistência transacional suficiente para recuperação.

### 7.6. Testes de aceite

Executar múltiplas transferências, alterar prioridades, limitar banda, cancelar, pausar/retomar quando suportado, reiniciar o aplicativo durante uma operação e comprovar a recuperação consistente.

---

## 8. Módulo 06 — Visualizador e editor hexadecimal

**ID:** `MOD-HEX`

### 8.1. Objetivo

Inspecionar e editar bytes de qualquer arquivo suportado, inclusive arquivos de vários gigabytes, sem carregar todo o conteúdo em memória.

### 8.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| HEX-001 | Abrir arquivos grandes | RAM utilizada não cresce proporcionalmente ao tamanho total do arquivo |
| HEX-002 | Navegar por offset | Saltar para offsets válidos em decimal/hexadecimal |
| HEX-003 | Exibir hexadecimal | Bytes apresentados corretamente |
| HEX-004 | Painel textual | ASCII e UTF-8 com tratamento claro de bytes inválidos |
| HEX-005 | Editar bytes | Alterações refletem o valor binário pretendido |
| HEX-006 | Salvar bytes brutos | Não converter implicitamente o conteúdo completo |
| HEX-007 | Undo | Desfazer alterações não consolidadas |
| HEX-008 | Detectar alterações externas | Avisar se o arquivo mudar durante a edição |
| HEX-009 | Limitar recursos | Buffer, cache e journal limitados/configuráveis |
| HEX-010 | Recuperar falha | Estratégia definida para interrupção durante gravação |

### 8.3. Requisitos técnicos

- Utilizar acesso aleatório/offset e leitura por blocos.
- Definir tamanho de buffer após benchmark.
- Virtualizar a grade; renderizar somente as linhas visíveis e uma margem controlada.
- A coluna textual será apenas uma representação dos bytes; não pode alterar os bytes só por trocar a codificação de visualização.
- Bytes inválidos em UTF-8 devem ser representados de forma explícita sem corromper dados.
- Manter edição em buffer/journal antes de consolidar.
- Validar identidade, tamanho e estado do arquivo original antes de salvar.
- Proteger o usuário contra salvamento acidental sobre um arquivo que foi modificado por outro processo.
- O método de salvamento (in-place, temporário ou outro) deverá ser decidido por ADR com foco em arquivo grande, espaço livre, preservação de atributos e recuperação.
- Explicar e validar o comportamento para arquivos bloqueados, permissões insuficientes, discos removíveis e falta de espaço.

### 8.4. Testes de aceite

Abrir arquivo de vários GB, navegar para offsets próximos do final, alterar bytes específicos, salvar e comparar independentemente. Testar falta de espaço, bloqueio, alteração externa, cancelamento e interrupção durante gravação.

---

## 9. Módulo 07 — Split/Combine

**ID:** `MOD-SPLIT`

### 9.1. Objetivo

Dividir arquivos em partes de tamanho em bytes configurado e reconstruir o original com validação de sequência e integridade.

### 9.2. Requisitos funcionais

| ID | Requisito | Critério de aceite |
|---|---|---|
| SPLIT-001 | Split por tamanho | Cada parte, exceto a última, tem tamanho exato configurado |
| SPLIT-002 | Nomear partes | Nomenclatura sequencial consistente |
| SPLIT-003 | Combine | Reconstruir na ordem correta |
| SPLIT-004 | Validar conjunto | Detectar ausentes, duplicados, truncados e inconsistentes |
| SPLIT-005 | CRC32 | Validar partes e resultado conforme manifesto |
| SPLIT-006 | Manifesto | Registrar versão, ordem, tamanhos e checksums |
| SPLIT-007 | Retomar/reiniciar | Revalidar estado antes de continuar |
| SPLIT-008 | Preservar original | Não excluir origem automaticamente |
| SPLIT-009 | SHA-256 opcional | Possibilitar auditoria mais forte do arquivo completo |

### 9.3. Manifesto e integridade

O formato do manifesto deve ser versionado e documentado. Campos candidatos:

- Versão do formato.
- Nome lógico do arquivo original.
- Tamanho total em bytes.
- Quantidade e ordem das partes.
- Nome e tamanho esperado de cada parte.
- CRC32 de cada parte e do arquivo reconstruído.
- SHA-256 opcional do original e do resultado.
- Timestamp e informações não sensíveis da operação.

CRC32 detecta erros acidentais; não é garantia de autenticidade nem proteção criptográfica contra adulteração.

### 9.4. Regras

- Nunca combinar partes fora de ordem.
- Não considerar resultado válido se houver parte ausente, duplicada ou corrompida.
- Não sobrescrever destino existente sem política explícita.
- Construir o resultado em arquivo temporário e promovê-lo ao destino após validação, sempre que o filesystem permitir.
- Manter fragmentos até validação final.
- Reportar erros de leitura/escrita e falta de espaço.
- Definir comportamento para arquivo vazio e tamanhos de divisão maiores que o arquivo.

### 9.5. Testes de aceite

Testar arquivo vazio, arquivo menor que uma parte, múltiplo exato do tamanho, última parte parcial, parte corrompida, ausente, duplicada e fora de ordem, bem como falta de espaço durante reconstrução.

---

## 10. Decisões técnicas e bibliotecas candidatas

As opções desta seção são candidatas para verificação. Nenhuma biblioteca fica aprovada automaticamente só por aparecer nesta lista.

### 10.1. Runtime e UI

- Confirmar `TargetFramework`, versão de SDK e dependências atualmente usados.
- Se a base já utiliza WinUI 3, avaliar a continuidade antes de propor migração. O custo e o risco de migração precisam ser justificados por um requisito concreto.
- WPF e Avalonia só devem ser considerados alternativas mediante análise de viabilidade e benefício real. Uma migração de UI não é objetivo desta SPEC.
- Preferir as APIs nativas do .NET para streams, filesystem, hashing, RegEx, cancelamento e concorrência quando forem suficientes.

### 10.2. Rede

- FluentFTP: https://github.com/robinrodricks/FluentFTP — candidato para FTP/FTPS.
- SSH.NET: https://github.com/sshnet/SSH.NET — candidato para SSH/SFTP.

Validar licença, versão, manutenção, target framework, host key/certificados, API assíncrona, retomada e limitações reais antes de aprovar.

### 10.3. Persistência

- Microsoft.Data.Sqlite: https://learn.microsoft.com/dotnet/standard/data/sqlite/ — candidato para histórico, estado da fila, transações de renomeio e manifestos.
- Antes de introduzir banco novo, verificar se o projeto já possui persistência adequada. Evitar manter duas fontes de verdade para configurações ou operações.

### 10.4. Scripting

- MoonSharp: https://github.com/moonsharp-devs/moonsharp
- Jint: https://github.com/sebastienros/jint

Comparar capacidade de limitar execução, manutenção, compatibilidade com a runtime, diagnóstico e facilidade para expor uma API restrita.

### 10.5. Metadados

- MetadataExtractor: https://github.com/drewnoakes/metadata-extractor-dotnet — candidato para metadados de imagens.
- TagLibSharp: https://github.com/mono/taglib-sharp — candidato para tags de áudio.

Confirmar formatos e campos necessários, licença, tamanho das dependências e comportamento com arquivos malformados.

### 10.6. Integridade, RegEx e Hex

- Hash: preferir APIs nativas do .NET e streams; não introduzir dependência sem benchmark que a justifique.
- RegEx: iniciar com `System.Text.RegularExpressions`, aplicando timeout e validação.
- Editor Hex: provável implementação específica integrada à UI existente; evitar biblioteca gráfica antes de comparar maturidade, virtualização, acessibilidade e licença.

### 10.7. Gate de aprovação de dependências

Antes de adicionar um pacote NuGet, documentar:

1. Nome, versão e URL oficial.
2. Licença e obrigações de redistribuição.
3. Compatibilidade com target framework e arquitetura.
4. Estado de manutenção e vulnerabilidades conhecidas.
5. Alternativas existentes no projeto ou no .NET.
6. Benefício concreto e custo de manutenção.
7. Testes de integração necessários.

Fixar versões de forma reprodutível, manter lock file quando apropriado e integrar auditoria de dependências ao build.

---

## 11. ADRs — Architecture Decision Records

Criar ADRs em `docs/ADR/`. O status pode ser `Proposed`, `Accepted`, `Rejected` ou `Superseded`. Decisões estruturais não devem ser consideradas aprovadas sem autorização do responsável pelo produto.

| ADR | Tema | Pergunta principal |
|---|---|---|
| ADR-001 | Runtime .NET | Qual SDK/TFM a base atual utiliza e qual deve ser mantido? |
| ADR-002 | UI existente | O framework atual atende aos diferenciais? Há evidência que justifique mudança? |
| ADR-003 | Limites modulares | Quais serviços/projetos existentes devem ser reutilizados ou separados? |
| ADR-004 | Persistência | Como persistir filas e transações sem duplicar mecanismos existentes? |
| ADR-005 | File provider | Existe abstração apropriada ou deve ser estendida/criada? |
| ADR-006 | Fila e concorrência | Como funcionam pausa, cancelamento, prioridade e recuperação? |
| ADR-007 | FTP/FTPS/SFTP | Quais bibliotecas e garantias são aceitas? |
| ADR-008 | Scripting | Lua/MoonSharp, JavaScript/Jint ou outra opção? |
| ADR-009 | Hash/cache | Como invalidar resultados e quando exigir auditoria completa? |
| ADR-010 | Rename transacional | Como lidar com colisões, nomes temporários e rollback? |
| ADR-011 | Editor Hex | Qual estratégia de buffer, journal e salvamento? |
| ADR-012 | Split/Combine | Qual formato versionado de manifesto e política de integridade? |
| ADR-013 | Distribuição | Como os módulos entram no processo de build/instalação existente? |
| ADR-014 | Logs e privacidade | Quais dados registrar, ocultar ou reter? |
| ADR-015 | Upstream/licença | Como preservar licença e administrar divergências do fork? |

### 11.1. Modelo de ADR

```markdown
# ADR-NNN — Título

Status: Proposed | Accepted | Rejected | Superseded
Data:
Responsável:

## Contexto
Problema, estado atual do código e requisito relacionado.

## Opções avaliadas
A. ...
B. ...

## Critérios
Compatibilidade, desempenho, segurança, manutenção, custo e risco de regressão.

## Decisão
Decisão e justificativa. Se estiver pendente, manter Proposed.

## Consequências
Benefícios, limitações e impacto no código existente.

## Evidências
Diffs, testes, protótipos, benchmarks e fontes verificáveis.

## Revisão futura
Condições que justificariam reavaliar a decisão.
```

---

## 12. Requisitos não funcionais transversais

| ID | Requisito | Verificação |
|---|---|---|
| RNF-001 | UI responsiva | Operações longas sem bloqueio perceptível da interface |
| RNF-002 | Integridade | Validar conteúdo produzido por leitura independente |
| RNF-003 | Segurança | Revisar credenciais, certificados, host keys e ações destrutivas |
| RNF-004 | Recuperação | Interrupção e reinício sem estado falso de conclusão |
| RNF-005 | Escalabilidade | Benchmarks com árvores grandes e arquivos de vários GB |
| RNF-006 | Observabilidade | Logs correlacionados por operação, sem segredos |
| RNF-007 | Testabilidade | Testes reproduzíveis e independentes da UI quando possível |
| RNF-008 | Compatibilidade | Testar versões de Windows e runtime declaradas como suportadas |
| RNF-009 | Manutenibilidade | Contratos documentados e ausência de duplicação desnecessária |
| RNF-010 | Distribuição | Build/instalação/atualização compatíveis com o app atual |
| RNF-011 | Regressão | Testes críticos dos recursos preexistentes continuam passando |
| RNF-012 | Licenciamento | Dependências e avisos compatíveis com a licença do projeto-base |

### 12.1. Definition of Done (DoD)

Uma tarefa não está concluída só porque compila. Deve atender ao seguinte checklist:

- [ ] Requisito identificado por ID da SPEC.
- [ ] Estado anterior do recurso e impactos documentados.
- [ ] Implementação integrada aos padrões existentes.
- [ ] Testes unitários/de integração adequados ao risco.
- [ ] Build executado em configuração documentada.
- [ ] Falhas, cancelamentos e permissões tratados.
- [ ] Logs/diagnóstico implementados sem expor dados sensíveis.
- [ ] Nenhuma regressão conhecida sem registro e aprovação.
- [ ] Documentação, ADRs e rastreabilidade atualizados.
- [ ] Diff revisado e evidências de aceite registradas.
- [ ] Sem alterações locais de terceiros descartadas ou sobrescritas.

---

## 13. Plano de implementação incremental

As fases são orientativas. Após a auditoria, Claude deverá mapear o que já existe, remover do plano o que estiver comprovadamente concluído e reordenar os incrementos conforme dependências reais.

### Fase 0 — Auditoria do projeto existente

- Inspecionar Git, branches, mudanças locais e remote upstream.
- Descobrir stack real, target framework, serviços e padrões atuais.
- Mapear recursos existentes contra esta SPEC.
- Identificar correções e funcionalidades próprias a preservar.
- Verificar licença da base original e das dependências.
- Criar matriz de risco e propostas de ADR.
- **Gate:** responsável aprova a matriz de situação e as decisões que afetam arquitetura.

### Fase 1 — Infraestrutura mínima necessária

Implementar somente o que estiver ausente:

- Build e testes reproduzíveis.
- Logging e tratamento de erros consistente.
- Contratos para operação longa, cancelamento e progresso.
- Persistência de estado, se necessária.
- Regras de dependência entre serviços e UI.
- **Gate:** caminho de build/teste documentado e nenhuma regressão crítica.

### Fase 2 — Primeiro diferencial de ponta a ponta

Recomendação inicial: inventário local, hash SHA-256 e comparação em modo somente leitura/dry-run.

- Reutilizar serviços e componentes existentes.
- Comparar duas árvores e classificar diferenças.
- Exibir relatório no padrão visual atual.
- Testar conteúdo diferente com tamanho e data iguais.
- Medir responsividade e desempenho inicial.
- **Gate:** comparação confiável e nenhum arquivo alterado durante dry-run.

### Fase 3 — Queue Manager e operações seguras

- Auditar mecanismos de cópia/movimentação já implementados.
- Integrar ou evoluir a fila existente, evitando uma segunda fila paralela.
- Adicionar progresso, cancelamento, prioridades e persistência conforme escopo.
- Validar destino parcial, conflitos e recuperação.
- **Gate:** interrupção e reinício não produzem falsa conclusão nem perda silenciosa.

### Fase 4 — Sincronização aplicada

- Atualização em uma direção.
- Plano de espelhamento revisável.
- Exclusões de órfãos com proteção explícita.
- Conflitos bidirecionais, somente após consolidar as regras de comparação.
- **Gate:** testes destrutivos executados primeiro em fixtures isoladas.

### Fase 5 — Rede remota

- Selecionar bibliotecas após aprovação dos ADRs.
- Validar FTP/FTPS/SFTP com servidores de teste.
- Integrar navegação e Queue Manager.
- Validar credenciais, host keys/certificados e retomada real.
- **Gate:** testes de segurança e recuperação documentados.

### Fase 6 — Renomeio avançado

- Implementar/estender RegEx e máscaras.
- Integrar metadados EXIF/ID3.
- Preview, colisões e transação/rollback.
- **Gate:** rollback após reinício e conflito externo tratados.

### Fase 7 — Split/Combine

- Definir formato do manifesto.
- Fragmentação por bytes exatos.
- Reconstrução com ordem e CRC32 validados.
- SHA-256 opcional do arquivo completo.
- **Gate:** corrupção/ausência de parte sempre detectada.

### Fase 8 — Editor hexadecimal

- Acesso aleatório, buffer e virtualização.
- Visualizações Hex e texto.
- Buffer de edição, undo e salvamento recuperável.
- **Gate:** edição validada em arquivo de vários GB e recuperação de falhas testada.

### Fase 9 — Scripting interno

- Comparar engines com tarefas reais.
- Publicar API limitada e documentada.
- Eventos, atalhos, logs, timeout e cancelamento.
- Isolamento adicional se houver execução de scripts não confiáveis.
- **Gate:** script demonstrativo automatiza um fluxo real com permissões verificadas.

### Fase 10 — Hardening e distribuição

- Regressão completa de recursos existentes.
- Benchmarks documentados.
- Testes de estresse e recuperação.
- Revisão de licenças e vulnerabilidades.
- Integração com instalação/atualização existentes.
- **Gate:** release candidata reproduzível e relatório de aceite.

---

## 14. Regras de colaboração entre Claude e Antigravity (AGY)

### 14.1. Responsabilidades

**Claude — arquitetura e revisão técnica**

- Inspecionar o código real antes de propor alterações.
- Traduzir requisitos em tarefas pequenas e rastreáveis.
- Propor ADRs, comparar alternativas e explicar impactos.
- Revisar o diff produzido por AGY, os testes e as evidências.
- Atualizar a SPEC quando requisitos forem aprovados para mudança.
- Identificar regressões, duplicação e contratos inconsistentes.

**AGY — execução de tarefas delimitadas**

- Trabalhar somente em uma tarefa com escopo e critérios de aceite definidos.
- Verificar o estado do Git e alterações existentes antes de editar.
- Implementar o menor incremento funcional adequado.
- Rodar build e testes aplicáveis e registrar os resultados reais.
- Informar arquivos alterados, decisões, limitações e bloqueios.
- Não modificar contratos centrais sem autorização.

**Responsável pelo produto**

- Aprovar ADRs relevantes, prioridades e alterações de escopo.
- Validar entregas de produto e trade-offs.
- Preservar a visão dos diferenciais e as decisões sobre comportamento.

Os papéis podem mudar em uma tarefa específica, mas deve sempre haver revisão independente suficiente para alterações de risco elevado.

### 14.2. Regras operacionais

1. Selecionar uma tarefa cujo pré-requisito esteja satisfeito.
2. Registrar ID da tarefa, requisitos, estado inicial e critérios de aceite.
3. Inspecionar os arquivos e serviços existentes antes de editar.
4. Evitar tarefas paralelas que editem os mesmos arquivos/contratos.
5. Implementar a menor alteração que satisfaça os critérios.
6. Executar testes e build reais; nunca inventar resultados.
7. Revisar o diff e preservar alterações locais preexistentes.
8. Atualizar a rastreabilidade e documentação.
9. Integrar a tarefa somente após aprovação dos critérios.
10. Selecionar a próxima tarefa liberada pelas dependências.

### 14.3. Relatório obrigatório de entrega

```text
TASK-ID:
REQUISITOS:
ESTADO ANTES DA TAREFA:
RESUMO:
ARQUIVOS CRIADOS/ALTERADOS:
DECISÕES/DEPENDÊNCIAS:
BUILD EXECUTADO:
TESTES EXECUTADOS E RESULTADOS:
CRITÉRIOS DE ACEITE COMPROVADOS:
LIMITAÇÕES/RISCOS:
REGRESSÕES IDENTIFICADAS:
DOCUMENTAÇÃO ATUALIZADA:
ESTADO DO GIT:
PRÓXIMA TAREFA SUGERIDA:
```

### 14.4. Condições para bloquear uma tarefa

Bloquear apenas a parte afetada quando:

- Uma decisão de arquitetura necessária ainda não estiver aprovada.
- O teste expuser risco de perda/corrupção de dados.
- A tarefa exigir descartar alterações locais.
- O requisito conflitar com a licença ou condições de segurança.
- Não houver forma de validar uma operação destrutiva com segurança.

Tarefas independentes que não dependam desse bloqueio podem continuar.

---

## 15. Prompt persistente para Claude

Salvar este conteúdo no arquivo de instruções do projeto que Claude realmente utilize. Confirmar o local efetivo de instruções no ambiente antes de depender dele.

```text
PAPEL: Arquiteto e coordenador técnico de um gerenciador de arquivos existente baseado no Files, com melhorias locais já realizadas.

A fonte de verdade funcional é docs/SPEC.md. As decisões aprovadas ficam em docs/ADR/. O projeto NÃO deve ser refeito do zero.

Antes de qualquer implementação:
1. Inspecione o repositório, o estado do Git e as alterações locais.
2. Identifique a stack e a arquitetura reais; não suponha a estrutura de projetos.
3. Mapeie o requisito para código existente usando EXISTS, PARTIAL, MISSING, BLOCKED ou NEEDS_REVIEW.
4. Localize serviços, modelos, componentes e testes reutilizáveis.
5. Defina uma tarefa pequena com critérios de aceite e testes.

Regras:
- Preserve correções, funcionalidades e alterações locais já existentes.
- Não troque framework, arquitetura ou biblioteca sem ADR e justificativa concreta.
- Não duplique serviços existentes por falta de inspeção.
- Não declare algo pronto sem evidência de código integrado e testes reais.
- Não remova testes para mascarar falhas.
- Não introduza dependências sem validar licença, versão, manutenção e benefício.
- Para operações destrutivas, exija escopo validado, preview quando aplicável e proteção contra perda de dados.
- Não registre credenciais ou conteúdo sensível em logs.
- Faça revisão do diff de AGY; não aceite somente o relato de conclusão.

Cada entrega deve informar requisitos atendidos, arquivos alterados, build/testes executados, evidências, riscos, limitações e próxima tarefa.

Trabalhe em incrementos pequenos. Se uma decisão importante estiver pendente, documente alternativas e aguarde aprovação somente para a parte afetada. Continue tarefas independentes liberadas.
```

---

## 16. Primeira tarefa executável — TASK-000

**Título:** Auditoria do código existente e mapeamento dos diferenciais  
**Objetivo:** determinar o que já está implementado, o que precisa ser estendido e qual diferencial oferece o melhor próximo incremento.

### Instruções para Claude/AGY

1. Ler esta SPEC e inspecionar todo o repositório relevante.
2. Registrar stack, target framework, UI, padrões de navegação, DI, logging, persistência, serviços de arquivos e testes.
3. Inspecionar estado do Git, branches, alterações locais e remotes. Não fazer reset, checkout destrutivo, clean ou sincronização automática com upstream.
4. Verificar os commits e a licença da base upstream quando a informação estiver disponível.
5. Mapear cada requisito `SYNC-*`, `REMOTE-*`, `SCRIPT-*`, `REN-*`, `QUEUE-*`, `HEX-*` e `SPLIT-*` para o código atual.
6. Listar componentes que podem ser reutilizados e pontos de extensão já disponíveis.
7. Identificar risco de regressão e dependências críticas.
8. Preparar ADRs em estado `Proposed` apenas para decisões realmente necessárias.
9. Ordenar os incrementos por dependência, risco, custo e valor diferencial.
10. Recomendar uma primeira entrega funcional pequena, sem presumir que precise ser o Sync se outro módulo tiver base muito mais madura.

### Entregáveis

- Inventário técnico do projeto.
- Matriz requisito → estado → evidência no código/testes.
- Lista de correções e funcionalidades existentes a preservar.
- Mapa de dependências dos módulos.
- Avaliação de bibliotecas já utilizadas antes de sugerir novas.
- ADRs propostos com opções e consequências.
- Backlog incremental ordenado.
- Primeira tarefa recomendada, com arquivos candidatos, testes e critérios de aceite.

### Critérios de aceite

- O relatório deve se apoiar no código real, não em pressupostos.
- Funcionalidades existentes não devem ser reimplementadas sem necessidade demonstrada.
- Alterações locais não podem ser descartadas.
- Decisões pendentes devem continuar identificadas como pendentes.
- Nenhum benchmark ou teste pode ser relatado como executado sem ter sido realmente executado.
- Nenhum módulo novo precisa ser criado nesta tarefa se não houver justificativa.

**Aguardar aprovação para decisões arquiteturais relevantes.** A auditoria e o planejamento podem ser concluídos antes dessa aprovação.

---

## 17. Critério para escolher o próximo diferencial

Após TASK-000, atribuir a cada recurso uma avaliação simples (1–5) para:

- Valor diferenciado para o usuário.
- Quanto do código atual já pode ser aproveitado.
- Risco de perda/corrupção de dados.
- Complexidade de integração com a UI.
- Dependências e bloqueios.
- Esforço de teste e manutenção.

A priorização não deve depender apenas do número de linhas ou da aparente facilidade. Preferir a próxima entrega que tenha valor demonstrável, baixo risco e contrato bem definido.

O primeiro caso vertical deve incluir UI, serviço, operação real e testes, para validar a arquitetura atual — mas seu módulo deverá ser escolhido com base na auditoria, não imposto antecipadamente.

---

## 18. Definição de sucesso do projeto

O trabalho será considerado bem-sucedido quando:

- Os diferenciais aprovados estiverem integrados à aplicação existente, e não isolados como protótipos sem integração.
- As funcionalidades e correções próprias já existentes continuarem funcionando.
- Operações de risco possuírem preview/políticas adequadas, logs e recuperação documentada.
- A UI permanecer responsiva durante operações prolongadas.
- A integridade de transferências, hashes, renomeios, fragmentos e gravações for verificável.
- Claude e AGY conseguirem evoluir o código em incrementos revisáveis e rastreáveis.
- Dependências, licenças, versões e limitações estiverem documentadas.
- O repositório permanecer compilável e testável a cada incremento integrado.

**Princípio final:** esta SPEC é um plano de evolução, não uma ordem de reescrita. A auditoria do código real deve determinar o que será reaproveitado, adaptado, criado ou descartado — e nada que já funcione deve ser substituído sem motivo técnico verificável.
