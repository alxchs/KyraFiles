# Build 2026-10-09 10:45 — Explorer redirecionado, F3 com Agent Ransack, colunas no idioma do app

- **Executável:** `src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\Files.exe`, registrado por `.\Open-FilesDev.ps1` (pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`).
- **Código:** branch `feature/f012-explorer-replacement` (commits desde `1a131a045`; o último publicado é o adendo WIP `chore(f012): WIP`).
- **Build:** `mkfile r Files.slnx` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).

## O que foi liberado

| Item | O que muda para o usuário | Estado |
|---|---|---|
| Win + E abre o Files | Com o Files aberto ou em segundo plano e definido como gerenciador padrão, o Win + E abre "Este Computador" no Files. A janela do Explorer não chega a aparecer (no máximo ~10 ms). | CONFIRMED por `SendInput` (`tools\perf\f012_explorer_redirect.ps1`) |
| `explorer.exe <pasta>` abre no Files | Idem; a pasta abre no Files ~3 s depois do comando (o Explorer demora isso para expor a pasta). | CONFIRMED; tempo medido |
| "Mostrar na pasta" (`explorer /select`) | A pasta abre no Files com o arquivo marcado. | CONFIRMED com `explorer /select`; navegadores NOT TESTED; o arquivo aparece focado, possivelmente não selecionado (OBSERVED) |
| Painel de Controle e outras telas do Windows | Continuam no Explorer. | CONFIRMED (Painel de Controle) |
| Files fechado ou opção desligada | O Explorer abre normalmente. | CONFIRMED |
| F3 abre o Agent Ransack | F3 numa pasta abre o Agent Ransack já nessa pasta; Ctrl+F continua abrindo a busca do Files; fora de pastas de disco, F3 abre a busca do Files. | CONFIRMED (Agent Ransack em ~2 s, pasta com espaço e acento) |
| Colunas do Explorer no idioma do app | O diálogo "Escolher detalhes" e os cabeçalhos das colunas extras saem em inglês ou português conforme o idioma do Files (antes, sempre em português). | CONFIRMED em inglês (captura `docs/agents/evidence/g1c2/01_colunas_en.png`); português NOT TESTED pela tela |
| Diálogo "Abrir" do Files | Continua funcionando com o redirecionamento ligado. | CONFIRMED (regressão) |

## Para o Win + E funcionar depois de reiniciar o PC

O Files precisa estar rodando. Ligue *Configurações > Avançado > Abrir o Files ao iniciar o Windows* (hoje está **desligado** nesta máquina). Com o Files fechado, o Win + E abre o Explorer.

## Como testar

1. Feche o Files Dev e rode `.\Open-FilesDev.ps1`.
2. Win + E: deve abrir o Files em "Este Computador".
3. `Win + R`, digite `explorer C:\Windows`: deve abrir no Files.
4. Numa pasta do Files, F3: abre o Agent Ransack nessa pasta. Ctrl+F: busca do Files.
5. Botão direito no cabeçalho "Nome" > "More...": nomes das colunas no idioma do app.

## Ainda não está neste exe

- Abertura mais rápida do `explorer.exe <pasta>` (D-PERF-03).
- Correções da revisão do G1, exceto a parte de idioma do C2 (feita aqui).
- Teste de desinstalação (AC-9) e de pastas especiais (AC-10).
