# F086 - Plano

## Estado atual (lido no codigo em 02/11/2026, CONFIRMED)
- `Program.cs`: o semaforo do construtor estatico e descartado no `finally`, entao nao segura a instancia. O redirecionamento em `Main` so ocorre com argumentos nao vazios (inicio simples sem argumentos nunca redireciona) e depende da opcao `OpenTabInExistingInstance`.
- `MainWindow.InitializeApplicationAsync`, caso `ILaunchActivatedEventArgs` com argumentos vazios e instancias existentes, cairia no ramo final que recria a `MainPage`.

## Abordagem
1. `Program.Main`: antes de registrar a chave da instancia, tomar um mutex nomeado `Local\KyraFiles-{ambiente}-MainInstance` (FR-1). Dono => processo principal, mutex guardado pela vida do processo. Nao dono (nao dialogo) => `TryRedirectToRunningInstance`: tenta por ate 8 s ler `INSTANCE_ACTIVE`, localizar a instancia viva e redirecionar a ativacao (FR-2, AC-3). Sem sucesso em 8 s o processo segue como principal (AC-5). Mutex abandonado conta como dono.
2. `MainWindow`: ramo novo para inicio simples redirecionado: apenas traz a janela para a frente (AC-1).
3. A opcao `OpenTabInExistingInstance` continua existindo na tela de configuracoes e nos ramos antigos de `Main`, mas o novo bloco roda por ultimo e cobre o que ela deixava passar (FR-3).

## Mapa
| Requisito | Codigo |
|---|---|
| FR-1, AC-3, AC-5 | Program.cs (mutex + TryRedirectToRunningInstance) |
| FR-2, AC-1, AC-2 | Program.cs + MainWindow.xaml.cs |
| AC-6 | `IsFileDialogLaunch()` excluido do bloco |

## Mexer
src/KyraFiles.App/Program.cs, src/KyraFiles.App/MainWindow.xaml.cs
## Nao mexer
Dialogos (OpenDialog/SaveDialog), Launcher, HotkeyService, Server, IndexService.

## Riscos
- Redirecionar quando o principal esta ocupado: protegido por tempo limite e fallback (AC-5).
- Acao "abrir em nova janela" deixa de criar processo; passa a abrir aba. Aceito pelo Alexandre.

## Teste por AC
Medicao externa: contar processos `KyraFiles` e memoria total antes/depois (tools/perf), so no horario de teste de tela (METODO 16); build de verificacao agora.
