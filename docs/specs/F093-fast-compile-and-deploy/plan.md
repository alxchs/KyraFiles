# F093 — Compilação e Deploy Otimizados — PLAN

Status: **APROVADO** em 14/11/2026.

## 1. Abordagem de Implementação
Criar o script canônico `tools/Quick-Ship.ps1` com os parâmetros:
- `-Fast` (switch, default se nenhum especificado):
  1. Encerra processos ativos (`KyraFiles`, `KyraFiles.App.Server`, etc.).
  2. Compila incrementalmente `src/KyraFiles.App/KyraFiles.App.csproj` em `Release` com `-p:Platform=x64 -v:minimal`.
  3. Gera o instalador via Inno Setup (`ISCC.exe`) passando diretiva de compressão rápida (`/DFAST_COMPRESSION`).
  4. Executa deploy dos binários em `%LOCALAPPDATA%\Programs\KyraFiles`.
  5. Roda `verify_no_index_service.ps1`.
- `-Full` (switch):
  1. Executa testes unitários isolados com `CreateNoWindow`.
  2. Compilação limpa.
  3. Inno Setup com compressão `lzma2/ultra64`.
  4. Deploy oficial e validação completa.

## 2. Ajustes no Instalador (`KyraFilesSetup.iss`)
Adicionar suporte condicional ao Inno Setup:
```iss
#ifdef FAST_COMPRESSION
Compression=lzma/fast
#else
Compression=lzma2/ultra64
#endif
```
Isso reduz o tempo de empacotamento de 1m33s para ~15 segundos no modo rápido!

## 3. Riscos e Mitigações
- **Travamento de binários em execução:** O script garante encerramento de `KyraFiles`, `KyraFiles.App.Server`, `KyraFiles.Watchdog`, `KyraFiles.HotkeyService`.
- **Foco do Windows:** Toda execução de comando/testes utiliza `-NoNewWindow` / `CreateNoWindow = true` para nunca roubar o foco do Alexandre.
