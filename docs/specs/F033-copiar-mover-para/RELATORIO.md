# Relatório de Conclusão e Aceite: SPEC-0029 (F033 — Copiar para / Mover para no menu de contexto)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 3 pts  
**Evidências:** `docs/agents/evidence/F033/` (`relatorio.md`, `f033_context_menu.png`)

---

## 1. Escopo e Implementação
Implementação das opções "Copiar para" e "Mover para" no menu de contexto com segurança de acionamento:
- Submenus dedicados com histórico das últimas 12 pastas de destino utilizadas.
- Opção "Escolher pasta..." com diálogo seletor de pastas do Windows.
- Diálogo de confirmação prévia com botão padrão **"Não"** (Enter/Esc cancelam a operação, prevenindo movimentações acidentais).
- Integração ao pipeline padrão com suporte a cancelamento e `Ctrl+Z` (desfazer).
- Filtragem automática de destinos apagados ou unidades desconectadas.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Menus "Copiar para" e "Mover para" visíveis no menu de contexto | `CONFIRMED` | `f033_context_menu.png` |
| **AC-2** | Confirmação com default "Não": Enter não transfere; "Sim" transfere e atualiza histórico | `CONFIRMED` | Diálogo com `DefaultButton = Close` |
| **AC-3** | Histórico limita até 12 pastas mais recentes sem duplicidade | `CONFIRMED` | Persistido nas configurações do usuário |
| **AC-4** | "Mover para" move os arquivos e suporta Ctrl+Z | `CONFIRMED` | Operação no pipeline `FilesystemHelpers` |
| **AC-5 / AC-6** | Pastas excluídas somem do menu e lista persiste entre sessões | `CONFIRMED` | Validação de existência dinâmica |
