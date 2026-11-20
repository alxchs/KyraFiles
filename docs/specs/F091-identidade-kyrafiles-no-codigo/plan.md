# F091 — Identidade KyraFiles de ponta a ponta no código — PLAN

Status: **RASCUNHO** (Claude, 12/11/2026). Depende da spec aprovada. **Não aplicar** até spec, plan e tasks aprovados e a F092 integrada na `main`.

## 1. Arquitetura atual (lida na `main` em 12/11/2026)

| Fato | Estado | Comando |
|---|---|---|
| 1.271 arquivos `.cs` com `namespace Files.*`; só 4 com `namespace KyraFiles.*` | CONFIRMED | `git grep -l '^namespace Files\.' main -- '*.cs' \| wc -l` |
| Por projeto: App 985, Controls 101, Shared 45, App.Storage 35, Search.Index.Tests 35, App.Search.Index 23, Core.SourceGenerator 13, UITests 12, Core.Storage 12, InteractionTests 9, IndexService 1 | CONFIRMED | mesmo grep agrupado por pasta |
| `App.Server` e `App.BackgroundTasks` já usam `KyraFiles.*` (namespace e `RootNamespace`) | CONFIRMED | `git grep '^namespace' -- src/KyraFiles.App.Server src/KyraFiles.App.BackgroundTasks` |
| 98 XAML com `x:Class="Files.`; 307 ocorrências de `using:Files.` em 144 XAML | CONFIRMED | `git grep -l 'x:Class="Files\.'`, `git grep -h -o 'using:Files\.[A-Za-z.]*'` |
| Geradores de código procuram atributos por nome em texto: `"KyraFiles.Shared.Attributes.GeneratedRichCommandAttribute"` (`CommandManagerGenerator.cs:12`) e `"KyraFiles.Shared.Attributes.RegistrySerializableAttribute"` (`RegistrySerializationGenerator.cs:19`) | CONFIRMED | `git grep '"Files\.Shared'` |
| **Abas salvas guardam o nome completo do tipo da página**: `TabBarItemParameter.cs:37` grava `InitialPageType.FullName`; `:55-61` compara com `typeof(ShellPanesPage/ModernShellPage/ColumnShellPage).FullName` e, se não bater, cai em `ShellPanesPage` | CONFIRMED | `git show main:src/KyraFiles.App/Data/Parameters/TabBarItemParameter.cs` |
| `App.Server` registra classes WinRT pelo `FullName` (`Program.cs:31`), já em `KyraFiles.App.Server` | CONFIRMED | idem |
| Manifesto: `KyraFiles.App.BackgroundTasks.UpdateTask` (já novo) | CONFIRMED | `git grep EntryPoint -- Package.appxmanifest` |
| `BuildInfo.cs:46` procura `KyraFiles.App.dll` e `:55` `KyraFiles.exe`, mas o assembly é `KyraFiles` (`KyraFiles.App.csproj:9`) | CONFIRMED (leitura); efeito em execução INFERRED | `git grep '"Files\.App\.dll\|"Files\.exe'` |
| `tools/Create-SelfContained-Release.ps1:17` exige `KyraFiles.App.OpenDialog64.dll` etc., mas os `.vcxproj` geram `KyraFiles.App.OpenDialog$(PlatformArchitecture)` | CONFIRMED (leitura); falha da release INFERRED (não executado) | `git grep TargetName -- '*.vcxproj'` |
| `Core.SourceGenerator/Properties/launchSettings.json:11` perfil chamado `KyraFiles.App.Server` | CONFIRMED | — |
| Ocorrências de limpeza legítima (instalador `[InstallDelete]`, encerrar processos antigos em `Update-KyraFiles.ps1`, `Open-KyraFilesDev.ps1`, `Program.cs:143`, `ZipStorageFolder.cs:155`, `MainWindow.xaml.cs:83`) | CONFIRMED | `git grep -P '(?<![A-Za-z])Files\.App\.'` |
| 176 arquivos com "KyraFiles" (maioria histórico) | CONFIRMED | `git grep -l "KyraFiles" main \| wc -l` |

## 2. Abordagem
Renome **mecânico**, por projeto, das folhas para a raiz, cada passo compilando sozinho:

1. Verificador primeiro (`tools/check-legacy-names.ps1`) com lista de exceções explícita; ele define o "pronto" de cada passo.
2. Correções de nome que já são defeito hoje (release script, `BuildInfo`, `launchSettings`), em commit à parte, antes do renome em massa.
3. Compatibilidade de leitura das abas salvas antes do renome do App: aceitar `KyraFiles.App.Views.*` e `KyraFiles.App.Views.*` ao ler; gravar só o novo.
4. Renome por projeto: `Shared` (com as strings dos geradores no mesmo commit) → `Core.Storage` → `Core.SourceGenerator` → `App.Storage` → `App.Search.Index` → `App.Controls` → `App` → `IndexService` → testes. Em cada um: `namespace`, `using`, `global using`, `x:Class`, `using:` de XAML, `RootNamespace` se houver.
5. Documentação viva (`AGENTS.md`, `CLAUDE.md`, `AGENT_CONTEXT.md`, `STATUS.md`, `docs/agents/PLAYBOOK.md`, `INSTRUCTIONS_OPERARY.md`, `MASTER_SPEC.md` só nas partes vivas).

Ferramenta: substituição por expressão regular com fronteira (`(?<![A-Za-z])Files\.` → `KyraFiles.`) restrita a `.cs`, `.xaml`, `.csproj`, `.props`, `.targets`, `.json` de `src/` e `tests/`, revisada pelo diff. Lookbehind obrigatório: `KyraFiles.App.X` contém `KyraFiles.App.X`.

### Opções descartadas
- **Tudo num commit só:** diff de ~1.400 arquivos impossível de revisar e de bisseccionar.
- **Manter `Files.*` e só documentar:** não atende ao pedido do Alexandre.
- **Renomear nomes de classes/estrutura junto:** muda lógica; fora de escopo (spec §7).

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001 / AC-1 | todos os `.cs`/`.xaml` listados em §1; strings dos geradores |
| FR-002 / AC-2 | `TabBarItemParameter.cs` (leitura dos dois nomes) |
| FR-002 / AC-3 | conferir em T0 se alguma preferência grava nome de tipo (busca por `FullName`, `GetType().Name`, `nameof` em chaves de JSON) |
| FR-003 / AC-4 | `Package.appxmanifest`, `App.Server/Program.cs` — **não mexer** além de conferir |
| FR-004 / AC-5 | `tools/Create-SelfContained-Release.ps1`, `tools/Build-Native.ps1` (comentário), `BuildInfo.cs`, `launchSettings.json` |
| FR-005 | instalador, `Update-KyraFiles.ps1`, `Open-KyraFilesDev.ps1`, `Program.cs:143` — exceções |
| FR-006 / AC-7 | `tools/check-legacy-names.ps1` |
| FR-007 / AC-6 | docs vivas listadas no passo 5 |

## 4. Não mexer
- `Package.appxmanifest` (identidade do pacote: F087).
- `LICENSE-MIT`, `LICENSE-MPL`, cabeçalhos `Copyright (c) KyraFiles contributors` e `SPDX-License-Identifier`.
- Saída gerada do CsWin32, `bin/`, `obj/`.
- `docs/agents/handoffs/`, `docs/agents/evidence/`, `docs/releases/` já publicadas.
- Nomes de classes, métodos e pastas (só o prefixo do namespace).

## 5. Riscos
| Risco | Mitigação |
|---|---|
| Abas salvas perdem o modo de exibição (cai em `ShellPanesPage` em silêncio) | T2 antes do renome do App; teste com JSON antigo |
| Gerador de código deixa de achar o atributo e comandos somem sem erro de build | strings dos geradores no mesmo commit de `Shared`; teste que conta os comandos gerados antes/depois |
| XAML compila mas falha em execução (tipo não achado) | abrir o app e as telas principais após o passo do App (regra de UI do projeto) |
| Conflito com branch aberta | NFR-002: só com nenhuma branch de código aberta |
| Reflexão/AOT por nome | T0 lista `Type.GetType`, `DynamicDependency` com texto e afins |
| Falso positivo do grep (`KyraFiles.App.X`) | lookbehind no verificador |

## 6. Teste por AC
- **AC-1, AC-7:** `tools/check-legacy-names.ps1` sai 0; versão com uma ocorrência plantada sai 1.
- **AC-2:** JSON de abas da versão anterior (os três tipos de página) lido após o renome; conferir tipo e endereço.
- **AC-3:** perfil de configurações copiado da versão instalada, app aberto, comparação de preferências.
- **AC-4:** roteiro manual: abrir/salvar de outro app, Win+E, gerenciador padrão, segunda instância, tarefa de atualização, serviço de índice (gate F021).
- **AC-5:** `tools\Build-Native.ps1`, `mkfile r`, `tools\Create-SelfContained-Release.ps1` e instalador; listar nomes no pacote.
- **AC-6:** grep nas docs vivas.
- **AC-8:** mesmos scripts de `tools/perf/` antes e depois; `verify_no_index_service.ps1`.
