# F040 — Localizar arquivos duplicados — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Fotos, downloads e backups acumulam cópias idênticas. Hoje não há como achá-las nem escolher com segurança qual manter. O Directory Opus tem localizador de duplicados embutido.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero escolher pastas e achar arquivos idênticos entre elas.
- **US2 (P1)** Como usuário, quero marcar quais cópias remover com regras seguras ("manter a mais antiga", "manter a da pasta X") e mandá-las para a Lixeira.
- **US3 (P2)** Como usuário, quero também achar "parecidos": mesmo nome e tamanho, ou mesmo tamanho (modo rápido).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | 2 pastas com 3 pares idênticos | analiso | os 3 grupos aparecem com todos os caminhos |
| AC-2 | arquivos do mesmo tamanho mas conteúdo diferente | analiso | não são listados como duplicados no modo "conteúdo" |
| AC-3 | grupo de 3 cópias | aplico "manter a mais antiga" | 2 ficam marcadas e 1 desmarcada; nada é apagado ainda |
| AC-4 | marcação feita | "Mover para a Lixeira" | só as marcadas vão; é impossível marcar **todas** as cópias de um grupo |
| AC-5 | análise em andamento | "Cancelar" | para sem alterar nada |
| AC-6 | arquivo em uso ou sem permissão | analiso | é pulado e listado como "não verificado" |
| AC-7 | duplicado dentro de arquivos protegidos do sistema | analiso | não entra na lista por padrão |

## 4. Requisitos
- **FR-001** Comparar primeiro por tamanho, depois por conteúdo (resumo criptográfico), nunca só pelo nome.
- **FR-002** Nunca permitir apagar a última cópia de um grupo.
- **FR-003** Excluir só para a Lixeira (reversível), com desfazer.
- **FR-004** Opção de ignorar pastas (`.git`, `node_modules`) e arquivos menores que X.
- **NFR-001** Ler cada arquivo só quando houver outro do mesmo tamanho; memória estável com 1 milhão de arquivos (medido).

## 5. Casos de borda
- Hardlinks (mesmo arquivo, dois caminhos: não é duplicado a remover). Arquivos de zero byte. Pastas aninhadas selecionadas juntas.

## 6. Fora de escopo
- Imagens "visualmente parecidas"; mesclar pastas.

## 7. Critérios de sucesso
- Liberar espaço de uma pasta de fotos com cópias, sem apagar nenhum original (verificado por contagem antes/depois).

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: modo "só nome + tamanho" (rápido, menos seguro) entra? Recomendação: sim, rotulado como "não garantido".]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
