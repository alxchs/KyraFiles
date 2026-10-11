# F015 — Índice de busca próprio (todos os discos, sem duplicatas) — SPEC

Status: APROVADA (Alexandre, 11/10/2026; pontos em aberto decididos em `DECISIONS.md` D-019). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui: isso é do `plan.md`.

Substitui, para busca por nome, os motores da F011 (Agent Ransack e Everything). O F3 da F013 (abrir o Agent Ransack) continua como está.

## 1. Por quê

Queixa do Alexandre (11/10/2026): "O pesquisar está extremamente lento. Parecendo trabalho de principiante." Medido no mesmo dia: a busca por nome com o motor configurado (Agent Ransack) levou 8,3 s em `C:\desenv` (86.954 arquivos), mais que uma listagem simples do PowerShell (5,9 s), e a tela fica vazia até o fim. O Everything resolve isso, mas a decisão é **não depender dele**: o Files precisa de um índice próprio, **igual ou melhor** em velocidade, cobrindo **todos os discos**, sem indexar duas vezes o mesmo disco e sem mostrar duas vezes o mesmo arquivo quando ele é alcançável por mais de um caminho (letra criada com `subst`, link simbólico, junção, volume montado em pasta, unidade com duas letras).

Decisão de arquitetura (Alexandre, 11/10/2026, opção C): dois modos, **completo** (com um componente instalado uma vez com autorização de administrador, indexação e atualização no nível do sistema de arquivos) e **básico** (sem privilégio, sempre disponível). O básico funciona sozinho; o completo, quando o usuário aceita instalá-lo, assume os volumes que suporta.

## 2. Histórias de usuário (por prioridade)

- **US1 (P1)** Como usuário, quero digitar parte do nome e ver os arquivos de **todos os meus discos** aparecerem enquanto digito, para achar qualquer coisa sem saber onde está.
- **US2 (P1)** Como usuário, quero que buscar dentro da pasta atual (e subpastas) seja igualmente instantâneo, para não depender de varrer o disco a cada busca.
- **US3 (P1)** Como usuário, quero que cada arquivo apareça **uma vez só**, mesmo que eu chegue a ele por uma letra `subst`, um link ou um volume montado em pasta.
- **US4 (P1)** Como usuário, quero que o índice reflita o que mudou no disco (criado, renomeado, movido, apagado) em segundos, sem eu mandar reindexar.
- **US5 (P2)** Como usuário sem querer dar privilégio de administrador, quero que a busca funcione mesmo assim (modo básico), sabendo o que perco.
- **US6 (P2)** Como usuário, quero ver o estado do índice (quais discos, em que modo, quantos itens, indexando/pronto) e poder incluir, excluir e reindexar um disco ou pasta.
- **US7 (P3)** Como usuário brasileiro, quero que `acao` encontre `Ação.docx` (busca sem distinção de maiúsculas **e de acentos**), o que o Everything não faz por padrão.

## 3. Cenários de aceite

Ambiente de referência (máquina do Alexandre, levantado em 11/10/2026): C:, D:, R: (NTFS, fixos), S: (NTFS, USB), volume virtual do Google Drive montado em `C:\Google\Drive\disk` (relatado como FAT32), dois volumes de sistema sem letra.

| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | índice pronto, modo completo | digito `ShellViewModel` na busca de "Este computador" | os resultados de todos os discos aparecem em menos de 100 ms após a última tecla, sem tela vazia de espera |
| AC-2 | índice pronto | busco `ShellViewModel` estando em `C:\desenv` | só itens dentro de `C:\desenv` (e subpastas), em menos de 100 ms, mesmo número de resultados que uma varredura completa da pasta |
| AC-3 | `subst X: C:\desenv` criado | busco em "Este computador" | cada arquivo de `C:\desenv` aparece **uma vez**, pelo caminho `C:\...`; o volume não é indexado de novo |
| AC-4 | `subst X: C:\desenv` criado | busco estando em `X:\utils` | os resultados aparecem com caminho `X:\utils\...` (o caminho por onde estou navegando), uma vez cada |
| AC-5 | junção ou link simbólico `C:\atalho` → `D:\projetos` | busco em "Este computador" | cada arquivo de `D:\projetos` aparece uma vez, pelo caminho real `D:\projetos\...`; o link em si aparece como item (é um nome que existe), mas o conteúdo não é repetido por baixo dele |
| AC-6 | o mesmo link de AC-5 | busco estando em `C:\atalho` | os resultados aparecem com caminho `C:\atalho\...`, uma vez cada |
| AC-7 | volume montado só em pasta (sem letra) | busco em "Este computador" | o conteúdo do volume aparece uma vez, pelo caminho da pasta de montagem |
| AC-8 | volume com letra **e** montado em pasta | busco em "Este computador" | o conteúdo aparece uma vez, pelo caminho com a letra |
| AC-9 | link que aponta para um ancestral dele mesmo (ciclo) | o índice é construído e busco | a indexação termina, nada é listado infinitamente nem duplicado |
| AC-10 | Files aberto, índice pronto | crio, renomeio, movo e apago um arquivo pelo Explorer do Windows | a busca reflete cada mudança em até 2 s (modo completo) ou até 5 s (modo básico) |
| AC-11 | Files fechado, modo completo | crio e apago arquivos, depois abro o Files | a busca já reflete as mudanças sem reindexar o disco inteiro |
| AC-12 | Files fechado, modo básico | crio e apago arquivos, depois abro o Files | a busca reflete as mudanças depois de uma reconferência que não bloqueia o uso (enquanto isso, o estado mostra "conferindo") |
| AC-13 | conecto o disco USB S: | — | ele entra no índice sozinho; ao desconectar, seus resultados somem da busca; ao reconectar sem mudanças, não é reindexado do zero |
| AC-14 | componente do modo completo não instalado | uso a busca | funciona no modo básico, e o estado do índice diz qual modo está ativo e o que se ganha instalando o completo |
| AC-15 | busco `acao` | — | `Ação.docx`, `AÇÃO.txt` e `acao.md` aparecem |
| AC-16 | outro usuário do Windows tem arquivos no perfil dele | busco como usuário comum | não aparece nenhum nome de arquivo ou pasta que eu não teria permissão de listar |
| AC-17 | volume do Google Drive (arquivos só na nuvem) | o índice é construído | nenhum arquivo é baixado da nuvem por causa da indexação ou da busca |
| AC-18 | a busca está rodando | troco de pasta, apago o texto ou pressiono Esc | a busca para na hora e a tela não mostra resultados velhos |
| AC-19 | índice pronto | abro um resultado, uso o menu de contexto, ordeno por tamanho ou data | funciona como numa pasta comum; tamanho e data aparecem sem esperar o disco |
| AC-20 | busca por nome com o componente e o índice prontos | comparo com o Everything na mesma máquina e consulta | o Files não é mais lento (mesma ordem de grandeza, medido com o mesmo script) e acha os mesmos arquivos, salvo as regras de duplicata desta spec |
| AC-21 | arquivo `D:\a\orig.txt` com um segundo nome `D:\b\copia.txt` (link físico) | busco `txt` em "Este computador" | aparece só `D:\a\orig.txt` |
| AC-22 | unidade de rede mapeada | busco em "Este computador" e depois dentro da unidade | no primeiro caso ela não entra; no segundo, os resultados aparecem aos poucos, por varredura, e Esc cancela |

## 4. Requisitos

### Cobertura
- **FR-001** O índice DEVE cobrir, por padrão, todos os volumes locais com letra ou ponto de montagem: fixos e removíveis.
- **FR-002** Volumes de sistema sem letra e sem ponto de montagem (recuperação, EFI) NÃO DEVEM ser indexados nem aparecer.
- **FR-003** O usuário DEVE poder excluir um volume ou pasta do índice e incluir de volta, e pedir reindexação de um volume.
- **FR-004** Volumes de nuvem (arquivos sob demanda) DEVEM ser indexados só pelo que já está visível localmente, sem provocar download (AC-17).

### Sem duplicata
- **FR-010** Cada volume físico DEVE ser indexado **uma única vez**, identificado pela sua identidade de volume, não pela letra ou pelo caminho por onde é visto.
- **FR-011** Letras criadas com `subst` e letras de rede que apontam para pasta local NÃO DEVEM gerar indexação própria; são traduções de caminho para um volume já indexado.
- **FR-012** A indexação NÃO DEVE entrar em links simbólicos, junções nem pontos de montagem: o link é registrado como item (com o destino dele), e o conteúdo do destino é indexado só no seu lugar real.
- **FR-013** Volume montado em pasta e também com letra DEVE ser apresentado pelo caminho com letra. Volume montado só em pasta, pelo caminho da pasta.
- **FR-014** Uma busca DEVE devolver cada arquivo no máximo uma vez.
- **FR-016** Arquivo com vários nomes no mesmo volume (link físico) DEVE aparecer **uma vez, no local original**: o primeiro nome que o sistema de arquivos registra para ele (em geral, o nome com que foi criado). Os outros nomes não aparecem na busca. Se a busca estiver restrita a uma pasta que só contém um dos outros nomes, esse arquivo não aparece ali.
- **FR-017** Unidades de rede NÃO DEVEM ser indexadas. Buscar dentro de uma delas DEVE continuar varrendo na hora, com resultados progressivos e cancelamento (FR-023). A busca em "Este computador" não inclui unidades de rede.
- **FR-015** Quando a busca parte de um caminho alternativo (letra `subst`, pasta-link), o escopo DEVE ser traduzido para o lugar real, e os resultados DEVEM voltar escritos no caminho alternativo por onde o usuário está navegando (AC-4, AC-6).

### Busca
- **FR-020** A busca por nome DEVE ser sem distinção de maiúsculas e de acentos (AC-15).
- **FR-021** Várias palavras DEVEM significar "todas presentes no nome", em qualquer ordem. `*` e `?` DEVEM funcionar como curingas.
- **FR-022** O escopo DEVE ser a pasta atual com subpastas, ou todos os discos quando a busca parte de "Este computador" ou da página inicial.
- **FR-023** Os resultados DEVEM aparecer de forma progressiva e a busca DEVE ser cancelável a qualquer momento (AC-18).
- **FR-024** Cada resultado DEVE trazer nome, caminho, tamanho, datas e atributos vindos do índice, sem consultar o disco item por item para mostrar a lista (AC-19).
- **FR-025** A busca NÃO DEVE mostrar nome de item que o usuário atual não teria permissão de listar (AC-16).
- **FR-026** Itens ocultos e de sistema DEVEM seguir a mesma preferência de exibição que o Files já aplica às pastas.

### Atualização
- **FR-030** Com o Files aberto, mudanças no disco DEVEM chegar ao índice em até 2 s (completo) ou 5 s (básico).
- **FR-031** No modo completo, mudanças feitas com o Files fechado DEVEM ser recuperadas sem reindexação total, salvo se o registro de mudanças do sistema tiver sido perdido ou reiniciado; nesse caso o volume é reindexado e o estado diz por quê.
- **FR-032** No modo básico, ao abrir, uma reconferência DEVE atualizar o índice em segundo plano, sem bloquear a busca (que usa o índice anterior enquanto isso).
- **FR-033** Disco removível desconectado DEVE sair dos resultados; ao reconectar, DEVE ser reaproveitado o índice anterior se o disco não mudou.

### Modos e estado
- **FR-040** O modo básico DEVE funcionar sem privilégio de administrador nem instalação adicional.
- **FR-041** O modo completo DEVE ser instalado só com aceite explícito do usuário (uma vez), DEVE poder ser removido pelas configurações, e sua ausência ou falha DEVE fazer a busca cair no modo básico **avisando**, nunca em silêncio.
- **FR-042** O Files DEVE mostrar o estado do índice por volume: modo, itens, situação (indexando com progresso, pronto, conferindo, erro com motivo).
- **FR-043** Volume que o modo completo não suporta DEVE ser coberto pelo modo básico.

### Não funcionais
- **NFR-001** Busca por nome em todos os discos, com o índice pronto: primeiros resultados em < 100 ms e lista completa em < 300 ms para até 5 milhões de itens indexados. Medido por script versionado, do texto digitado à lista desenhada.
- **NFR-002** Indexação inicial no modo completo: o C: desta máquina em < 60 s. Modo básico: tempo medido e publicado (sem meta prévia, porque depende de varrer pasta por pasta); a busca DEVE funcionar parcialmente durante a indexação.
- **NFR-003** Memória: reportar, medida, a RAM do índice por milhão de itens. Meta: até 100 MB por milhão no processo que guarda o índice, e o Files sem crescimento proporcional ao índice quando a busca não está em uso.
- **NFR-004** Abertura do Files com o índice já existente: a busca disponível em < 2 s, sem atrasar a abertura da janela.
- **NFR-005** Indexação e atualização em segundo plano não devem tornar a máquina perceptivelmente mais lenta: prioridade baixa de CPU e disco e limite de uso medido.
- **NFR-006** O índice em disco DEVE ficar protegido para que outro usuário não leia nomes de arquivo alheios.
- **NFR-007** A escolha de motor da F011 é substituída: o índice próprio é o padrão para busca por nome. Agent Ransack continua como ferramenta de conteúdo pelo F3.

## 5. Casos de borda

- Ciclos de links (AC-9), link quebrado, link para unidade de rede, link relativo.
- `subst` criado ou removido com o Files aberto; `subst` de pasta dentro de outro `subst`.
- Volume com duas letras, letra trocada pelo Gerenciamento de Disco, mesmo disco USB em outra letra.
- Disco cheio, volume somente leitura, volume protegido por BitLocker ainda bloqueado (fica fora até ser desbloqueado, com estado dizendo isso).
- Nomes com caracteres raros (emoji, combinação de acento composta e decomposta), caminhos com mais de 260 caracteres, nomes com vírgula ou aspas.
- Pastas sem permissão de leitura durante a indexação básica.
- Milhões de arquivos numa pasta só, ou árvores de dezenas de níveis (`node_modules`).
- Hibernação ou suspensão com mudanças pendentes.
- Mudança de usuário do Windows (troca rápida), dois usuários com o Files aberto.
- Busca com texto vazio, só espaços, só curinga `*`, que casaria com milhões de itens: o resultado é limitado e a tela diz que foi limitado.
- Componente do modo completo de versão diferente do Files (atualização de um sem o outro).

## 6. Fora de escopo

- Busca dentro do **conteúdo** dos arquivos (fica para uma spec futura; o Agent Ransack via F3 cobre).
- Indexar unidades de rede e compartilhamentos remotos (FR-017).
- Sintaxe avançada (regex, `size:`, `date:`, `ext:`). Fica para depois, e o índice já guarda os dados que ela vai precisar.
- Remover o código dos provedores da F011 (fica desativado da busca por nome; a remoção vira tarefa à parte).

## 7. Critérios de sucesso

- O Alexandre digita um nome na busca e vê resultados de todos os discos "na hora", sem perceber espera, comparável ao Everything lado a lado (AC-1, AC-20).
- Com `subst` e links criados de propósito, nenhum arquivo aparece duplicado (AC-3 a AC-9).
- Números de tempo e de memória publicados com o comando que os mediu, antes e depois.

## 8. Pontos em aberto

Nenhum. Os dois pontos do rascunho foram decididos pelo Alexandre em 11/10/2026 (D-019):
1. Unidades de rede: não indexar, busca nelas por varredura na hora (FR-017).
2. Links físicos: mostrar uma vez, no local original (FR-016).

## 9. Histórico

- 11/10/2026 — rascunho criado a partir da queixa de lentidão, da medição do Agent Ransack e da decisão do Alexandre pela opção C (índice próprio, modo completo e básico, sem duplicatas de `subst`/links).
- 11/10/2026 — APROVADA. Acrescentados FR-016 (link físico uma vez, no local original) e FR-017 (unidade de rede fora do índice), decisões do Alexandre.
