# F039 — O que está ocupando meu disco — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
"Meu disco encheu, o que apaga?" é a pergunta mais comum e o Explorer não responde. Hoje só dá para ver o tamanho de uma pasta por vez, e devagar.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero ver as pastas e arquivos de uma unidade ou pasta ordenados do maior para o menor.
- **US2 (P2)** Como usuário, quero um mapa de blocos (cada bloco proporcional ao tamanho) para achar o gigante de relance.
- **US3 (P2)** Como usuário, quero ir do resultado direto ao arquivo (abrir no local, apagar para a Lixeira).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | unidade C: | "Analisar uso de disco" | em até 5 s vejo as 20 maiores pastas, e a análise segue refinando sem travar a janela |
| AC-2 | análise em andamento | clico "Cancelar" | para em até 1 s e mantém o que já mediu |
| AC-3 | resultado na tela | clico num bloco/linha | navega para dentro; "Voltar" sobe um nível |
| AC-4 | item grande | "Mostrar no local" | abre a pasta com o item selecionado |
| AC-5 | item selecionado | "Excluir" | vai para a Lixeira com confirmação; o total é atualizado sem refazer tudo |
| AC-6 | pastas sem permissão | analiso | são contadas como "inacessíveis" e listadas, sem interromper |
| AC-7 | resultado | comparo com o tamanho da unidade | a soma das partes + "inacessível" + "arquivos de sistema" fecha com o usado do disco, com diferença explicada |

## 4. Requisitos
- **FR-001** Medir só o que o usuário tem permissão de ver.
- **FR-002** Aproveitar o índice da busca (F023) quando existir; senão, varredura normal.
- **FR-003** Filtrar por tipo (vídeos, imagens, instaladores, `node_modules`...) e por idade.
- **NFR-001** Memória estável em unidade com 5 milhões de arquivos (medido, sem listar tudo na tela).

## 5. Casos de borda
- Links simbólicos/junctions (não contar duas vezes); arquivos esparsos e compactados; OneDrive "só na nuvem" (não baixar); disco de rede.

## 6. Fora de escopo
- Apagar automaticamente; recomendações por IA.

## 7. Critérios de sucesso
- Achar os 5 maiores arquivos da unidade C: em menos de 10 s de interação.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: mapa de blocos já na primeira versão ou só a lista? Recomendação: lista primeiro (P1), mapa em seguida.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Concorrente de referência: WinDirStat/TreeSize (ver pesquisa).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
