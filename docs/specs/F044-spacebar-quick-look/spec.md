# F044 — Visualização rápida com a barra de espaço — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Ver o conteúdo de um arquivo sem abrir outro programa (como no macOS e no OneCommander) acelera quem confere dezenas de arquivos. Hoje o app só delega a programas externos (QuickLook/Seer), se instalados.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero apertar Espaço num arquivo e ver uma prévia por cima da lista; Espaço ou Esc fecha.
- **US2 (P1)** Como usuário, quero passar para o próximo/anterior com as setas, com a prévia acompanhando.
- **US3 (P2)** Como usuário, quero ampliar para tela cheia e ver metadados (tamanho, data, dimensões, local da foto).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | um arquivo de imagem selecionado | aperto Espaço | a prévia abre em até 300 ms por cima da lista |
| AC-2 | prévia aberta | aperto ↓ | a seleção e a prévia vão ao item seguinte |
| AC-3 | prévia aberta | aperto Esc ou Espaço | fecha e o foco volta ao item |
| AC-4 | arquivo sem prévia possível (binário, bloqueado) | aperto Espaço | mostra ícone, tipo, tamanho e o motivo, sem erro |
| AC-5 | seleção de uma pasta | aperto Espaço | mostra resumo (itens, tamanho) calculado sem travar |
| AC-6 | QuickLook ou Seer instalados | aperto Espaço | o app usa o configurado como preferido, sem abrir os dois |
| AC-7 | tecla Espaço segurada | observo | não abre/fecha em repetição; abre uma vez por toque |

## 4. Requisitos
- **FR-001** Suportar imagens, texto/código, PDF, áudio/vídeo e Markdown no mínimo.
- **FR-002** Não manter arquivo aberto/bloqueado depois de fechar a prévia.
- **FR-003** Em renomeação ou caixa de texto, Espaço continua sendo um espaço.
- **NFR-001** Trocar de item com a seta mostra o próximo em até 150 ms para imagens até 10 MB (medido).

## 5. Casos de borda
- Arquivo enorme (só o começo, para texto). Arquivo na nuvem "só online" (não baixar sem pedir). Seleção múltipla.

## 6. Fora de escopo
- Editar na prévia; anotações.

## 7. Critérios de sucesso
- Percorrer 50 fotos com Espaço + setas sem abrir nenhum programa.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: usar o painel de preview existente (F005) ou uma sobreposição própria? Recomendação: sobreposição própria, reaproveitando os mesmos leitores.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
