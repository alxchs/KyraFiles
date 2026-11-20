# Ordem de serviço AGY — F015 T1 e T2 (11/10/2026, Claude)

Você é a executora. O Claude é o gestor e revisa. Repositório `C:\desenv\utils\KyraFiles`, branch `feature/f015-indice-de-busca` (já existe e está ativa). Rode `ia claim agy "F015 T1 T2"` no início e `ia release` no fim.

Leia antes, nesta ordem:
- `AGENTS.md`
- `AGENT_CONTEXT.md`
- `docs/specs/F015-indice-de-busca/spec.md`
- `docs/specs/F015-indice-de-busca/plan.md`
- `docs/specs/F015-indice-de-busca/tasks.md`

Execute **só T1 e T2** da `tasks.md`. Não comece T3.

## Restrição importante: administrador

O Alexandre não sabe operar UAC por conta própria. **Você não executa nada elevado.** Para todo passo que exige administrador (instalar e remover serviço, ler a MFT, criar serviço com `sc`):
1. Deixe um script pronto, que roda sozinho, sem perguntas, e grava a saída num log em `%TEMP%\claude\desenv\utils\KyraFiles\f015\`.
2. Liste no handoff o comando exato para o Claude disparar com `Start-Process pwsh -Verb RunAs -ArgumentList ...`.

O Claude dispara, o Alexandre só clica "Sim", e o Claude lê o log.

## T1 — como instalar o serviço

- Protótipo descartável em `tools/spikes/f015-service/`, fora de `src/` e fora do `KyraFiles.slnx`:
  - serviço que roda como SYSTEM e responde "ping" com o nome da conta (`WindowsIdentity.GetCurrent().Name`) num named pipe `KyraFiles.IndexService.Spike`, com ACL para usuários autenticados;
  - cliente de console não elevado que faz o ping.
- Compare as duas formas:
  - (a) **Serviço empacotado no MSIX** (`desktop6:Service`, capacidades `packagedServices` e `localSystemServices`). Leia a documentação da Microsoft e diga se funciona com o pacote sideload que o projeto usa (`Open-KyraFiles.ps1`) e o que exige. Não altere o `Package.appxmanifest` real: se precisar testar, use um pacote mínimo de teste dentro da pasta do spike.
  - (b) **Instalador elevado**: copia o exe para `%ProgramFiles%\KyraFiles\IndexService\` e cria o serviço; o desinstalador remove os dois.
- Deixe prontos `Install-Spike.ps1` e `Uninstall-Spike.ps1` (elevados, para o Claude disparar) e `Test-Spike.ps1` (não elevado: `sc query` + ping).
- Escreva **D-020** em `DECISIONS.md` com a recomendação e o motivo. Marque como PENDING até o teste elevado rodar; o Claude atualiza depois.

## T2 — medir antes de construir

Scripts em `tools/perf/`:
- **`f015_basic_scan.ps1` (sem admin):** varredura com `GetFileInformationByHandleEx(FileIdBothDirectoryInfo)` via um pequeno exe ou C# compilado em `tools/spikes/f015-scan/`, sem descer em ponto de reanálise e sem abrir arquivo.
  - Volumes: C:, D:, R:, S:.
  - Por volume: tempo, itens, pastas e pico de RAM do processo.
  - Rode e anote.
- **`f015_mft_probe.ps1` (admin, só prepare):**
  - abre `\\.\C:`;
  - lê a MFT em blocos grandes (`FSCTL_GET_NTFS_VOLUME_DATA` + extents de `$MFT` via `FSCTL_GET_RETRIEVAL_POINTERS`);
  - conta registros em uso, com tempo e MB/s, saída em log.
- **`f015_gdrive_probe.ps1` (sem admin):**
  - lista `C:\Google\Drive\disk` inteiro sem abrir arquivos;
  - mede bytes recebidos pelo processo do Google Drive antes e depois (contadores de E/S do processo, `Get-Process` / `Win32_PerfFormattedData_PerfProc_Process`);
  - diga se baixou algo.
- Adicione ao `plan.md` a seção **"Medições T2"** com cada número e o comando que o produziu. A medição da MFT fica como "pendente de execução elevada".

## Regras

- Compile com `mkfile` (PowerShell) quando for projeto; para spikes, `dotnet build` dentro da pasta do spike vale.
- Um commit por tarefa (`docs(f015): ...` / `chore(f015): ...`). **Sem push.**
- Temporários só em `%TEMP%\claude\desenv\utils\KyraFiles\f015\`.
- Estados de evidência: `CONFIRMED` só com comando e saída.
- Ao terminar ou parar, escreva o handoff em `docs/agents/handoffs/2026-10-11_<HHMM>_AGY_F015-T1-T2.md` e atualize `AGENT_CONTEXT.md` (NEXT ACTION: comandos elevados para o Claude disparar).
