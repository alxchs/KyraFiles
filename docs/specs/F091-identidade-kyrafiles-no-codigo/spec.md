# F091 — Identidade KyraFiles de ponta a ponta no código — SPEC

Status: **APROVADA e APLICADA por pedido do Alexandre em 13/11/2026** ("Eu pedi pra trocar a desgraca do nome faz mais de 30 dias. Primeiro verifica o nome dentro dos fontes URGENTE, comita, faz push e depois muda o nome do repo"). Estado anterior: rascunho de 12/11 ("Escreva, mas não aplique ainda").
Origem: decisão de 03/11/2026, que separou da F088 o renome interno do código para uma spec própria (F091). A spec nunca foi escrita; esta é ela.

Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui (isso é do `plan.md`).

## 1. Por quê
O produto já se chama KyraFiles para quem usa (marca, executáveis, instalador, telas). Por dentro, o código ainda se identifica pelo nome do projeto de origem. Isso confunde quem mantém, faz parecer que a troca ficou pela metade e já causou defeitos reais: script de release procurando arquivo com nome antigo, caminho de configuração apontando para pasta que não existe mais.

## 2. Histórias de usuário
- **US1 (P1):** Como mantenedor, quero que todo o código-fonte use o nome KyraFiles, para não haver dois nomes para a mesma coisa.
- **US2 (P1):** Como usuário que atualiza o app, quero continuar com minhas abas, preferências e atalhos exatamente como estavam, sem perceber que algo mudou por dentro.
- **US3 (P2):** Como mantenedor, quero que a documentação viva (regras, contexto, status, playbook) use o nome KyraFiles; o histórico (handoffs, evidências, relatórios antigos) fica como foi escrito.
- **US4 (P2):** Como mantenedor, quero um verificador que acuse qualquer nome antigo que volte a aparecer, para a regressão não passar despercebida.

## 3. Cenários de aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | O código-fonte depois da mudança | Procuro o nome antigo do projeto de origem como identificador de código | Não há ocorrência, exceto as da lista de exceções permitidas (§6) |
| AC-2 | Usuário com abas abertas salvas pela versão anterior, incluindo abas em modos de exibição diferentes | Atualiza e abre o app | Todas as abas voltam no mesmo endereço e no mesmo modo de exibição |
| AC-3 | Usuário com preferências, pastas fixadas, etiquetas e atalhos da versão anterior | Atualiza e abre o app | Tudo continua igual |
| AC-4 | App atualizado | Uso as funções que dependem de componentes auxiliares (diálogos de abrir/salvar, gerenciador padrão, Win+E, tarefa de atualização, serviço de índice, segunda instância) | Tudo funciona como antes |
| AC-5 | Build de release | Gero a release e o instalador | O processo termina sem erro e o pacote contém só nomes KyraFiles (fora as exceções de limpeza) |
| AC-6 | Documentação viva | Leio as regras, o contexto, o status e o playbook | O produto é chamado KyraFiles; o histórico continua intacto |
| AC-7 | Alguém reintroduz o nome antigo como identificador | Roda o verificador | O verificador falha e diz onde |
| AC-8 | App atualizado | Meço abertura, memória e CPU em repouso | Nada pior que a `main` anterior (regra de performance do projeto) |

## 4. Requisitos
- **FR-001:** Todo identificador de código (agrupamento de código, referência em telas, referências entre módulos, metadados usados por geradores de código) usa o nome KyraFiles.
- **FR-002:** Dados salvos pela versão anterior que guardem nomes internos continuam sendo lidos corretamente (compatibilidade de leitura); dados novos são gravados com o nome novo.
- **FR-003:** Identificadores que o Windows usa para registrar o app ou seus componentes (identidade do pacote, classes ativáveis, associações) não mudam nesta spec, salvo se já usarem o nome novo.
- **FR-004:** Scripts de build, release e instalador referenciam só nomes que o build realmente gera.
- **FR-005:** Ocorrências do nome antigo que existem para limpar instalações antigas (encerrar processos e apagar arquivos da versão anterior) continuam, marcadas como exceção.
- **FR-006:** Um verificador automático lista ocorrências do nome antigo fora das exceções e falha se houver alguma.
- **FR-007:** Documentação viva renomeada; documentação histórica intocada.
- **NFR-001:** Mudança mecânica, sem alteração de lógica; cada etapa compila sozinha.
- **NFR-002:** Executada com nenhuma outra branch de código aberta, para não gerar conflito em massa.
- **NFR-003:** Zero avisos novos no build de release.

## 5. Casos de borda
- Aba salva com um nome interno desconhecido: abre no modo padrão, como hoje (não pode travar nem perder o endereço).
- Usuário que pula versões (atualiza de uma versão bem antiga direto para esta): mesmas garantias de AC-2 e AC-3.
- Avisos de licença e cabeçalhos de copyright do projeto de origem: ficam (obrigação de licença), não contam como ocorrência.

## 6. Exceções permitidas (o nome antigo pode ficar)
- Avisos de licença e cabeçalhos de copyright.
- Limpeza de instalações antigas (processos e arquivos da versão anterior).
- Leitura de dados antigos (compatibilidade de FR-002).
- Documentação histórica (handoffs, evidências, relatórios, notas de versão já publicadas).

## 7. Fora de escopo
- Mudar a identidade do pacote no Windows (o sistema trataria como app novo; é a F087).
- Renomear o repositório no GitHub (ação do Alexandre no site; o remoto local é ajustado depois).
- Reescrever o histórico do git.
- Refatorar lógica, estrutura de pastas ou nomes de classes além do prefixo (a "refatoração total" ampla citada em 03/11 vira spec própria se o Alexandre quiser).

## 8. Pontos decididos (antes `[NEEDS CLARIFICATION]`)
1. **Escopo:** só o prefixo do nome (recomendação da Claude, adotada pela urgência do pedido). Refatoração ampla, se houver, vira spec separada e por área.
2. **Protocolo de ativação `files-dev:` e alias `files-dev.exe`:** ficam nesta entrega. Estão ligados ao registro do app no Windows (FR-003) e mudam junto com a identidade do pacote (F087), aceitando o antigo por uma versão.

## 9. Histórico
- 2026-11-12 — rascunho (Claude), a pedido do Alexandre. Não aplicar até aprovação de spec, plan e tasks e até a F092 estar na `main`.
- 2026-11-13 — aplicada (Claude) por pedido urgente do Alexandre, sem esperar a F036 (NFR-002 dispensado por ele): o renome é idempotente por `tools/rename-legacy-namespaces.ps1`, que a branch da F036 roda antes do rebase.
