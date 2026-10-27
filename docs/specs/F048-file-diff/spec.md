# F048 — Comparar arquivos e pastas (diff) — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Quem desenvolve compara versões de arquivos e de pastas várias vezes ao dia. Hoje é preciso abrir outro programa e navegar até os mesmos itens. O gerenciador já tem os dois itens selecionados.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero selecionar 2 arquivos e escolher "Comparar" para ver as diferenças.
- **US2 (P1)** Como desenvolvedor, quero escolher a ferramenta (a embutida, WinMerge, `code --diff`, Beyond Compare).
- **US3 (P2)** Como desenvolvedor, quero comparar 2 pastas e abrir só os arquivos diferentes.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | 2 arquivos de texto selecionados | "Comparar" | a ferramenta escolhida abre já com os 2 arquivos |
| AC-2 | 1 arquivo selecionado e outro na área de transferência (copiado) | "Comparar com o copiado" | abre comparando os dois |
| AC-3 | 3 itens selecionados | olho o menu | "Comparar" fica desabilitado, com dica |
| AC-4 | 2 pastas | "Comparar pastas" | lista arquivos só de um lado, diferentes e iguais; duplo clique abre o diff do par |
| AC-5 | ferramenta escolhida não instalada | "Comparar" | mensagem clara e opção de escolher outra; nada trava |
| AC-6 | arquivo binário | "Comparar" | avisa que é binário e oferece comparação por tamanho/resumo |
| AC-7 | caminho com espaços e acentos | "Comparar" | os dois caminhos chegam corretos à ferramenta |

## 4. Requisitos
- **FR-001** Lista de ferramentas configurável (nome + comando + argumentos com marcadores).
- **FR-002** Detectar automaticamente as instaladas.
- **FR-003** Comparação embutida simples (texto) opcional.
- **NFR-001** Abrir a comparação leva menos de 1 s além do tempo da ferramenta externa.

## 5. Casos de borda
- Arquivos muito grandes; mesma pasta duas vezes; arquivos em rede; links simbólicos.

## 6. Fora de escopo
- Mesclar (merge) de 3 vias; resolver conflitos do git.

## 7. Critérios de sucesso
- De 2 arquivos selecionados ao diff na tela em 2 cliques.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: incluir uma tela de diff embutida ou só delegar à ferramenta externa? Recomendação: só delegar; embutida fica para depois.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
