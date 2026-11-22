# Relatório de Conclusão e Aceite: SPEC-0003 (F004 — Barra de Menus Tradicional Opcional)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 3 pts  
**Evidências:** `docs/agents/evidence/f004/` (`f004_initial_disabled.png`, `f004_settings_appearance.png`, `f004_settings_page.png`, `f004_menu_bar_visible.png`, `f004_file_menu_opened.png`, `f004_edit_menu_opened.png`, `f004_view_menu_opened.png`)

---

## 1. Escopo e Implementação
Implementação da barra de menus clássica (`MenuBar`) opcional e suspensa:
- Controle isolado `ClassicMenuBar.xaml` com os 6 menus canônicos: File, Edit, View, Go, Tools, Help.
- Integração total com `ICommandManager`: comandos e atalhos herdados diretamente sem duplicação de lógica.
- Opção `ShowClassicMenuBar` em `AppearanceSettingsService` com interruptor em Configurações > Aparência.
- Desligada por padrão: custo zero de espaço e layout original intacto quando desativada.
- Navegação por teclado completa via tecla `Alt` e mnemonics (`Alt+F`, `Alt+E`, `Alt+V`, etc.), setas e `Esc`.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Desligada por padrão mantendo o layout original idêntico | `CONFIRMED` | `f004_initial_disabled.png` |
| **AC-2** | Opção para ligar/desligar em Configurações > Aparência | `CONFIRMED` | `f004_settings_appearance.png` |
| **AC-3** | Barra posicionada entre abas e toolbar com consistência de tema | `CONFIRMED` | `f004_menu_bar_visible.png` |
| **AC-4** | Menus abrem por clique e por atalhos Alt+F, Alt+E, etc. | `CONFIRMED` | `f004_file_menu_opened.png`, `f004_edit_menu_opened.png` |
| **AC-5** | Estados habilitado/desabilitado refletem o contexto atual | `CONFIRMED` | Gerenciado via `IRichCommand.CanExecuteChanged` |
