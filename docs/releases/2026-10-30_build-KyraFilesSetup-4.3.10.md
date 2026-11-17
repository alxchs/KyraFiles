# Build 2026-10-30 — KyraFilesSetup v4.3.10 (não publicado)

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.3.10.exe` (52.789.680 bytes, 50,34 MB). Inno Setup 6.
- **SHA256:** `8E42E322E5AF5724D14198BE9FEDC6BED0B95990EB4978F65D84D1A3AED2B37A` (`Get-FileHash`).
- **Tamanho comparado à 4.3.9:** +103.047 bytes (52.789.680 contra 52.686.633), abaixo do limite de 5 MB / 10%.
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` → `0 Warning(s)`, `0 Error(s)`, 3m59s. Pacote: 1.333 arquivos, 212,41 MB (`installer\package_manifest.json`).
- **Branch:** `colab` (publicada como `colab-2026-10-30`). Não publicado no GitHub.
- **Conteúdo:** correções da F065 (resultado de busca nasce visível; leitura da linha do Everything) e da F063 (sem liberação de memória por perda de foco; espera de operações de arquivo com prazo de 30 s).

## Estado

| Item | Estado | Base |
|---|---|---|
| Leitura da linha do Everything | CONFIRMED | 6 testes novos, suíte 159/160 (1 pulado) |
| Prova de tela F065 (AC-1..7) e F063 (AC-1..5) | NOT TESTED | prova às 09:00 de 31/10 |
| Desempenho no app | NOT TESTED | medição às 09:00 de 31/10 |
| Instalador instalado e aberto | NOT TESTED | não aberto (METODO §16) |
