# Handoff — Claude, F012 T9, F013 e G1 C2 (09/10/2026 10:45)

Branch `feature/f012-explorer-replacement` (checkout principal). Publicada até o adendo WIP; os commits seguintes estão só locais. Executor: Claude, a pedido do Alexandre ("pode fazer, preciso esgotar sua cota").

## Feito nesta rodada

- **Limpeza pedida pelo Alexandre:** 181 PNGs `<guid>.tmp` apagados do Downloads (autor desconhecido; não eram deste projeto). Temporários do Claude agora em `%TEMP%\claude\<caminho do projeto>` (regra em `METODO.md` §12 e memória).
- **OneCommander:** verbos `OpenInOneCommander` removidos do HKCU (pasta, fundo de pasta, unidade, fundo de unidade) e o verbo padrão das unidades. Backup para voltar: `C:\Users\alxch\Backups\registro\2026-10-09_antes-de-remover-OneCommander_HKCU.reg` (duplo clique restaura).
- **F012 T9 (D-017):** `ExplorerWindowRedirector` redireciona as janelas do Explorer para o Files enquanto ele roda. Medidas e limites em `docs/specs/F012-explorer-replacement/tasks.md` (seções 09:50 e 10:45).
- **F013:** F3 abre o Agent Ransack na pasta atual (`docs/specs/F013-f3-agent-ransack/`).
- **G1 C2 (parte de idioma):** catálogo de colunas pelos arquivos de tradução; STATUS e tarefas da F010 com a contagem real (39). O resto do C2 no brief da AGY já não se aplica a esses arquivos.

## Pendente

1. Alexandre: ligar "Abrir o Files ao iniciar o Windows" (hoje desligado) para o Win + E valer após reiniciar.
2. Push dos commits locais: só com autorização.
3. F012: AC-3 com navegador real, AC-9 (desinstalação), AC-10, T8 (fechamento).
4. DISCOVERY novas em `STATUS.md`: D-PERF-03, D-LEAK-01, D-C4.
5. Meus testes abriram várias abas na sessão do Files do Alexandre (This PC, Settings, pastas de teste em `%TEMP%\claude`); não fechei para não mexer nas abas dele.
