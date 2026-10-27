# F036 — Spike 3: CopyFile2, pausa e tarefas concorrentes em pasta de rede (NAS)

Data: 25/10/2026. Autor: Claude. Local de teste autorizado pelo Alexandre: `\\hd-roteador\sda1\temp` ("pode usar à vontade dentro dessa pasta; crie o que precisar dentro dela e nada fora"). **Todos os dados de teste foram criados só dentro dessa pasta** (subpasta `claude\<GUID>`, apagada ao fim). Fora dela existem só o programa compilado (código) e estes resultados no repositório. Origens locais usadas **somente para leitura**: um instalador do próprio projeto (50 MB), um arquivo de 782 MiB e um PDF de 11 MB que já existiam em `Downloads`. Código em `tools/perf/spikes/F036-copy-spike/`; saídas em `results3-*.csv` desta pasta. Continuação de `spike2-copyfile2-paridade.md`.

## Ambiente (OBSERVED)
Servidor `hd-roteador` (192.168.1.1, ping 0 ms). O volume reporta **NTFS**, 173,3 GiB livres de 465,8, **ACL: sim; fluxos alternativos (`FILE_NAMED_STREAMS`): não; esparso: não; compressão: não; criptografia: não**. Velocidade bruta: gravar 22,9 MiB/s, ler 20,2 MiB/s (256 MiB).

## Resultados

### 1. Envio do computador para o NAS (782 MiB, origem local somente leitura) — CONFIRMED
Todos com **SHA-256 idêntico** (9 de 9): `CopyFile2` direto 22 e 24 MiB/s; `File.Copy` 25 e 25 MiB/s; **pausa dupla** (aos 200 e 500 MiB, destino com 207 e 507 MiB nas pausas) 2 de 2; **cancelar na pausa** (apagar o parcial funcionou, nova cópia limpa correta); **nome temporário + renomear** e **renomear substituindo** um arquivo existente, ambos corretos. Memória do processo: 24 MiB. A velocidade é limitada pelo NAS, não pelo método.

### 2. Cópia de NAS para NAS — OBSERVED, parcial
- **50 MB:** `CopyFile2` funcionou 2 de 2 (13 MiB/s, hash igual).
- **256 MiB (1ª tentativa, saída do console; o CSV desta rodada foi apagado por engano ao iniciar a rodada seguinte):** `File.Copy` terminou em 71,7 s (hash igual); `CopyFile2` **falhou** com `0x8007003B` (`ERROR_UNEXP_NET_ERR`) aos 65,7 s, deixando um arquivo de destino incompleto (hash diferente). **Uma ocorrência (n = 1): frequência desconhecida.**
- Em outra rodada, 2 cópias de 256 MiB com `File.Copy` levaram 40,6 s em sequência (20 s cada): a primeira cópia lenta (71 s) **não se repetiu**, então o NAS tem variação grande.

### 3. A marca "baixado da internet" (`Zone.Identifier`) — CONFIRMED
Origem: PDF de 11 MB com `Zone.Identifier`. Copiado para o NAS por `File.Copy`, `CopyFile2` e cópia por blocos: **os três terminam sem erro, com conteúdo idêntico, e descartam o fluxo em silêncio** (o destino não suporta fluxos). O motor do shell mostra um aviso de propriedades que não serão copiadas; os outros três não. (O dado "o destino não tem o fluxo" vem de o volume declarar que não suporta fluxos; o `meta` do destino devolveu vazio.)

### 4. Tarefas concorrentes no NAS (o cenário pedido pelo Alexandre) — OBSERVED, n = 1 por caso
| Caso | Resultado |
|---|---|
| Copiar um lote de 24 arquivos de 8 MiB (192 MiB) **sozinho** | 18,1 s (10,6 MiB/s) |
| Apagar 300 arquivos pequenos **sozinho** | 5,35 s |
| **Os dois ao mesmo tempo** | cópia 21,8 s (8,8 MiB/s, **20% mais lenta**); apagar 26,1 s (**4,9 vezes mais lento**); tempo total 26,8 s contra 23,4 s se fossem um depois do outro (**14% pior** que sequencial). Resultado correto: 24 de 24 copiados, 0 restantes a apagar |
| **2 cópias de 256 MiB em sequência** | 40,6 s (12,6 MiB/s no total) |
| **As mesmas 2 cópias juntas** | 32,9 s (15,5 MiB/s no total): **19% mais rápido que em sequência** |

**Leitura:** (a) no NAS, duas cópias **grandes** juntas foram **mais rápidas** que em sequência; (b) copiar e apagar muitos arquivos pequenos ao mesmo tempo foi **pior** que um depois do outro, com o apagar sofrendo muito (disputa de operações de metadados); (c) nada falhou nem corrompeu nos casos concorrentes. **n = 1 por caso e a variação do NAS é grande**: vale como indício, não como medida.

## O que isso muda
1. **Fase C em SMB:** viável para **envio** (provado). Regra nova **FR-008**: se a origem tem fluxos alternativos e o destino não os suporta, recuar para o motor atual (que avisa).
2. **Fila por destino:** a premissa "serializar é melhor" **não foi sustentada**: no NVMe local a medição da AGY foi inconclusiva (n = 3, uma rodada atípica) e, no NAS, duas cópias grandes juntas foram mais rápidas. Há indício contra serializar tudo e a favor de **tratar tipos de operação diferentes**: cópias/movimentos (muito dado) podem ter um limite de simultaneidade por destino; apagar e renomear (muito metadado) não deveriam ficar atrás de uma cópia nem disputar com ela sem necessidade. Ver `DECISIONS.md` D-F036-3 (pendente).
3. **Medição:** antes de a F036 fixar uma política, a T2 deve medir com **≥ 10 rodadas** em NVMe local, disco rotativo e NAS, sozinho/juntos, copiar/apagar/mover.

## NOT TESTED
Gravar a partir do NAS para o computador (destino local fora de `%TEMP%` não foi tocado); EFS; propriedades do shell; FAT/exFAT; rede lenta com perdas; pasta inteira; muitos arquivos pequenos em lote pela fase C; retomada depois de o app fechar.
