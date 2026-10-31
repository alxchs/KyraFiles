# FILA KYRAFILES — ordem de trabalho da AGY (Claude, 31/10/2026)

Fonte do plano: artefato "Super Explorer: Plano de Módulos" (https://claude.ai/artifact/BX5PJvm1fX53UErsA7v2ep, privado). Este arquivo é autocontido: a AGY não precisa abrir o artefato.

Tag de segurança antes da fila: `tag_v4.3.10.0_f063-f065-correcoes-f067-pwsh-f068-docs-offline_salvo` (local, sem push). Base: `colab` em `30a43de27`.

## 0. Produto: KyraFiles

- O nome do produto passa a ser **KyraFiles**. Decisão do Alexandre em 31/10/2026.
- Logo "igual à do KyraDB": **BLOQUEADO.** Não existe arquivo de logo no repositório `C:\desenv\utils\KyraDB` (busca por `.svg`, `.png`, `.ico`, `.jpg`, `logo`, `brand`, em árvore e histórico git, retornou 0; o repositório só tem `docs`, `pesquisa` e `tools`). Sem o arquivo, a AGY **não inventa** a logo. Ela entrega tudo o que não depende dela (item 4, sub-specs a, b, c) e deixa a sub-spec d (ícones) em `WAITING` até o Alexandre informar onde está a arte.
- Regra de arte do Alexandre: nunca vermelho, nunca vermelho com amarelo, nunca vermelho com estrela amarela.
- Licença: arquivos que vieram do Files Community mantêm o aviso `Copyright (c) Files Community` e a licença MIT. Trocar nome e marca não autoriza apagar esse aviso.

## 1. Regras da fila (valem para todos os itens; descumprir = Claude reverte e o Alexandre é avisado)

1. **Um item por vez, na ordem abaixo.** Só começa o próximo depois de o anterior estar devolvido na `colab` (§18 do método).
2. **Uma branch por spec**, `feature/<id>-<slug>`, saindo da `colab` atualizada. Nunca na `main`.
3. **SDD:** para cada item a AGY escreve `docs/specs/<ID>-<nome>/spec.md`, `plan.md` e `tasks.md` (modelos em `~/.dev-method/templates/`), status RASCUNHO, faz um handoff e **para**. Só o Claude (ou o Alexandre) aprova. Sem `tasks.md` aprovado não há código.
4. **Sem tecnologia na spec.** Dúvida vira `[NEEDS CLARIFICATION]`. Cada tarefa cita `FR`/`AC`; cada teste cita o `AC`.
5. **Zero regressão de desempenho** (§18.5): antes de codar, meça o baseline da colab (tempo até a primeira janela e a operação afetada); depois, mesmos scripts. Pior que a colab = rejeitado.
6. **Relatório de conclusão** por spec, em `docs/specs/<ID>/RELATORIO.md`: entregas e evidências, arquitetura e arquivos tocados, riscos, comparativo de desempenho (§18.7).
7. **Prova de tela** só com o Alexandre ausente ou às 09:00, **sem roubar foco**, só com UI Automation ou o helper com trava (`tools/perf/Send-GuardedInput.ps1`). AC de interface sem prova fica `NOT TESTED`.
8. **Nunca** `git push`, tag, merge na colab ou na main, release. A integração (squash, rebase, ff) é do Claude depois da revisão.
9. Texto que o usuário final vê (notas de versão, avisos) segue o §14 do método: benefício em português simples, sem jargão.
10. Temporários em `%TEMP%\agy\<GUID>\desenv\utils\FilesApp\Files\`. Apague ao fim.
11. Dúvida de arquitetura ou de escopo: `PENDING DECISION` em `DECISIONS.md` e siga com outra tarefa independente.
12. Se a AGY estiver só no modelo flash, os itens 4 (identidade) e 9 (desfazer) exigem revisão do plano pelo Claude antes de qualquer código (mudam dado do usuário).
13. Cota: ao receber 429, aplique a troca de grupo e de conta descrita em `~/.claude/CLAUDE.md`. Falta de cota **não** passa o trabalho para o Claude.

## 2. A fila

Esforço e ganho são estimativas do Claude, a confirmar em cada plan. IDs de F069 em diante são provisórios; F051, F052 e F053 já existem.

### Item 0. Portão P0 (já em andamento)
F065-T11/T12, F063-C6/C7/C8, A1, A3 e os dois travamentos de `docs/agents/SITUACAO.md`. Provas às 09:00 de 31/10. **Nada abaixo começa antes de o Claude liberar.**

### Item 1. F069 Sair para o Explorer após quedas seguidas (baixo × médio)
- **Por quê:** hoje o lançador (F012) cai para o Explorer quando o programa falta ou está corrompido, mas não quando ele existe e fecha logo após abrir.
- **AC semente:** AC-1 duas quedas em 60 s abrem o Explorer na pasta pedida; AC-2 na próxima abertura manual um aviso explica o que houve e oferece tentar de novo; AC-3 queda isolada não aciona nada; AC-4 o motivo fica registrado em log local.
- **Fora de escopo:** casca própria leve (só se este item provar necessidade).
- **Pontos a decidir:** o que conta como queda (código de saída, tempo de vida menor que N s); onde guardar o contador.

### Item 2. F070 Login do GitHub próprio (baixo × médio) — sub da F079
- **Por quê:** o login usa o marcador `githubclientid.secret` (`src/Files.App/Constants.cs`), que no fork provavelmente nunca recebeu um valor real (INFERRED, não testado). Por isso "conectar GitHub" não funciona.
- **Antes de tudo:** reproduzir e registrar o erro real (CONFIRMED ou refutado).
- **AC semente:** AC-1 entrar com o código de dispositivo funciona e mostra o usuário; AC-2 sair apaga a credencial; AC-3 a credencial fica no cofre do Windows, nunca em arquivo de configuração; AC-4 sem identificador configurado, o item explica o que falta em vez de falhar calado.
- **Depende do Alexandre:** criar um aplicativo OAuth no GitHub (fluxo de dispositivo) e informar o identificador público. A AGY faz o identificador configurável, sem segredo no código. Sem o identificador, os ACs 1 e 2 ficam `NOT TESTED`; avisar no handoff.

### Item 3. F071 Integridade e arquivos baixados (baixo × médio)
- **Sub-specs:** a) hash e conferência; b) liberar a marca de internet em lote.
- **AC semente a:** hash (algoritmos a decidir) de arquivo de vários GB sem carregar o arquivo inteiro na memória (pico medido); comparar com valor colado; cancelar a qualquer momento.
- **AC semente b:** selecionar vários arquivos baixados e liberar o bloqueio de uma vez; mostrar quantos tinham a marca; nada muda nos que não tinham.
- **Fora de escopo:** verificação de assinatura digital (vira ideia futura).

### Item 4. F081 Identidade KyraFiles (alto, base)
- **Sub-specs, cada uma com sua branch e devolução:** a) decisão de nome, paleta e como o nome aparece (o nome está decidido: KyraFiles; falta paleta e textos) · b) identidade do pacote, pasta de dados do usuário e **migração das configurações** (quem usa o Files Custom não perde nada) · c) renomeio interno em lote (namespaces, nomes de arquivo, textos), com o aviso de copyright mantido · d) ícones e logo (**WAITING** pela arte do KyraDB).
- **Regra de execução:** nenhuma outra spec em paralelo durante o item 4; todas as branches abertas fecham antes.
- **AC semente:** AC-1 instalação nova abre com as configurações da versão anterior; AC-2 nenhum texto visível ao usuário cita o nome antigo, exceto o aviso de licença; AC-3 build `0 Error(s)` e `0 Warning(s)`; AC-4 atualizador acha a versão nova pelo canal novo; AC-5 sem vermelho nem vermelho com amarelo em nenhuma arte.
- **Riscos para o plan tratar:** identidade do pacote muda e o usuário pode precisar reinstalar (INFERRED); migração falha pela metade; canal de atualização antigo. Pedir decisão do Alexandre sobre o que acontece com quem está na versão antiga.

### Item 5. F052 Visão de repositório Git (médio × alto, já aprovada)
Segue a spec existente `docs/specs/F052-git-repo-view/`. A AGY só escreve `plan.md` e `tasks.md` (e atualiza nomes pelo item 4, se for o caso).

### Item 6. F051 Busca de conteúdo estilo ripgrep (médio × alto, já aprovada)
Segue `docs/specs/F051-code-search/`. Só plan e tasks. Depende da F065 devolvida.

### Item 7. F072 Painel de busca avançada (médio × alto)
- **Sub-specs:** a) critérios sobre o índice (nome com curinga e regex, tipo, tamanho, datas, atributos, local); b) buscas salvas e recentes; c) exportar resultado.
- **Depende de:** F051 e F065.
- **AC semente:** AC-1 combinar nome, tamanho e datas e ver o resultado crescer enquanto a busca anda; AC-2 salvar e reabrir a busca; AC-3 regex inválida mostra o motivo e não trava; AC-4 cancelar sempre funciona.
- **Regra:** desenho próprio, no estilo do app. Não copiar layout, textos nem ícones do Explorer ou de outro produto.
- **Pontos a decidir:** o índice guarda tamanho e datas? (verificar no código; se não, medir o custo de passar a guardar).

### Item 8. F073 Regras automáticas de pastas (médio × alto)
- **Sub-specs:** a) motor de regras (condição por tipo, idade, nome; ação mover, renomear, mandar para a lixeira); b) tela de regras; c) execução em segundo plano segura.
- **AC semente:** AC-1 toda regra mostra **prévia** do que fará antes de ligar; AC-2 tudo o que a regra faz entra num histórico e pode ser desfeito; AC-3 nenhuma regra apaga sem lixeira; AC-4 regra com erro para sozinha e avisa; AC-5 desligar uma regra a para na hora.
- **Pontos a decidir:** quando rodam (ao abrir, em intervalo, ao detectar arquivo novo); o que acontece com arquivo em uso.

### Item 9. F074 Desfazer de operações de arquivo (médio × alto)
- **Antes de tudo:** verificar e registrar o que o desfazer atual já cobre (CONFIRMED com comando). A spec cobre só a diferença.
- **AC semente:** AC-1 desfazer mover, renomear e apagar para a lixeira; AC-2 o histórico sobrevive a fechar e reabrir; AC-3 se o destino mudou, o desfazer avisa e não sobrescreve; AC-4 limite configurável do histórico.
- **Cuidado:** mexe em dado do usuário; prova com pasta de teste própria, nunca com arquivos reais.

### Item 10. F075 Duplicados · F076 Converter em lote · F053 Codificação e fim de linha (médio × médio)
Três specs, **três branches, em série**, nesta ordem. F053 já tem spec aprovada (só plan e tasks).
- **F075 (depende do hash da F071):** achar cópias por conteúdo, comparar, mandar para a lixeira com prévia. AC: nunca apaga sem prévia e sem lixeira; mantém sempre pelo menos uma cópia por grupo.
- **F076:** imagem (tamanho, formato) e PDF (juntar, dividir) em lote. AC: o arquivo de origem nunca é sobrescrito por padrão; programa externo (ex.: para vídeo) só sob demanda e nunca embarcado no pacote.

## 3. Entregas esperadas da AGY, por item

1. Handoff `docs/agents/handoffs/AAAA-MM-DD_HHMM_agy_<item>-spec.md` com a spec, plan e tasks em RASCUNHO e os pontos `[NEEDS CLARIFICATION]`.
2. **Parar** e esperar o Claude. Não avançar para o próximo item.
3. Depois da aprovação: código por tarefa (um commit cada), roteiro de teste, medições, `RELATORIO.md`, handoff final com o estado por `AC-n` (`NOT TESTED`, `OBSERVED`, `INFERRED`, `CONFIRMED`, cada `CONFIRMED` com comando e saída).

## 4. O que depende do Alexandre (o Claude avisa no Telegram)

- Arquivo da logo do KyraDB (item 4d).
- App OAuth do GitHub e o identificador público (item 2).
- Decisão sobre usuários da versão antiga na troca de identidade (item 4b).
- Provas de tela das 09:00 de 31/10 (item 0).

## 5. Estado

Criado em 31/10/2026 pelo Claude. Nenhum item desta fila foi iniciado. Nada foi enviado ao remoto.
