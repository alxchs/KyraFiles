# Build 2026-10-29 — KyraFilesSetup v4.3.9 (Parcial P0)

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.9.exe` (52.686.633 bytes, 50,24 MB). Inno Setup 6.
- **SHA256:** `E31293B803FBDCFCC8120995635443D59E526A06BD42F5735BCA3DA59514AFBF` (`Get-FileHash`; conferido de novo pelo Claude em 30/10/2026 e igual ao digest do GitHub).
- **Branch de Origem:** `feature/f063-memory-optimization-p0`.
- **Commit:** `ad7c7e0d2` (código); manifesto e notas em `bda243fd9` (tag `v4.3.9`).
- **Conteúdo Integrado:** Spec F063 (liberação da RAM ao minimizar ou ocultar a janela).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64`. A executora anotou `0 Error(s)`, `0 Warning(s)`, sem anexar a saída: **NOT TESTED** (a linha de base combinada era de 25 avisos).
- **Pacote:** `AppxManifest.xml` com `Version="4.3.9.0"`; 1.333 arquivos no manifesto `installer\package_manifest.json` (212,08 MB).
- **Tamanho comparado à 4.3.8:** 52.686.633 bytes (-1.528 bytes).
- **Publicação:** release `v4.3.9` no GitHub em 30/10/2026, por decisão do Alexandre ("1": publicar mesmo antes das correções Q1, Q2 e A3), com o texto ao usuário abaixo.

## O que é liberado

Revisado pelo Claude em 30/10/2026 (questionário de 29/10, Q9: "Sim, tudo"). Os estados originais eram todos CONFIRMED, sem comando e saída.

| Item | Estado | Base |
|---|---|---|
| RAM liberada ao minimizar | OBSERVED (pela AGY, 3 rodadas) | conjunto de trabalho de ~246 MB para 3,6–4,5 MB; a memória privada comprometida não cai por desenho; não repetido pelo Claude |
| Restauração da janela | OBSERVED (pela AGY) | 38–46 ms até a janela ficar visível (não até o primeiro quadro), contra 31 ms de base: +7 a +15 ms, acima da tolerância de ±10 ms decidida depois (Q5) |
| Liberação após 60 s sem foco | NOT TESTED | vetada (Q1) e removida na correção da F063 (`feature/f063-correcoes`); continua dentro da 4.3.9 publicada |
| Espera de operações de arquivo antes de liberar | NOT TESTED | espera sem prazo nesta versão; prazo e cancelamento na correção da F063 (Q2) |
| Gate F021 (IndexService inativo, CPU em repouso) | OBSERVED (pela AGY) | 3,33% de CPU anotado, sem a saída do script |
| Preservação de configurações ao reinstalar | OBSERVED (Claude, 29/10) | reinstalação na mesma pasta com o hash de `user_settings.json` e `filetags.json` inalterado; reinstalação a partir de outra pasta NOT TESTED |

## Texto para o usuário (igual ao publicado no GitHub)

**Novo:**
- **Menos memória RAM ocupada com o KyraFiles minimizado:** quando você minimiza a janela ou deixa o KyraFiles em segundo plano, a memória que ele ocupa na RAM diminui bastante e volta ao reabrir a janela.

**Melhorado:**
- Cópias e movimentações de arquivos ficam protegidas enquanto a memória é liberada.

Observação interna: "Melhorado" acima repete uma afirmação da executora que não tem prova (NOT TESTED); a 4.3.10 deve trocá-la por texto provado.
