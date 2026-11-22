# F011 — Motor de busca escolhido pelo usuário (F3 / Ctrl+F) — SPEC

Status: **APROVADA** pelo Alexandre em 12/10/2026.
Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Por quê
O usuário precisa de liberdade para escolher o motor de pesquisa integrado mais eficiente para seu fluxo de trabalho (busca nativa do Windows ou motor de indexação instantânea Everything), alternando a experiência de busca acionada por `F3` ou `Ctrl+F`.

---

## 2. Requisitos
- **FR-001:** Opção em Configurações > Busca para definir o provedor de busca ativo (`Native` ou `Everything`).
- **FR-002:** Interface `ISearchProvider` unificada permitindo alternância transparente sem recodificar a UI de resultados.
- **FR-003:** Fallback automático para a busca nativa quando o motor selecionado não estiver disponível na máquina.

---

## 3. Critérios de Aceite
- **AC-1:** Configurações exibe a escolha do motor e persiste a preferência.
- **AC-2:** Acionar `F3` ou `Ctrl+F` executa a consulta pelo motor configurado.
- **AC-3:** Resultados retornam de forma uniforme como itens da lista.
