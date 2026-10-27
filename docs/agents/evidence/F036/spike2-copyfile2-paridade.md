# F036 — Spike 2: paridade do CopyFile2 (T8)

Data: 25/10/2026. Autor: Claude. Autorizado pelo Alexandre ("ok") a gravar numa pasta de teste no D: (a pasta de teste foi apagada ao fim; a `D:\Temp` vazia que o teste criou **ficou**, porque a remoção foi bloqueada pela proteção da ferramenta e não foi contornada: pode ser apagada à mão). Código em `tools/perf/spikes/F036-copy-spike/` (`Program.cs`, `Spike2.cs`, `run-spike2.ps1`); saídas em `results2-*.csv` desta pasta. Tudo fora do app, sem teclas, com arquivos gerados pelo experimento. Continuação de `spike-copia-por-blocos.md`.

## Ambiente (OBSERVED)
C: NVMe Kingston SNV2S1000G; **D: HDD WD10SPZX (5400 rpm)**, outro disco físico; os dois NTFS. RAM disponível de 1,4 a 2,3 GB. Compartilhamento SMB `desenv` do próprio computador (aponta para `C:\desenv`).

## Resultados

| # | Pergunta | Resultado | Estado |
|---|---|---|---|
| 1 | Pausa e retomada **entre discos físicos diferentes** (C: para D:, 1 GiB; D: para C:) | Funciona nos dois sentidos. Hash SHA-256 completo igual em **todos** os casos (C para D: 5 de 5 cópias e 3 de 3 pausas; D para C: 2 de 2). A **latência da pausa entre discos não foi medida** neste spike (a do spike 1, no mesmo volume, foi de 28 a 35 ms); aqui só se provou que pausa e retomada funcionam e que o conteúdo fica idêntico. | CONFIRMED |
| 2 | **Pausar duas vezes** na mesma cópia | Funciona (3 de 3): destino com 302 MiB na 1ª pausa e 702 MiB na 2ª; o contador do callback é **cumulativo** entre retomadas (300 e 700 MiB). Hash igual. | CONFIRMED |
| 3 | **Cancelar durante a pausa** | O arquivo parcial **não fica bloqueado**: apagar funcionou (C para C e C para D), e uma cópia limpa em seguida terminou com `hr=0` e hash igual. | CONFIRMED |
| 4 | **Nome longo** (caminho de destino de 692 caracteres) | `CopyFile2`, `File.Copy` e a cópia por blocos funcionaram com o prefixo `\\?\` (hash igual). O **motor do shell falhou** (`CopyHere` não criou o arquivo), o que bate com o recuo para `StorageFile` que o app já tem (`NameTooLong`). | CONFIRMED |
| 5 | **Leitura por SMB** (origem em `\\localhost\desenv\...`, 50 MB, 2 rodadas) | `CopyFile2` e `File.Copy` funcionam; pausa e retomada funcionam (pausou com 17 MiB gravados, hash igual). **Atenção:** é loopback no mesmo computador, não rede real; o arquivo é pequeno. | OBSERVED, parcial |
| 6 | **Esparso e compactado** | Origem: esparso (2 MiB físicos de 64) e compactado (0 B físicos de 16 MiB). **Nenhum método preservou**, nem o motor do shell: todos gravaram o tamanho cheio (64 MiB e 16 MiB) e sem os atributos. Paridade total (todos perdem igual). | CONFIRMED |
| 7 | **Variação do tempo de retomada** (6 rodadas, 1 GiB, 300 MiB antes da pausa) | Retomada de 1,14 a 1,34 s em todas as 6 (faixa estreita). O 13,9 s do spike 1 não se repetiu; aconteceu com 2 GiB e 1,4 GB de RAM livre (pressão de memória é a hipótese, **não provada**). | OBSERVED |
| 8 | Desempenho entre discos (C para D, 1 GiB, HDD) | `File.Copy` 31 e 131 MiB/s (o 31 isolado, outlier); **`CopyFile2` 127 e 132**; **motor do shell 101 e 112**; **cópia por blocos 95 e 96**. O `CopyFile2` é o mais rápido ou igual; a cópia por blocos é a mais lenta. | OBSERVED |

## NOT TESTED (não foi possível ou não foi autorizado)
- **Gravar (destino) por SMB:** o único compartilhamento aponta para `C:\desenv` (fora de `%TEMP%`) e criar um compartilhamento exige administrador. Só a **leitura** por SMB foi testada.
- **Rede de verdade** (outro computador, NAS, VPN, latência): não há aqui; o resultado 5 não vale para rede lenta.
- **EFS (criptografia)**, **propriedades do shell** (`PropertyLoss`) e **FAT/exFAT**: não há volume FAT/exFAT autorizado para teste, e o disco externo (S:) não foi tocado.
- **Pasta inteira:** o `CopyFile2` copia **arquivo**; pastas precisam ser percorridas pelo app.
- **Cópia de muitos arquivos pequenos, em lote, e o mover** (apagar a origem após cada item).

## O que isso diz para a fase C
- **Viável:** pausa e retomada no meio do arquivo por `CopyFile2`, com paridade com o motor atual nos metadados testados, desempenho igual ou melhor, memória estável (24 MiB) e cancelar sem deixar arquivo preso.
- **Custo de integração (INFERRED, pelo código lido):** (a) o `CopyFile2` copia um arquivo por vez, então o app teria de percorrer pastas e chamá-lo por item; (b) o motor do shell hoje devolve resultado **por item** (`InUse`, `AlreadyExists`, `PropertyLoss`, `NameTooLong`) que o app trata com diálogos; um caminho próprio precisa reproduzir esse tratamento; (c) o histórico de desfazer exige os caminhos de destino; (d) o `Mover` tem de apagar a origem só depois de cada item verificado.
- **Recomendação:** a fase C **não** substitui o motor para tudo. Escopo mínimo: **só arquivos grandes** (por exemplo, a partir de 256 MiB), **só destino local ou SMB**, com **recuo automático para o motor atual** diante de qualquer erro do `CopyFile2`. O resto segue pela fase A+B. Isso deve virar uma feature separada (F036-C) depois que A e B estiverem publicadas e medidas, e a decisão de entrar é do Alexandre.
