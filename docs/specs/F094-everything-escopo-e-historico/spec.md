# F094 — Busca Everything: escopo global e histórico de buscas — SPEC

ID estável: SPEC-0078 (alias F094).
Status: **APROVADA** (Alexandre, 21/11/2026: "Aprovada F094"; histórico: "Guardar 20 buscas (informações completas para repetir a busca)"). Fonte da verdade do **comportamento**.

## 1. Por quê
Pedido do Alexandre (21/11/2026):
1. "Ele sempre imagina que é pra pesquisar na pasta atual e é pra procurar no banco todo, só procura no diretório específico se informado na barra de busca após a máscara de arquivos."
2. "Precisa guardar as buscas."

Hoje, com o motor Everything escolhido, a busca fica presa à pasta aberta no momento, o que joga fora a maior vantagem do Everything (achar em todo o computador na hora). E o histórico de buscas, embora exista, mostra no máximo uma busca anterior.

## 2. Histórias de usuário (por prioridade)
- **US1 (P1)** Como usuário do motor Everything, quero que a busca procure em todo o índice do Everything, qualquer que seja a pasta aberta.
- **US2 (P1)** Como usuário, quero limitar a busca a uma pasta escrevendo o caminho depois da máscara (ex.: `*.cs c:\desenv\KyraFiles`).
- **US3 (P1)** Como usuário, quero ver minhas buscas anteriores ao abrir a caixa de busca, todas elas (não só uma), e reexecutar com um clique.
- **US4 (P2)** Como usuário, quero que o histórico sobreviva ao fechar e reabrir o app.
- **US5 (P2)** Como usuário, ao digitar, quero ver primeiro as buscas anteriores que combinam com o que estou digitando.
- **US6 (P3)** Como usuário, quero tirar uma busca do histórico ou limpar o histórico todo.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | motor Everything, pasta `D:\Downloads` aberta, existe `C:\x\nota.txt` | busco `nota.txt` | `C:\x\nota.txt` aparece no resultado |
| AC-2 | motor Everything, qualquer pasta aberta | busco `*.cs c:\desenv\KyraFiles` | só aparecem arquivos `.cs` dentro de `c:\desenv\KyraFiles` e subpastas |
| AC-3 | motor Everything | busco `*.txt "C:\Program Files"` ou `*.txt C:\Program Files` (sem aspas) | só aparecem `.txt` dentro de `C:\Program Files` |
| AC-4 | motor Everything | busco `*.txt C:\pasta-que-nao-existe` | a busca roda com o texto como foi digitado (sintaxe do Everything) e o app não trava nem dá erro |
| AC-5 | motor Everything | busco com sintaxe própria do Everything (`path:`, `parent:`, `ext:`, regex) | o texto chega ao Everything sem alteração e sem pasta implícita |
| AC-6 | motor Everything | abro a caixa de busca e digito `nota` | as sugestões que aparecem enquanto digito também vêm de todo o índice |
| AC-7 | busca global com Everything | olho o título/endereço da página de resultados | ele diz que a busca foi em todo o computador (ou na pasta informada, em AC-2), não na pasta que estava aberta |
| AC-8 | motor Everything indisponível no momento da busca (não está rodando, `es.exe` falha) | busco | o app avisa que caiu para a busca na pasta atual, em vez de mostrar silenciosamente só a pasta atual |
| AC-9 | fiz as buscas A, B e C, nessa ordem | abro a caixa de busca vazia | vejo C, B, A (mais recente primeiro), todas |
| AC-10 | AC-9 | clico em B | a busca B é refeita e B vai para o topo do histórico |
| AC-11 | AC-9 | fecho e reabro o app e abro a caixa de busca | C, B, A continuam lá |
| AC-12 | histórico com `relatorio` e `nota` | digito `rel` | `relatorio` aparece como busca anterior acima dos resultados de arquivo |
| AC-13 | histórico com várias buscas | removo uma / limpo tudo | some a escolhida / somem todas, e continua assim depois de reabrir |
| AC-14 | busca global com Everything | uso "Salvar busca" (F042) | a busca salva guarda o escopo real: vazio (todo o índice) ou a pasta informada no texto, nunca a pasta que estava aberta |
| AC-15 | motores Nativo e Índice interno | busco | comportamento de hoje (pasta atual), sem mudança |
| AC-16 | busquei `*.cs c:\desenv\KyraFiles` e depois abri `D:\Downloads` | escolho essa busca no histórico | ela roda em `c:\desenv\KyraFiles`, não em `D:\Downloads` |
| AC-17 | fiz 25 buscas diferentes | abro o histórico | vejo as 20 mais recentes; as 5 mais antigas saíram |

## 4. Requisitos
- **FR-001** Com o motor Everything, a busca NÃO DEVE ser limitada à pasta aberta.
- **FR-002** Se o texto da busca terminar num caminho de pasta absoluto existente (letra de unidade ou rede), a busca DEVE ser limitada a essa pasta e às subpastas, e o restante do texto é a máscara/termo.
- **FR-003** Caminho com espaço DEVE funcionar com aspas e sem aspas.
- **FR-004** Texto com sintaxe própria do Everything (operadores `path:`, `parent:`, `infolder:` e equivalentes) DEVE ir ao Everything sem pasta implícita e sem alteração.
- **FR-005** Sugestões ao digitar seguem as mesmas regras de escopo de FR-001 a FR-004.
- **FR-006** A página de resultados DEVE informar o escopo real da busca.
- **FR-007** Queda para outro motor DEVE ser avisada na tela.
- **FR-008** O histórico DEVE guardar as 20 últimas buscas e mostrar todas, sem duplicatas, mais recente primeiro.
- **FR-009** O histórico DEVE persistir entre sessões.
- **FR-013** Cada busca do histórico DEVE guardar o necessário para repeti-la igual: o texto digitado, o escopo (pasta informada ou todo o índice; nos motores que buscam na pasta atual, a pasta de onde foi feita), o motor usado e a data. Repetir uma busca do histórico DEVE refazê-la com esse escopo e motor, não com a pasta aberta no momento.
- **FR-014** Buscas iguais (mesmo texto e mesmo escopo) contam como uma só; mesmo texto com escopo diferente são buscas distintas.
- **FR-010** Ao digitar, buscas anteriores que contêm o texto digitado DEVEM aparecer antes dos resultados de arquivo.
- **FR-011** O usuário DEVE poder remover um item ou limpar o histórico.
- **FR-012** "Salvar busca" com Everything DEVE usar o escopo real (FR-001/FR-002).
- **NFR-001** Zero regressão: tempo até o primeiro resultado com Everything igual ou menor que hoje (medido, mesmo script, mesma consulta, antes e depois).
- **NFR-002** O teto de resultados de hoje (500 por busca, 10 por sugestão) continua valendo, para a busca global não estourar memória.
- **NFR-003** Nenhuma janela de console aparece nem rouba o foco.

## 5. Casos de borda
- Texto só com caminho (`c:\desenv`): lista o conteúdo indexado daquela pasta.
- Caminho com barra final ou barras `/`: tratado como o mesmo caminho.
- Pasta de rede (`\\servidor\pasta`): vale se o Everything a indexa; se não indexa, resultado vazio, sem erro.
- Termo que parece caminho mas não existe: vai como texto (AC-4).
- Página Início, Lixeira, biblioteca, tag ou pasta virtual aberta: não muda nada (a busca já é global).
- Busca vazia não entra no histórico; busca repetida sobe para o topo em vez de duplicar.
- Busca por `tag:` continua com o comportamento atual.
- Só o serviço do Everything rodando, sem o programa aberto na sessão do usuário (visto no Heloize-PC em 21/11/2026): o Everything não responde e hoje o resultado volta vazio, sem aviso. Deve cair no aviso de AC-8.

## 6. Fora de escopo
- Mudar o escopo dos motores Nativo e Índice interno (continuam na pasta atual). DISCOVERY: aplicar a regra "caminho após a máscara" também a eles.
- Ordenação dos resultados do Everything, paginação além do teto, busca por conteúdo.
- Sincronizar histórico entre computadores.

## 7. Critérios de sucesso
- AC-1, AC-2, AC-9 e AC-11 confirmados no app aberto (UI Automation ou captura), não só por teste unitário.

## 8. Pontos resolvidos
- [RESOLVIDO 21/11/2026, Alexandre] "Guardar as buscas" = histórico automático com informação completa para repetir (FR-013). A barra lateral continua com "Salvar busca" manual (F042).
- [RESOLVIDO 21/11/2026, Alexandre] Tamanho: 20 buscas.

## 9. Histórico
- 21/11/2026: proposta inicial (Claude), a partir da investigação do código.
- 21/11/2026: aprovada pelo Alexandre; histórico de 20 buscas com informação completa (FR-013, FR-014); AC-16 e AC-17.
