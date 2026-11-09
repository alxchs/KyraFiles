# Handoff: Descontinuação da Branch `colab` e Alinhamento com Claude

- **Data/Hora:** 2026-11-09 07:30 BRT
- **Autor:** AGY (Antigravity)
- **Destinatário:** Claude / Alexandre
- **Decisão do Alexandre:** "Apague primeiro as branches que já foram integradas a main e colab, alias, pode apagar a colab local e remoto e vamos trabalhar apenas com a main a partir de agora. alinhe isso com a claude"

---

## 1. O que foi feito

### 1.1 Exclusão de Branches Integradas (Local e Remoto)
Todas as branches que já tiveram seus conteúdos e requisitos totalmente integrados na `main` foram removidas:
- **Localmente removidas:**
  - `colab` (estava em `fc2101f59`, idêntica à `main`)
  - `feature/f089-fontes` (integrada via commit `fc2101f59`)
  - `feature/f088-legacy-path-residues` (integrada via commit `b4897aa82`)
  - `feature/v4.3.12-release-installer` (integrada via commit `2d062627e`)
  - `backup/main-pre-f088-fase0`
  - `feature/f088-fase0-residuos`
  - `feature/installer-4.3.11`
  - `feature/kyrapet-settings-footer`
  - `feature/launcher-rename-consistency`
  - `feature/f052-git-repo-view`
- **Remotamente removidas (`origin`):**
  - `colab`
  - `feature/f089-fontes`
  - `feature/kyrapet-settings-footer`
  - `feature/launcher-rename-consistency`
- **Sincronização de referências:** `git remote prune origin` executado com sucesso.

---

## 2. Novo Fluxo Operacional: Exclusivamente com a `main`

A branch `colab` deixa de existir como intermediária. O ciclo de desenvolvimento passa a ser simplificado, mantendo todas as salvaguardas de qualidade e integridade:

1. **Base de trabalho:** Toda nova spec/feature parte exclusivamente da `main` atualizada (`git fetch origin`, `git checkout -b feature/<id>-<slug> main`).
2. **Regra de Ouro da `main` (Imutável):** É terminantemente PROIBIDO commitar direto na `main`. Todo desenvolvimento ocorre em branch `feature/<nome>`.
3. **Integração na `main`:** Squash dos commits da feature + rebase sobre `origin/main` + merge fast-forward (`git merge --ff-only`), gerando exatamente um commit atômico, limpo e linear por feature na `main`.
4. **Desempenho e Qualidade:** Zero regressão de performance em relação ao baseline da `main` anterior.
5. **Critério de Pronto e Sequência:** Cada spec concluída é integrada na `main` antes do início da próxima.

---

## 3. Arquivos de Governança Atualizados

- `AGENTS.md`: Regra de Ciclo atualizada formalizando a descontinuação da `colab` e o fluxo direto `feature/*` -> `main`.
- `AGENT_CONTEXT.md`: Registrada a decisão do Alexandre de 09/11/2026 e o estado limpo das branches.
- `docs/agents/PLAYBOOK.md`: Procedimentos atualizados para focar exclusivamente na `main`.
- `docs/agents/FILA-KYRAFILES.md`: Fila de prioridades ajustada para o fluxo da `main`.

---

## 4. Próxima Ação

A próxima spec liberada na fila é a **F090 (Escala da Interface)**:
- Branch a criar: `feature/f090-escala` saindo da `main` (`fc2101f59`).
- Pasta de especificações: `docs/specs/F090-escala-da-interface/` (`spec.md`, `plan.md`, `tasks.md`).
