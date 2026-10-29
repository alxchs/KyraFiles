# Build 2026-10-27 — FilesCustomSetup v4.3.8 (para teste)

- **Instalador:** `artifacts\Setup\FilesCustomSetup-4.3.8.exe` (52.649.986 bytes, 50,2 MB; a 4.3.7 tinha 52.694.203 bytes, -44.217 bytes). Inno Setup 6.
- **SHA256:** `C44718F1A9C6E208C2597CD5D80C60B46778EF0C116CDEB2E8BF7864E52A9817` (`Get-FileHash`).
- **Branch:** `feature/release-4.3.8`, criada sobre a `main` em `caa4c57ee`. Contém F056, F057 e a F061 (conserto do Win+E, refeito em 28/10/2026 sobre a branch `feature/f061-win-e-always-ready`). **Não contém a F060** (M1, M2, M5 ficam nas branches próprias).
- **Build:** `mkfile release src\Files.App\Files.App.csproj` = `0 Error(s)`, `25 Warning(s)` (CS0618 em código XAML gerado, a mesma linha de base de antes). Meta de 0 avisos: NOT MET, herdado, não novo.
- **Pacote:** `AppxManifest.xml` com `Version="4.3.8.0"`; 1.338 arquivos, 213,7 MB no layout; manifesto `installer\package_manifest.json` com 1.333 arquivos, 212,06 MB.

## O que é liberado

| Item | Estado |
|---|---|
| F056 Copiar caminho como (7 formatos, opções na página Geral) | CONFIRMED na tela, rodada 2 do build de teste; na rodada 1 o submenu falhou uma vez (intermitência não explicada) |
| F057 Atalhos sublinhados nos botões das caixas de diálogo | OBSERVED (sublinhado por captura); AC por teclado NOT TESTED |
| Instalador 4.3.8 | Compilado (CONFIRMED); **instalado e aberto: NOT TESTED** |
| Gate F021 (`verify_no_index_service.ps1`) | **NOT TESTED**: exige o app instalado e fecha o Files em execução |
| Release 4.3.8 sobre o build de teste | O build de teste foi registrado em Debug; este instalador é Release e ainda não foi aberto |

| F061 Win+E sempre pronto: Watchdog com espera crescente, serviço com código de saída próprio, `verify_win_e_health.ps1` | AC-1 a AC-5 CONFIRMED em cópia isolada (AGY, 28/10/2026; retomada com serviço externo em 36 ms); AC-6 OBSERVED (sem Watchdog vivo); AC-7 e AC-8 NOT TESTED (exigem este exe instalado) |

## Texto para o usuário (sem jargão)

- **Novo:** clique direito em um arquivo → "Copiar caminho como" para copiar o caminho em vários formatos (Windows, WSL, JSON e outros). Escolha quais formatos aparecem em Configurações → Geral.
- **Corrigido:** o atalho Win+E deixa de sobrecarregar o computador em segundo plano e volta a abrir o Files com rapidez, mesmo depois de uma falha.
- **Melhorado:** nas caixas de confirmação, cada botão mostra uma letra sublinhada; digite a letra para escolher o botão.

## Como testar

1. Restaurar o app instalado (pendente do Alexandre, como administrador) e só então rodar o instalador.
2. Instalar e abrir; conferir `Get-AppxPackage FilesDev` apontando para `%LOCALAPPDATA%\Programs\FilesCustom`.
3. Rodar `pwsh tools\perf\verify_no_index_service.ps1` e `pwsh tools\perf\verify_win_e_health.ps1` (esperado `[OK]` com 1 serviço e 1 Watchdog).
4. Clique direito → Copiar caminho como; excluir um arquivo de teste e usar as letras sublinhadas.

## O que NÃO está neste exe

F060 (Dividir/Juntar, editor hexadecimal, busca em linguagem natural), F058, e as demais specs do backlog.

## Pendências

- Tag e push desta branch/versão: aguardam autorização.
- Repetir F056 para medir a intermitência do AC-1; F057 AC-2/3/8 por teclado.
