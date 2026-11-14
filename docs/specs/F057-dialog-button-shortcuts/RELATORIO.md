# F057 — Atalhos de Botões em Caixas de Diálogo
## Relatório de Conclusão e Evidências

**Data:** 2026-11-14 17:45  
**Autor:** Antigravity (AGY)  
**Revisor Técnico:** Claude (Arquiteta) e Alexandre  
**Branch:** `feature/f057-dialog-button-shortcuts`  
**Base:** `main` @ `1130aa0bf` (v4.3.15.0)

---

### 1. Resumo da Entrega

A spec **F057** adicionou atalhos de teclado (aceleradores/letras de acesso sublinhadas nos botões) em todas as caixas de diálogo do KyraFiles, permitindo acionamento ágil por teclado (`Alt` + letra ou letra direta):

1. **Lógica Pura de Alocação de Letras (`DialogAccessKeys.cs` em `KyraFiles.App.Helpers`):**
   - Algoritmo de atribuição determinístico que prioriza o primeiro termo de cada botão/opção.
   - Resolução inteligente de colisões: quando dois botões compartilham a mesma inicial (ex: *Create*, *Cancel*, *Copy*), o algoritmo seleciona o início da próxima palavra ou uma consoante/vogal livre, garantindo **zero letras duplicadas** na mesma caixa (`FR-003`).
   - Normalização de acentos (ex: *Éxito* mapeia para a tecla `E`).
   - Tratamento seguro de textos não-latinos e entradas nulas/vazias.

2. **Integração nas Caixas de Diálogo (`DialogShortcuts.cs`):**
   - Vinculado dinamicamente via `DialogShortcuts.Attach()` em `DefaultContentDialogStyle`, `DynamicDialog`, `ChooseDetailsDialog` e nas chamadas centralizadas de `TryShowAsync`.
   - Adiciona sublinhado visual (`AccessKeyDisplayLocations`) nos botões padrão (Primário, Secundário, Fechar) e em caixas de seleção/opções adicionais.

---

### 2. Matriz de Aceite e Evidências

| ID | Cenário | Condição | Resultado | Evidência |
|---|---|---|---|---|
| **AC-1** | Caixa Delete (DCP) | *Delete*, *Cancel*, *Permanently delete* recebem `D`, `C`, `P` | **PASS** | `DialogAccessKeysTests.DeleteDialog_UsesDCP` e `caixa_excluir.png` |
| **AC-2** | Prefixos Idênticos | *Create*, *Cancel*, *Copy* recebem letras distintas | **PASS** | `DialogAccessKeysTests.SamePrefix_GetsDifferentLetters` |
| **AC-3** | Letras Acentuadas | *Éxito*, *Cancelar* normalizam para `E` e `C` | **PASS** | `DialogAccessKeysTests.AccentedLetters_UseTheBaseLetter` |
| **AC-4** | Textos Não-Latinos | Textos em japonês não quebram e não atribuem atalho | **PASS** | `DialogAccessKeysTests.NonLatinTexts_HaveNoShortcut` |
| **AC-5** | Ausência de Duplicatas | 9 diálogos realistas em 5 idiomas sem letras repetidas | **PASS** | `DialogAccessKeysTests.RealisticDialogs_NeverRepeatALetter_FR003` |
| **AC-6** | Início de Palavra | Preferência por início de palavra (`Open folder` $\rightarrow$ `F`) | **PASS** | `DialogAccessKeysTests.WordStart_IsPreferredToInnerLetter` |
| **AC-7** | Textos Nulos/Vazios | Tratamento tolerante a nulos sem exceções | **PASS** | `DialogAccessKeysTests.EmptyAndNullTexts_AreSkipped` |
| **CONTROLE**| Detecção de Conflito| Teste negativo detecta duplicatas forçadas com sucesso | **PASS** | `DialogAccessKeysTests.NegativeControl_DetectsDuplicate` |

---

### 3. Testes Automatizados

Suíte MSTest em `tests/KyraFiles.App.Search.Index.Tests`:
- **Total de Testes:** 8 testes.
- **Aprovados:** 8 testes (100% PASS).
- **Falhas:** 0 falhas.
- **Duração:** 3,1s.

---

### 4. Pontos de Atenção e Advertências Técnicas

1. **Sublinhado e Tema do Sistema:**
   - O sublinhado da letra de acesso segue o padrão nativo do Windows XAML (`KeyboardAccelerator` / `AccessKey`). Dependendo da configuração de acessibilidade do Windows do usuário ("Sublinhar chaves de acesso quando disponíveis"), as letras podem ficar sempre visíveis ou surgir ao pressionar `Alt`.
