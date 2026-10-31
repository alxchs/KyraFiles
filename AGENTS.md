# Files Development Guidelines

This project is a C#/.NET WinUI 3 desktop app; an alternative to File Explorer.

- Protect context usage. Any command with unknown or potentially large output must be capped. Prefer targeted commands such as `rg`, `Get-Content -TotalCount`, `Select-Object -First`, or focused `git diff -- <path>`; for example, `COMMAND 2>&1 | Select-Object -First 200`. If a line cap is still too noisy, narrow the query instead of dumping full output.
- Always follow `.editorconfig`
- Keep changed text files in CRLF line endings
- Keep comments concise and useful. Do not add comments that restate obvious code.
- Never read entire generated files in `bin` or `obj` unless the generated source is directly needed.
- Prefer targeted search over full file reads.
- Touch only what you must. Clean up only files you created or changed for the task.
- Treat file operations, shell integration, drag/drop, preview handlers, archive actions, settings persistence, and localization as high-risk areas.
- For Win32, COM, Shell, clipboard, hotkey, and file operation interop, prefer `src/Files.App.CsWin32`, `NativeMethods.txt`, and existing wrappers/helpers.
- Avoid ad hoc P/Invoke declarations when CsWin32 or existing interop code can cover the API.
- Do not edit generated CsWin32 output directly. Update source declarations, wrappers, or generator inputs instead.
- For UI work, use existing XAML resources, controls, converters, commands, and localization patterns. Avoid one-off styles or hard-coded user-visible strings.
- Start by identifying the smallest relevant project, feature area, and files for the task.
- Read nearby code before adding new abstractions. Prefer existing WinUI, MVVM, service, command, and storage patterns.
- Keep implementation scoped to the requested behavior. Avoid opportunistic refactors, formatting churn, dependency updates, and generated file edits.
- Treat tool output as evidence. When behavior changes, run the focused build that can prove it and report anything left unverified.

## Code Review Rules

### Native interop

- Reject new uses of `DllImport`. Prefer existing interop wrappers or CsWin32, and use `LibraryImport` only when CsWin32 cannot generate or safely express the required API.
- Reject new uses of `ComImport`. Prefer CsWin32-generated COM interfaces or `GeneratedComInterface`, and use a focused AOT-safe vtable wrapper only when source-generated COM cannot represent the interface.
- Add app runtime CsWin32 APIs to `src/Files.App.CsWin32/NativeMethods.txt`, and keep test-only APIs in the relevant test project's `NativeMethods.txt`; do not edit generated CsWin32 output.
- When introducing a required native module, add the matching `DirectPInvoke` entry to `src/Files.App/Files.App.csproj`. Do not direct-bind optional modules or entry points that are not guaranteed to exist on every supported Windows version.

### Reflection and Native AOT

- Flag new runtime reflection used for member discovery, invocation, dynamic activation, or runtime code generation unless trimming and AOT analysis can statically prove it safe. Simple type inspection such as `obj.GetType().Name` is out of scope.
- Prefer source generation, static dispatch, or explicitly registered types. When reflection is necessary, use `DynamicallyAccessedMembers`, `DynamicDependency`, or another narrowly justified preservation mechanism as required and keep trimming and AOT diagnostics clean.
- `RequiresDynamicCode` and `RequiresUnreferencedCode` declare incompatibility; they do not make reflection AOT-safe and are not acceptable exemptions.
- Keep suppressions of trimming or AOT diagnostics local, document why the reflected members remain available, and verify the relevant AOT build.

## Codebase Structure

```text
/src
├── Files.App                    Main WinUI app
├── Files.App.Controls           Shared app controls
├── Files.App.Storage            App storage abstractions and implementations
├── Files.App.CsWin32            Generated/native Win32 interop project
├── Files.App.BackgroundTasks    Background task project
├── Files.App.Server             App service/server project
├── Files.Core.SourceGenerator   Roslyn source generators and analyzers
├── Files.Core.Storage           Core storage abstractions
└── Files.Shared                 Shared attributes, extensions, and common code
```

```text
/tests
├── Files.App.UITests
├── Files.App.UnitTests
└── Files.InteractionTests
```

## Build

Prefer explicit platform/configuration builds.
Unless the task is specifically about resolving or inspecting warnings, add `-v:quiet -clp:ErrorsOnly` to `msbuild` commands so the log proves success or shows only actionable errors.

```powershell
msbuild -restore Files.slnx -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

If `msbuild` isn't available in the current shell, run it from Visual Studio Developer PowerShell. Match `-arch`, `-host_arch`, and `-p:Platform` to the platform you're verifying; use `x64` for x64 work and `arm64` for ARM64 work.

```powershell
pwsh.exe -NoProfile -Command "& {
  Import-Module 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll'
  Enter-VsDevShell 1ba2cc4e -SkipAutomaticLocation -DevCmdArguments '-arch=x64 -host_arch=x64'
  msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
}"
```

For focused C# work, build the affected project first.
Do not run build commands in parallel.

```powershell
msbuild -restore src/Files.Shared/Files.Shared.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.Core.SourceGenerator/Files.Core.SourceGenerator.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

## Test

We currently don't have a suitable set of tests for AI agents. Just make sure that the builds succeed.

## Commit & Push

When asked to commit, run these commands beforehand:

```powershell
git status --short
git diff --check
```

Do not revert unrelated user changes. Stage only files that belong to the requested change.

Use concise commit messages that describe the behavior change, for example:

```text
Add source-generated settings storage
```

## Open a PR

When asked to open a PR, use a short PR title that names the behavior, not the implementation mechanics only, and prepend the PR type:

- "Fix": use this prefix when the linked issue is a bug
- "Feature": use this prefix when the linked issue is a feature request
- "Code Quality": anything else

The repository maintainers draft release notes based on these PR types: only fixes and feature requests are listed.

Good examples:

```text
Fix: Fixed an issue where thumbnails wouldn't refresh when a file was updated
Feature: Add support for previewing AVI files in the Preview Pane
Code Quality: Add source-generated settings serialization
```

For the PR body, follow `./.github/PULL_REQUEST_TEMPLATE.md`.


## Files Custom (regras do fork)

Este checkout é o **Files Custom**, derivado do Files Community. Além das diretrizes acima:

- Leia `C:\Users\alxch\.dev-method\METODO.md` (metodologia geral do Alexandre, comum a todos os projetos), `MASTER_SPEC.md` (seções da tarefa), `AGENT_CONTEXT.md` (estado atual), `docs/agents/PLAYBOOK.md` (fila e pré-aprovações para seguir sem o Claude) e `docs/agents/CONTINUITY.md` (específico deste projeto) antes de agir.
- **REGRA DE OURO DA MAIN (15/10/2026):** É terminantemente PROIBIDO alterar ou comitar direto na `main`. Toda e qualquer alteração (conduzida pela AGY ou pelo Claude) DEVE ser feita em branch própria (`feature/<nome>`). A integração na `main` é feita OBRIGATORIAMENTE via squash/rebase, gerando um commit único atômico por feature para manter a `main` compacta, linear e organizada.
- **CICLO MANDATÓRIO DE SPEC, TESTE E COLAB (29/10/2026 - REGRA DURA):**
  1. Cada spec DEVE ter sua própria branch dedicada (`feature/<nome-ou-spec>`), saindo exclusivamente da `main`/`colab`.
  2. Em hipótese alguma pode iniciar uma nova spec sem a `colab` devidamente atualizada e sincronizada com a última entrega.
  3. Cada spec exige um roteiro de testes formal executado e validado.
  4. Em hipótese alguma pode devolver na `colab` com performance pior que a `colab` (sem lentidão, sem aumento injustificado de memória ou CPU).
  5. Apenas após a devolução completa na `colab` (squash/rebase + ff-only) é permitido iniciar outra spec.
  6. Ao final de cada spec, é obrigatório produzir um relatório de conclusão detalhado com os pontos importantes, evidências de testes e pontos de atenção/advertências técnicas.
  7. Esta regra é absoluta e soberana para AGY e Claude sempre.
- Nenhuma feature (F001+) antes do gate G0: baseline oficial compilado, aberto e com smoke test registrado em `STATUS.md`.
- **SDD (METODO §6):** feature nova nasce de `docs/specs/<ID>-<nome>/` (`spec.md`, `plan.md`, `tasks.md`), nessa ordem e com cada etapa aprovada; sem `tasks.md` aprovado não comece feature. Briefs em `docs/agents/tasks/` valem só para exploração, medição e features antigas já aprovadas. Melhoria fora do escopo vira `DISCOVERY`, não código.
- Declare `NOT TESTED`, `OBSERVED`, `INFERRED` ou `CONFIRMED`; nunca invente resultado.
- Commit local a cada passo verde; ao parar, escreva handoff em `docs/agents/handoffs/`. `git push` só com confirmação do Alexandre.
- Todo exe gerado para teste leva uma nota de liberação em `docs/releases/` (e página HTML quando possível): ver "Nota de liberação" em `docs/agents/PLAYBOOK.md`.
- **GATE MANDATÓRIO DE CONTENÇÃO DE RECURSOS (16/10/2026 - F021):** Nenhuma tarefa pode ser dada como concluída pela AGY ou pelo Claude sem a execução e validação formal de `pwsh tools\perf\verify_no_index_service.ps1`. O processo `Files.App.IndexService.exe` deve estar rigorosamente inativo e a CPU em repouso em 0% (ou < 5%).
- **HIGIENE ABSOLUTA DE BINÁRIOS (16/10/2026):** NUNCA deixar cópias de Debug ou em qualquer pasta auxiliar depois de desenvolver e testar. Somente a versão Release no local instalado (`%LOCALAPPDATA%\Programs\FilesCustom`) deve existir no ambiente.
- **REGRA DE DATA E HORA DE COMPILAÇÃO NA TELA SOBRE (31/10/2026 - REGRA DURA):** A tela Sobre (Settings > About) DEVE OBRIGATORIAMENTE exibir a data e hora exata da compilação (Build Date/Time) junto com o número da versão do aplicativo (ex: "Versão 4.3.9.0 • 2026-10-31 08:15:00"). Isso permite auditoria e comprovação imediata do binário em execução por AGY, Claude e pelo Alexandre. A data e hora devem ser geradas automaticamente pelo compilador/MSBuild no momento do build e embutidas no executável.
