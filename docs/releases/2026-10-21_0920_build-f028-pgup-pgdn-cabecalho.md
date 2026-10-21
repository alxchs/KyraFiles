# Build 2026-10-21 09:20 — PgUp/PgDn sem contorno "no nada" (F028, 3ª revisão)

- **Pacote dev registrado:** `FilesDev_4.3.1.0_x64__ykqwq8d6ps0ag`
- **Layout AppX (o mesmo de antes):** `src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX`
- **Como abrir:** menu Iniciar → **Files Dev** (ou `.\Open-FilesDev.ps1 -Configuration Release`)
- **Commit do código:** `06e5b9a22` (consolidado na `main` junto com F024 a F028)
- **Build:** `dotnet build src/Files.App/Files.App.csproj -c Release -p:Platform=x64` → `0 Warning(s)`, `0 Error(s)` (CONFIRMED)

## O que entrou

| Item | Estado | Descrição |
|---|---|---|
| Contorno de foco nunca sobre o cabeçalho | OBSERVED (quadros, teclas sintéticas) | Antes, o PgDn deixava o item escolhido atrás do cabeçalho das colunas e o contorno aparecia sobre o cabeçalho. Agora o item fica sempre inteiro, abaixo dele. |
| PgDn / PgUp como no Explorer | OBSERVED | 1º toque vai à última (PgDn) ou primeira (PgUp) linha visível; os seguintes viram uma página inteira (45 linhas na janela de teste). |
| Desempenho do PgDn segurado | OBSERVED | 765 linhas em 5 s (mediana de 5 rodadas), contra 643 do build anterior. |
| Setas | sem mudança | 74 linhas em 5 s, contra 82 antes: dentro do ruído; o código das setas não mudou. |
| Shift+PgUp/PgDn, lista agrupada | NOT TESTED | |

## Como testar
1. Abra uma pasta grande no modo Detalhes.
2. Um toque em PgDn: a seleção vai para a última linha visível. Segure PgDn: a lista vira páginas e o contorno fica na linha de baixo, nunca em cima do cabeçalho.
3. Faça o mesmo com PgUp (o contorno fica na primeira linha, logo abaixo do cabeçalho).
4. Shift+PgDn/PgUp para estender a seleção.

## O que ainda não está
- Setas continuam mais lentas que no Explorer (custo nativo do WinUI por linha). Próximo alvo, a especificar.
