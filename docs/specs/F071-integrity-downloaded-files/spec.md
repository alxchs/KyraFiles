# F071 — Integridade e Arquivos Baixados — SPEC

Status: **APROVADA pelo Alexandre em 12/11/2026** ("Specs aprovadas"). Estado anterior: RASCUNHO (AGY, 31/10/2026). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui: isso é do `plan.md`.

## 1. Por quê
Usuários frequentemente baixam arquivos grandes da internet (ISOs, instaladores, arquivos compactados) e precisam verificar se o download não foi corrompido antes de abri-los. Hoje, a conferência de hashes pode falhar ou consumir memória excessiva em arquivos gigantes, e arquivos baixados da web permanecem marcados com o bloqueio de segurança do Windows (Zone.Identifier / "Mark of the Web"), exigindo desbloqueio manual um por um através das propriedades. O sistema precisa permitir:
1. Calcular e comparar a integridade (hash) de arquivos de qualquer tamanho com baixo consumo de memória, barra de progresso e cancelamento instantâneo.
2. Liberar o bloqueio de internet de múltiplos arquivos selecionados em uma única ação em lote com retorno claro da quantidade de itens desbloqueados.

## 2. Histórias de usuário (por prioridade)
- **US1 (P1 - Integridade Confiável de Arquivos Grandes):** Como usuário que baixa imagens e arquivos grandes, quero calcular o hash (MD5, SHA-1, SHA-256) e comparar com o valor fornecido pela fonte, sem travar o aplicativo ou esgotar a memória do computador, podendo cancelar o cálculo quando desejar.
- **US2 (P2 - Desbloqueio de Arquivos da Internet em Lote):** Como usuário que baixa múltiplos anexos ou arquivos da web, quero selecionar vários itens e remover a marca de bloqueio de internet de uma só vez, vendo quantos arquivos estavam bloqueados e foram liberados.

## 3. Cenários de aceite

### Sub-spec A: Hash e Conferência
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | Um arquivo de grande porte (ex.: 4 GB ou superior) selecionado | O cálculo de hash (SHA-256) é iniciado | O hash é calculado através de fluxo em partes (streaming/chunking), sem carregar o arquivo inteiro na memória (pico de memória RAM do processo não excede 50 MB acima da linha de base). |
| AC-2 | O cálculo de hash de um arquivo grande está em andamento | O usuário clica em "Cancelar" | O processamento e a leitura em disco são interrompidos imediatamente (< 500 ms) e os recursos são liberados. |
| AC-3 | O hash do arquivo foi calculado ou está disponível | O usuário cola uma cadeia de caracteres de hash na caixa de comparação | O sistema compara o valor colado (ignorando maiúsculas/minúsculas e espaços) com os algoritmos disponíveis e indica visualmente se corresponde ou não. |
| AC-4 | O arquivo está aberto em modo exclusivo por outro processo ou inacessível | O cálculo é solicitado | O sistema reporta erro amigável de leitura sem travar ou fechar inesperadamente o aplicativo. |

### Sub-spec B: Liberar Marca de Internet em Lote
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-5 | Múltiplos arquivos selecionados, onde 3 possuem a marca de bloqueio de internet e 2 não possuem | O usuário aciona a ação "Desbloquear arquivos baixados" | A marca de bloqueio dos 3 arquivos é removida com sucesso; os 2 arquivos sem a marca permanecem inalterados. |
| AC-6 | A operação em lote é concluída | O término da execução ocorre | Uma mensagem de status/notificação informa: *"3 arquivos baixados foram desbloqueados com sucesso"* (ou mensagem no plural/singular apropriada). |
| AC-7 | Nenhum arquivo da seleção possui a marca de internet | O usuário aciona a ação de desbloqueio | O sistema informa que nenhum dos arquivos selecionados necessitava de desbloqueio, sem gerar erros. |

## 4. Requisitos
- **FR-001:** O sistema DEVE disponibilizar cálculo de integridade nos algoritmos CRC32, MD5, SHA-1, SHA-256, SHA-384 e SHA-512.
- **FR-002:** O cálculo de integridade DEVE ser executado em streaming com blocos de buffer controlados e suporte nativo a cancelamento.
- **FR-003:** O sistema DEVE fornecer campo para conferência rápida de hash com correspondência insensível a maiúsculas/minúsculas e remoção de espaços em branco.
- **FR-004:** O sistema DEVE identificar a presença do fluxo de origem da internet (`Zone.Identifier`) em arquivos do sistema de arquivos NTFS.
- **FR-005:** O sistema DEVE fornecer ação de comando para remoção em lote do fluxo de bloqueio nos itens selecionados.
- **FR-006:** O sistema DEVE emitir relatório quantitativo (contagem de itens liberados) após o processamento em lote.
- **NFR-001 (Consumo de Memória):** O pico de memória privada/working set durante o cálculo de hash de arquivos de múltiplos gigabytes DEVE permanecer estritamente contido (variação de RAM < 50 MB durante o stream).
- **NFR-002 (Não Bloqueio da Interface):** Todo I/O e computação criptográfica DEVE ocorrer fora da thread principal/UI.
- **NFR-003 (Conformidade Visual):** Os indicadores de sucesso ou aviso de incompatibilidade de hash não devem empregar paleta vermelha.

## 5. Casos de borda
- Arquivo armazenado em sistema de arquivos sem suporte a fluxos alternativos (FAT32/exFAT): a opção de desbloqueio de internet não se aplica e deve falhar de forma transparente sem erro crítico.
- Arquivo somente leitura com marca de internet: o sistema deve tentar remover o atributo somente leitura temporariamente para remoção do fluxo ou emitir aviso claro de permissão.
- Arquivo com tamanho zero bytes: hash de stream vazio é gerado corretamente conforme especificação de cada algoritmo.
- Cancelamento durante o desbloqueio em lote de milhares de itens: a operação deve parar no item corrente sem corromper os demais.

## 6. Fora de escopo
- Verificação de assinaturas digitais Authenticode (certificados X.509 de binários executáveis).
- Edição do conteúdo do fluxo `Zone.Identifier` (apenas a remoção/desbloqueio total é suportada).

## 7. Critérios de sucesso
- Cálculo de SHA-256 de um arquivo de teste de 2 GB executado até o fim sem elevação anormal de memória (< 50 MB de delta de RAM).
- Seleção de múltiplos arquivos com `Zone.Identifier` desbloqueados com contagem precisa exibida na interface.

## 8. Pontos em aberto
- [NEEDS CLARIFICATION: Onde posicionar a ação "Desbloquear arquivos baixados" no menu de contexto? Proposta da AGY: subitem em "Mais opções" ou seção de contexto quando ao menos 1 item selecionado for arquivo baixado].

## 9. Histórico
- 31/10/2026 — Criação inicial da spec em RASCUNHO pela AGY conforme `FILA-KYRAFILES.md` Item 3.
