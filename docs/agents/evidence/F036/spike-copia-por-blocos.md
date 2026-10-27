# F036 — Spike: pausa e retomada no meio do arquivo (cópia por blocos e CopyFile2)

Data: 25/10/2026. Autor: Claude (a pedido do Alexandre: "testar algo que copiasse conjunto de bytes; se não der certo use a (b)"). **Fora do app, sem entrada global, só arquivos gerados pelo próprio experimento** em `%TEMP%\claude\<GUID>\...` (apagado ao fim). Código e scripts em `tools/perf/spikes/F036-copy-spike/`; saídas em `results-*.csv` desta pasta.

## Ambiente (OBSERVED)
Windows 11 Pro build 26340; origem e destino no **mesmo volume C:** (NVMe Kingston SNV2S1000G); RAM disponível ~1,4 a 1,9 GB durante os testes (muita pressão de memória, o que torna o cache de arquivos pouco confiável); Defender ativo. **Não testado:** disco diferente (o D: é HDD e ficou de fora para não escrever fora de `%TEMP%`), rede (SMB), nomes longos, EFS, arquivos esparsos, propriedades do shell.

## O que foi comparado
`File.Copy` (CopyFile2 por baixo), `robocopy`, `CopyFile2` direto (P/Invoke), e uma **cópia por blocos** própria (leitura e escrita síncronas, bloco de 1, 4 ou 16 MiB). Execuções **intercaladas**, 3 rodadas.

## Resultado 1 — a pausa funciona nos dois (CONFIRMED, hash SHA-256 completo igual em 100% dos casos)

| Método | Latência para a pausa fazer efeito | Memória do processo (pico) | Retomada | Hash origem = destino |
|---|---|---|---|---|
| **Blocos de 4 MiB** (2 GiB, pausa aos 700 ms por 3 s) | 3,3 / 0,5 / 7,4 ms | 29 a 30 MiB | do offset, sem recopiar | 3 de 3 |
| **CopyFile2** (2 GiB, pausa após 500 MiB por 3 s) | 29 / 28 / 35 ms | 24 a 25 MiB | `COPY_FILE_RESUME_FROM_PAUSE`, destino parcial de 502 MiB | 3 de 3 |
| **Blocos: processo morto** no meio e retomado por outro processo | n/a | n/a | retomou em 336 e 272 MiB | 2 de 2 |

A meta da spec era parar em até 2 s: os dois estão **centenas de vezes abaixo** disso. O `CopyFile2` devolveu `0x80070BEA` (pausado) e a retomada terminou com `0x00000000`. Tempo de retomada do `CopyFile2` para o resto do arquivo: 3,8 s, 13,9 s e 3,8 s (um valor fora da curva, não explicado).

## Resultado 2 — desempenho (OBSERVED, uma máquina, mesmo NVMe)

MiB/s por rodada (1 GiB, quase tudo no cache de memória): `File.Copy` 999 / 672 / 775; `robocopy` 636 / 587 / 770; **blocos 4 MiB 566 / 434 / 449**; blocos 1 MiB 406 / 474 / 429; blocos 16 MiB 438 / 453 / 464; `CopyFile2` 714 / 622 / 110 (um valor muito baixo, fora da curva).
MiB/s por rodada (**2 GiB**, maior que a memória livre, mais perto do disco): `File.Copy` 465 / 418 / 760; `robocopy` 504 / 158 / 661; **blocos 4 MiB 378 / 239 / 402**; `CopyFile2` 478 / 602 / 497.

Mediana: em 1 GiB os blocos ficaram cerca de **42% abaixo** do `File.Copy`; em 2 GiB, cerca de **19% abaixo** (e `CopyFile2` ~7% acima). O ruído é grande (as rodadas variam até 4x), então vale como **ordem de grandeza, não como número exato**. Esta versão não faz leitura adiantada (duas operações em paralelo); com isso a diferença poderia diminuir, o que **não foi medido**.

## Resultado 3 — o que cada método preserva (OBSERVED, arquivo de 8 MiB, oculto, com datas antigas, fluxo `Zone.Identifier` e ACL própria)

| Item | Origem | Motor de cópia do **shell** (o que o app usa hoje, via `Shell.Application.CopyHere`) | `File.Copy` | `CopyFile2` | **Cópia por blocos** |
|---|---|---|---|---|---|
| Atributos (oculto, arquivo) | Hidden, Archive | **sim** | sim | sim | **não** (perdeu o Hidden) |
| Fluxo alternativo `Zone.Identifier` (marca da web) | presente | **sim** | sim | sim | **não** (não copiou o fluxo) |
| Data de gravação | preservada | **sim** | sim | sim | **não** |
| Data de criação | 2020 | não | não | não | não |
| ACL própria (não herdada) | protegida | não (herda do destino) | não | não | não |

(O `robocopy` copiou também a data de criação. O `robocopy /COPY:DATSOU` falhou por falta de privilégio de administrador e ficou de fora.)

**Leitura:** o `CopyFile2` tem **paridade com o motor do shell** em tudo o que foi testado. A cópia por blocos **perde** atributos, o fluxo `Zone.Identifier` (isto tem efeito de segurança: a marca de "baixado da internet" some e o SmartScreen deixa de agir), e a data de gravação, a menos que cada coisa seja reimplementada (e há outras fora do teste: fluxos de EFS, atributos de compressão, arquivos esparsos, propriedades).

## Erros do próprio experimento, corrigidos (para não valerem como resultado)
1. A primeira versão do script comparava só o **último caractere** do hash (um único valor devolvido era tratado como lista). Todos os `hash_igual=True` daquela rodada **foram descartados**; a fase foi refeita com o hash completo (resultados acima).
2. O script não apagava o arquivo oculto entre as rodadas de metadados, então um método herdava os atributos do anterior. Corrigido e refeito.
3. Na primeira leitura das colunas de desempenho eu concluí, de cabeça, que os blocos eram mais rápidos; ao reler os números, eram mais lentos. Os valores acima são os conferidos.

## Veredito (para a decisão D-F036-1)
- **Cópia por blocos:** funciona como mecanismo (pausa em milissegundos, memória baixa, retoma até depois de o processo morrer), **mas não passa na régua de fidelidade** (perde atributos, `Zone.Identifier` e datas) e é mais lenta nesta forma. Não adotar sem reimplementar os metadados e provar paridade. **Não passou.**
- **`CopyFile2`:** funciona para pausar e retomar, com paridade nos metadados testados e desempenho equivalente ao `File.Copy`. É o candidato real para a pausa **no meio do arquivo**. **Ainda não provado:** disco diferente e rede, nome longo, propriedades, EFS/esparso, cancelamento e um caso de retomada lenta (13,9 s).
- **Consequência:** pela instrução do Alexandre, vale a **opção (b)** (pausa entre arquivos) como o compromisso desta versão. O `CopyFile2` fica como evolução (fase C) a decidir depois de um segundo spike de paridade em disco diferente e rede.
