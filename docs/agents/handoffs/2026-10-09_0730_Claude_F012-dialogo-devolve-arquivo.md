# Handoff — Claude, F012: diálogo devolve o arquivo (09/10/2026 07:30)

Branch `feature/f012-explorer-replacement` (checkout principal). Sem push. Executor: Claude, a pedido do Alexandre (AGY sem cota desde 08/10).

## Feito

- Bloqueio do Firewall resolvido (o `PickerHost` não estava mais aberto); automação de UI voltou a funcionar.
- **T7:** o diálogo devolvia "Cancelar". Causa e correção na D-016 (a instância de diálogo restaurava e regravava a sessão do usuário e lia a seleção tarde demais). Script novo `tools/perf/f012_file_dialog_return.ps1` testa o fluxo completo por UI Automation.
- **T3:** gerenciador padrão desligado e religado pela tela, sem UAC, com o registro conferido.
- **T6:** abertura de pasta pelo shell vai para o KyraFiles; `explorer.exe <pasta>` direto vai para o Explorer (D-017, PENDING DECISION).
- Sessão do KyraFiles desta máquina limpa das abas de teste (cópia em `user_settings.json.f012-backup`, em `LocalState\settings`).
- Nota `docs/releases/2026-10-09_0730_build-F012-dialogo-devolve-arquivo.md`.

## Estado por AC

Tabela "Estado em 09/10/2026 07:30" em `docs/specs/F012-explorer-replacement/tasks.md`.

## Pendente

1. Alexandre: decidir a D-017 (`explorer.exe <pasta>`); testar `Win + E` com teclado real (AC-1).
2. AC-3 ("Mostrar na pasta" de navegador, `/select`), AC-7 (modo de segurança), AC-10 (pastas especiais), AC-9 (desinstalação real): NOT TESTED.
3. Ao fim da F012, decidir se as opções ficam ligadas nesta máquina (hoje: ligadas).
4. DISCOVERY novas em `STATUS.md`: D-A11Y-01 (nome acessível com tipo "Pasta" para arquivo) e D-PERF-02.
