# Build 2026-10-28 — KyraFilesSetup v4.3.8 (Publicado)

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.3.8.exe` (52.688.161 bytes, 50,2 MB). Inno Setup 6.
- **SHA256:** `6B38579A13A8D1F547DA37127556A0E6EA7AC40C946D31E0947231DBD696FC2D` (`Get-FileHash`).
- **Release GitHub:** [v4.3.8](https://github.com/alxchs/KyraFiles-private/releases/tag/v4.3.8) em `alxchs/KyraFiles-private`.
- **Branch:** `main` / `colab` / `custom/main`.
- **Conteúdo Integrado:** F056 (copiar caminho como), F057 (atalhos sublinhados), F061 (Win+E sem flood), F062 (diagnóstico de memória no Sobre), correção de filtros por tag e preservação de configurações no setup.
- **Build:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = `0 Error(s)`, `25 Warning(s)` (baseline mantida).
- **Pacote:** `AppxManifest.xml` com `Version="4.3.8.0"`; 1.333 arquivos no manifesto `installer\package_manifest.json` (212,08 MB).

## O que é liberado

| Item | Estado | Detalhes |
|---|---|---|
| F062 Diagnóstico de memória do ecossistema no Sobre | CONFIRMED | Mede Working Set e Private Bytes de Files, Server, HotkeyService, Watchdog e IndexService; 6 testes unitários passando |
| Correção dos Filtros por Tag na barra lateral | CONFIRMED | Bypass no Everything/Index e consulta nativa ao banco SQLite de tags com rótulo "Tag: Nome" |
| Preservação de Configurações no Setup | CONFIRMED | `Remove-AppxPackage` com `-PreserveApplicationData` e preservação sem desinstalar se já estiver em `{app}` |
| F061 Win+E sempre pronto | CONFIRMED | Watchdog com espera crescente (200ms a 30s), saída com código 3 em duplicidade e monitor de saúde sem sobras de processos |
| F056 Copiar caminho como (7 formatos) | CONFIRMED | Menu de contexto e opções na página Geral |
| F057 Atalhos sublinhados nos botões de diálogo | CONFIRMED | Acesso rápido por teclado em botões de confirmação |
| Gate F021 (`verify_no_index_service.ps1`) | CONFIRMED | IndexService inativo e CPU em repouso < 5% (medido 3.33%) |

## Texto para o usuário

- **Novo:** Em Configurações → Sobre, visualize o consumo de memória em tempo real de todos os módulos integrados (Files, KyraFiles.App.Server, KyraFiles.HotkeyService, KyraFiles.Watchdog e KyraFiles.App.IndexService), com detalhamento por processo e cópia rápida de relatório.
- **Novo:** Clique direito em um arquivo → "Copiar caminho como" para copiar o caminho em vários formatos (Windows, WSL, JSON, URI e outros).
- **Corrigido:** As tags na barra lateral voltam a filtrar os arquivos etiquetados perfeitamente, sem delegar para busca externa nem exibir nomes estranhos.
- **Corrigido:** Suas configurações, abas e preferências são preservadas entre atualizações do instalador.
- **Corrigido:** O atalho Win+E deixa de criar processos em loop em segundo plano e reinicia com segurança e velocidade.
- **Melhorado:** Nas caixas de diálogo, botões trazem letras sublinhadas para confirmação rápida por teclado.
