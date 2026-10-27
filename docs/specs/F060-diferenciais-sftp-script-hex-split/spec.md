# F060 — Diferenciais: Dividir/Juntar, editor hexadecimal, SFTP, script e busca por frase — SPEC

Status: **APROVADA pelo Alexandre em 26/10/2026** ("Siga com a F060"; ordem de entrega pedida por ele: independentes primeiro, dependências antes dos dependentes). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui (isso é do plan).
Origem: `origem/SPEC_original_v1.2.md`, recortada pelo que **ainda não existe** (ver `matriz-situacao.md`). Sync, renomeio em lote e fila de cópias **não** estão aqui: já têm spec aprovada (F041, F037, F036).

- [x] **APROVADA pelo Alexandre** (data: 26/10/2026)

Esta é uma spec-mãe de cinco módulos independentes. Cada um vira sua própria pasta `docs/specs/F06x-...` com plan e tasks quando for aprovado para começar; nenhum começa antes de aprovado.

## 1. Por quê
O Files Custom já navega, copia e compara. Faltam quatro ferramentas que quem mexe com arquivos grandes, servidores e rotinas repetidas hoje busca em programas separados: dividir um arquivo em partes e juntar de novo conferindo se nada se perdeu, olhar e corrigir bytes de um arquivo enorme, abrir servidores SFTP como se fossem pastas, e automatizar tarefas repetitivas sem sair do programa.

## 2. Histórias de usuário (por módulo)
- **M1 Dividir/Juntar (P1)** Como usuário, quero dividir um arquivo grande em partes de tamanho fixo (para pendrive FAT32, e-mail ou envio) e juntar de volta com a certeza de que ficou idêntico.
- **M2 Editor hexadecimal (P2)** Como usuário, quero abrir qualquer arquivo, mesmo de vários GB, ver e corrigir bytes sem o programa travar nem usar toda a memória.
- **M3 SFTP (P2)** Como usuário, quero abrir um servidor SFTP (por senha ou chave) na mesma tela das pastas e copiar de e para ele, com aviso claro se a identidade do servidor mudou.
- **M4 Script (P3)** Como usuário, quero escrever um script curto que filtre arquivos da pasta atual e execute uma ação em lote, vendo antes o que ele faria.
- **M5 Busca por frase (P3, experimental)** Como usuário, quero digitar o que procuro em linguagem natural (ex.: "todos os arquivos ini salvos nos últimos 12 dias") e ver os arquivos, depois de conferir o que o programa entendeu.

## 3. Cenários de aceite

### M1 — Dividir/Juntar
| ID | Dado | Quando | Então |
|---|---|---|---|
| M1-AC-1 | arquivo de 10 MB e tamanho de parte 4 MB | divido | saem 3 partes (4 MB, 4 MB, 2 MB) numeradas em sequência, mais um arquivo de conferência; o original continua intacto |
| M1-AC-2 | as 3 partes e o arquivo de conferência | junto | o resultado tem os mesmos bytes do original (conferido por resumo) e só aparece com o nome final depois de conferido |
| M1-AC-3 | uma parte ausente, repetida, truncada ou alterada | tento juntar | o programa recusa, diz **qual** parte e **por quê**, e não cria o arquivo final |
| M1-AC-4 | arquivo vazio, ou menor que uma parte, ou múltiplo exato do tamanho | divido e junto | resultado correto nos três casos |
| M1-AC-5 | já existe um arquivo com o nome do resultado | junto | pergunta antes; nunca sobrescreve sem eu escolher |
| M1-AC-6 | falta espaço no disco no meio | divido ou junto | para com mensagem clara, não deixa arquivo final incompleto com nome de pronto |

### M2 — Editor hexadecimal
| ID | Dado | Quando | Então |
|---|---|---|---|
| M2-AC-1 | arquivo de vários GB | abro | a tela aparece sem esperar o arquivo inteiro e a memória usada não cresce com o tamanho do arquivo (medido) |
| M2-AC-2 | arquivo aberto | vou a um deslocamento (decimal ou hexa), inclusive perto do fim | mostra os bytes corretos |
| M2-AC-3 | bytes mostrados em hexa e em texto | seleciono um byte numa coluna | o mesmo byte fica destacado na outra |
| M2-AC-4 | altero 3 bytes | salvo e comparo por fora | só esses 3 bytes mudaram |
| M2-AC-5 | alterações ainda não salvas | desfaço | voltam ao valor original |
| M2-AC-6 | o arquivo foi alterado por outro programa enquanto eu editava | tento salvar | sou avisado e nada é sobrescrito sem minha decisão |
| M2-AC-7 | arquivo somente leitura, em uso, ou disco sem espaço | tento salvar | mensagem clara; o original permanece íntegro |

### M3 — SFTP
| ID | Dado | Quando | Então |
|---|---|---|---|
| M3-AC-1 | servidor SFTP de teste e senha correta | conecto | vejo as pastas do servidor como as locais; copio um arquivo para lá e de volta, idêntico |
| M3-AC-2 | login por chave privada | conecto | funciona sem pedir senha do servidor |
| M3-AC-3 | primeira conexão a um servidor | conecto | mostro a identidade do servidor e só confio se eu confirmar |
| M3-AC-4 | a identidade do servidor mudou desde a última vez | conecto | recuso e explico o risco; só prossigo se eu remover a confiança antiga de propósito |
| M3-AC-5 | a rede cai no meio de uma cópia | espero | o programa diz que a rede caiu (não que o arquivo sumiu) e não deixa arquivo parcial com nome de pronto |
| M3-AC-6 | senha ou chave informadas | olho logs e configurações | nenhuma aparece em texto legível |

### M5 — Busca por frase (experimental, desligada por padrão)
| ID | Dado | Quando | Então |
|---|---|---|---|
| M5-AC-1 | a frase "todos os arquivos ini salvos nos últimos 12 dias" | confirmo | aparecem chips "extensão .ini" e "modificado nos últimos 12 dias" e só então a busca roda |
| M5-AC-2 | os chips mostrados | edito um chip | a busca usa o valor editado |
| M5-AC-3 | frase que não entendo (ex.: "coisas bonitas") | confirmo | o programa diz que não entendeu e não busca |
| M5-AC-4 | "maiores que 100 MB" e "fotos de ontem" | confirmo | tamanho e data viram filtros corretos |
| M5-AC-5 | a mesma frase em inglês | confirmo | mesmo resultado |
| M5-AC-6 | uma busca que gostei | salvo | fica na lista de buscas salvas |

### M4 — Script
| ID | Dado | Quando | Então |
|---|---|---|---|
| M4-AC-1 | script que seleciona os `.log` da pasta atual e os copia para outra pasta | rodo em "simular" | vejo a lista do que seria copiado e nada é alterado |
| M4-AC-2 | o mesmo script | rodo de verdade | copia só aqueles arquivos e mostra um relatório por arquivo |
| M4-AC-3 | erro de sintaxe | executo | aponta a linha e a coluna do erro |
| M4-AC-4 | script que não termina | executo | posso cancelar, e há um limite de tempo configurável |
| M4-AC-5 | script que tenta apagar, ou usar o sistema fora do que foi permitido | executo | é barrado ou pede minha confirmação explícita |
| M4-AC-6 | script importado de fora | abro | vem **desligado** e identificado; nunca roda sozinho |

## 4. Requisitos
- **FR-M1** Partes de tamanho exato em bytes (a última pode ser menor); nomes em sequência; arquivo de conferência versionado com ordem, tamanhos e resumos; resumo forte (SHA-256) opcional além do CRC32. Original nunca é apagado pelo programa. Resultado montado em arquivo temporário e promovido só após a conferência.
- **FR-M2** Leitura por blocos e deslocamento; só as linhas visíveis são desenhadas; edições ficam em memória/diário limitados e só vão ao disco ao salvar; antes de salvar confere se o arquivo original ainda é o mesmo; a coluna de texto nunca altera bytes.
- **FR-M3** SFTP por senha e por chave; confiança na identidade do servidor registrada pelo usuário; credenciais guardadas com a proteção do Windows do usuário; erros distintos para tempo esgotado, senha recusada, identidade alterada e queda de rede; cópias passam pelo mesmo painel de progresso e pela fila das outras cópias.
- **FR-M4** Linguagem de script embutida (escolha em ADR); API restrita a: listar e filtrar arquivos, copiar/mover/renomear pela fila, ler metadados, registrar relatório. Modo simular obrigatório antes da primeira execução de um script novo. Sem acesso livre ao sistema.
- **FR-M5** Tradutor por regras, offline, em português e inglês; mostra o que entendeu antes de buscar; "salvo" significa data de modificação (a prévia diz qual data usou); só localiza, nunca apaga ou altera; reaproveita a busca e o índice existentes, sem segundo motor. Camada de IA fica fora desta versão.
- **NFR-001** Nenhum módulo bloqueia a janela; operação longa mostra progresso e aceita cancelar.
- **NFR-002** Memória medida (não estimada) em M1 e M2 com arquivo de pelo menos 4 GB; o pico não pode crescer proporcionalmente ao tamanho.
- **NFR-003** Nenhuma senha, chave ou conteúdo de arquivo em log.
- **NFR-004** Cada módulo adiciona no máximo o tamanho de pacote justificado por escrito (regra do Alexandre: crescimento acima de 5 MB ou 10% exige justificativa).

## 5. Casos de borda
- Nomes com acento, espaço e caminho longo em todos os módulos.
- M1: tamanho de parte maior que o arquivo; arquivo vazio; partes em pastas diferentes.
- M2: arquivo de zero byte; arquivo em disco removível retirado durante a edição; bytes que não formam texto válido na coluna de texto.
- M3: servidor com relógio errado; chave protegida por frase-senha; caminho com barra invertida do Windows.
- M5: "recentes" sem número; "semana passada" (qual intervalo?); extensão com e sem ponto; virada de mês e de ano.
- M4: script que dispara outro script; script que renomeia o que ele mesmo filtra.

## 6. Fora de escopo
- M5 com IA ou envio de texto a serviços externos.
- Sincronização, renomeio em lote e fila de cópias (F041, F037, F036).
- FTP e FTPS (FTP já existe; FTPS só entra se a revisão do M3 mostrar lacuna, como tarefa à parte).
- Migrar a interface ou trocar a arquitetura.
- Executar scripts de origem desconhecida com isolamento de segurança forte (só o modo restrito acima).
- Edição hexadecimal de disco bruto ou de memória de processos.

## 7. Critérios de sucesso
- Dividir um arquivo de 4 GB e juntar de volta com resumo idêntico, com o pico de memória medido e sem a janela travar.
- Corrigir 1 byte no fim de um arquivo de vários GB e salvar em segundos, comprovado por comparação externa.
- Copiar para e de um servidor SFTP real de teste sem expor credencial.
- Um script de 5 linhas que arruma uma pasta de logs, com simulação antes.

## 8. Pontos em aberto `[NEEDS CLARIFICATION]`
- **Q1** DECIDIDO (26/10/2026): ordem pelo critério do Alexandre, independentes primeiro (ver `plan.md` §2).
- **Q2** M3: biblioteca para SFTP e licença — decidir por ADR-007 antes do plan (candidata da SPEC original: SSH.NET). Não aprovada.
- **Q3** M4: linguagem do script (Lua ou JavaScript embutidos, candidatos da SPEC original) — decidir por ADR-008 com protótipo.
- **Q4** M2: salvar no próprio arquivo ou em cópia temporária (arquivo de vários GB e pouco espaço livre) — ADR-011.
- **Q5** DECIDIDO pela recomendação (Alexandre pode vetar): formato próprio simples e versionado, documentado. M1: o arquivo de conferência segue algum formato já usado por outra ferramenta (HJSplit, 7-Zip por volumes)? Recomendação: formato próprio simples e versionado, e documentar; **o Alexandre decide** se precisa abrir as partes em outro programa.
- **Q6** Licença do projeto-base (Files): confirmar a versão exata usada no fork antes de qualquer pacote novo (ADR-015).
- **Q8** DECIDIDO (Alexandre, 26/10/2026): "semana passada" = últimos 7 dias; a prévia mostra o intervalo exato.
- **Q7** FTPS: já funciona do jeito esperado? Revisão somente leitura do que o FluentFTP está fazendo hoje (M3 T0).

## 9. Histórico
- 26/10/2026 — criada a partir do arquivo enviado pelo Alexandre; recortada: removido o que o código já tem (hashes, FTP, status de progresso) e o que já tem spec aprovada (F036, F037, F041).
- 26/10/2026 — APROVADA. Q1 e Q8 decididos pelo Alexandre; Q5 pela recomendação. Q2, Q3, Q4, Q6 e Q7 viram a tarefa 0 do módulo afetado (só esse módulo espera).
