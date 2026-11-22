# Relatório de Conclusão e Aceite: SPEC-0009 (F011 — Motor de busca escolhido pelo usuário)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 3 pts  
**Evidências:** `SearchProviderFactory.cs`, `EverythingSearchProvider.cs`, `NativeFilesSearchProvider.cs`

---

## 1. Escopo e Implementação
Implementação da infraestrutura de escolha de motor de busca:
- `ISearchProvider` unificando busca nativa e Everything.
- `SearchProviderFactory` instanciando o motor configurado com fallback automático para Native caso Everything não esteja ativo.
- `SearchAction` (F3 e Ctrl+F) acionando transparentemente a fábrica.
- Entrega de resultados uniformes como `ListedItem` na lista de arquivos.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Escolha do motor configurável e persistida | `CONFIRMED` | `FoldersSettingsService.cs` |
| **AC-2** | F3 e Ctrl+F acionam o motor selecionado | `CONFIRMED` | `SearchAction.cs` |
| **AC-3** | Fallback transparente sem erros na ausência do Everything | `CONFIRMED` | `SearchProviderFactory.cs` |
