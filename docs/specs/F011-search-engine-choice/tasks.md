# F011 — Motor de busca escolhido pelo usuário (F3 / Ctrl+F) — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

- [x] **T1 — Interface e Fábrica de Provedores:**
  - Criar `ISearchProvider` e `SearchProviderFactory.cs`.
  - Implementar suporte aos motores `Native` e `Everything`.

- [x] **T2 — Integração com Ações Globais:**
  - Ligar `SearchAction` (F3 / Ctrl+F) ao provedor selecionado.
  - Adicionar opção nas Configurações.

- [x] **T3 — Validação e Conclusão:**
  - Testar execução com e sem Everything ativo, comprovando fallback automático e entrega de resultados na lista.
