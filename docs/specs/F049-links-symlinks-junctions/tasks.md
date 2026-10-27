# F049 — Links simbólicos, junctions e hardlinks — TASKS

Status: APROVADAS (Claude, 24/10/2026). **Bloqueadas** até o Claude liberar. Branch: `feature/f049-links`. Uma tarefa por commit. Regras: as de `docs/agents/ORDEM-RIGIDA-AGY.md`. **Todo teste que apaga ou copia roda só em pasta de teste em `%TEMP%\agy\<GUID>\...`.**

- [ ] **T0 — Confirmar o plan no código (somente leitura).** O que `IsSymLink` marca de fato; onde ficam as colunas do layout de detalhes; como a enumeração devolve atributos (reparse point); onde a exclusão trata links; se `FolderSizeProvider` segue links; como junction será criada. Entregar `docs/agents/evidence/F049/t0.md` com comandos. Pronto: cada afirmação marcada CONFIRMED/INFERRED.
- [ ] **T1 — Controle negativo de segurança (FR-003; AC-3, AC-6).** **Antes de mudar qualquer código**, escrever o teste que, na versão atual, exclui um link de pasta e confere se o destino foi tocado, e calcula o tamanho de uma pasta com link. Registrar o resultado atual (pode reprovar: é o objetivo). Pronto: CSV com o comportamento **atual**. Se a exclusão atual apagar o destino, avisar o Claude na hora (é bug P0).
- [ ] **T2 — Criar links (FR-001; AC-1, AC-5).** Ação e diálogo para simbólico, junction e hardlink, com o recuo explicado quando falta permissão. Pronto: build 0/0; teste cria os três tipos na pasta de teste e confere com a API do sistema.
- [ ] **T3 — Mostrar e ler o destino (FR-002; AC-2, AC-4, NFR-001).** Marcação por atributo da enumeração, destino lido em segundo plano, coluna "Destino do link" desligada por padrão, link quebrado sem exceção. Pronto: listar pasta de 10 mil itens sem aumento medido de tempo (antes/depois, CSV).
- [ ] **T4 — Excluir sem seguir e tamanho sem duplicar (FR-003; AC-3, AC-6).** Corrigir só o que o T1 mostrou errado. Pronto: o teste da T1 agora passa; contagem de arquivos do destino igual antes e depois.
- [ ] **T5 — Copiar link (AC-7).** Diálogo "copiar o link ou o conteúdo". Pronto: teste com link relativo.
- [ ] **T6 — Prova de UI (AC-1 a AC-7).** Instância própria, helper revisado, estrutura de teste: menu, diálogo, coluna. `resultado.csv` com PASS por AC e controle negativo; captura da janela inteira como anexo.
- [ ] **T7 — Fechamento.** `STATUS.md`, `CHANGELOG.md`, `AGENT_CONTEXT.md`, handoff por AC. Claude revisa antes do merge (squash + rebase, versão incrementada).
