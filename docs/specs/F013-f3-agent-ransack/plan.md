# F013 — F3 abre o Agent Ransack — PLAN

Status: **APROVADO** (Claude, 09/10/2026).

## 1. Arquitetura atual (lida no código em 09/10/2026)

- CONFIRMED: `Actions/Global/SearchAction.cs` tem `HotKey` Ctrl+F e `SecondHotKey` F3; executa `ToolbarViewModel.SwitchToSearchMode()`.
- CONFIRMED: ações com `[GeneratedRichCommand]` são registradas pelo gerador `CommandManagerGenerator` (o nome do comando é o da classe sem `Action`); o usuário troca atalhos em Configurações > Ações.
- CONFIRMED: `Services/Search/AgentRansackSearchProvider.cs` (F011) procura o executável em `Program Files` e `Program Files (x86)`.
- CONFIRMED (teste em 09/10/2026): `AgentRansack.exe -d "<pasta>"` abre o Agent Ransack com a pasta no título "Search: <pasta>\", com espaço e acento.

## 2. Abordagem

- Ação nova `Actions/Open/SearchWithAgentRansackAction.cs`, atalho F3, categoria Open. `ExecuteAsync`: se o Agent Ransack existir e `context.ShellPage.ShellViewModel.WorkingDirectory` for pasta de disco ou rede (`Path.IsPathRooted` e `Directory.Exists`), inicia `AgentRansack.exe -d "<pasta>"` (`ProcessStartInfo.ArgumentList`); senão, executa o comando de busca do Files.
- `SearchAction` perde o `SecondHotKey` F3 (fica só Ctrl+F).
- O caminho do executável passa a ser público e estático no `AgentRansackSearchProvider` (`GetExecutablePath`), para não duplicar a lista.
- Textos novos em `Strings/en-US` e `Strings/pt-BR` (`SearchWithAgentRansack`, `SearchWithAgentRansackDescription`).

Descartado: registrar o Files como "Find" do Windows (não é o caso de uso); trocar o F3 só quando o motor escolhido for Agent Ransack (o Alexandre quer o F3 assim independentemente da F011).

## 3. Arquivos

A mexer: `SearchWithAgentRansackAction.cs` (novo), `SearchAction.cs`, `AgentRansackSearchProvider.cs` (visibilidade de `GetExecutablePath`), `Strings/en-US/Resources.resw`, `Strings/pt-BR/Resources.resw`.
A não mexer: o restante da F011 (provedor, fábrica, Configurações > Pastas).

## 4. Teste por AC

- AC-1: script `tools/perf/f013_f3_agent_ransack.ps1`: Files numa pasta com espaço e acento, F3 por `SendInput`, confere o título da janela do Agent Ransack.
- AC-2: INFERRED pelo código (renomear o executável do Agent Ransack exigiria administrador).
- AC-3: F3 em "Início": caixa de busca do Files abre, Agent Ransack não.
- AC-4: Ctrl+F abre a busca do Files.
- AC-5: captura da paleta de comandos.
