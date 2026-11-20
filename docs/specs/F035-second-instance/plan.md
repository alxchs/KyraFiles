# F035 — Segunda instância e abertura direta — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026). Prioridade P0 (bug que derruba o processo).

## 1. Arquitetura atual (lida no código em 24/10/2026)

- **CONFIRMED** (`src/KyraFiles.App/Program.cs:57-79`): o construtor estático de `Program` cria o semáforo nomeado `Files-<ambiente>-Instance` com contagem inicial 0 e máximo 1. Se ele já existe (`isNew == false`) e a ativação não é de diálogo de arquivo, a instância atual lê `INSTANCE_ACTIVE`, acha a principal, chama `pool.Release()` (linha 69), redireciona a ativação (`RedirectActivationTo`, linha 72) e encerra (`Environment.Exit(0)`).
- **CONFIRMED** (`App.xaml.cs:225-243` e `398-427`): a instância principal "estaciona" criando o mesmo semáforo e esperando nele (`WaitOneAsync(pool)`). Cada `Release()` acorda a principal uma vez. Fora desse estado, ela não consome nada.
- **CONFIRMED** (log de eventos `.NET Runtime` 1026, 18/10/2026 17:57, 21:35, 21:47, 21:48 e 21:48): `SemaphoreFullException` em `Semaphore.ReleaseCore` ← `Program..cctor`. O processo morre **antes** de chamar `RedirectActivationTo`, então o pedido (a pasta) se perde.
- **INFERRED** (a causa): duas segundas instâncias chamam `Release()` antes de a principal consumir o primeiro sinal; a contagem já está no máximo (1) e a segunda `Release()` lança. Precisa ser reproduzida (T1).
- **CONFIRMED** (Event Log, 16 a 23/10): `REGDB_E_CLASSNOTREG` em `DeploymentManagerAutoInitializer.AutoInitialize` ← `.cctor()` **antes do `Main`** quando o `KyraFiles.exe` abre sem identidade de pacote. Já documentado em `STATUS.md:177` e no PLAYBOOK: a tentativa anterior (`WindowsAppSdkBootstrapperAutoInitialize=false`) não resolveu.
- **CONFIRMED** (WER, 17 a 19/10): 4 das falhas `REGDB_E_CLASSNOTREG` vieram do `KyraFiles.exe` **instalado** (`%LOCALAPPDATA%\Programs\KyraFiles\KyraFiles.exe`), ou seja, alguém o abriu direto (atalho, duplo clique ou PATH).

## 2. Abordagem

**Parte A (FR-001, FR-002; AC-1, AC-2, AC-3, AC-5): corrigir o semáforo.**
Em `Program.cs`, envolver `pool.Release()` em `try/catch (SemaphoreFullException)` com comentário `// KyraFiles:`. Se o semáforo já está no máximo, a principal **já** foi acordada por outro disparo: ignorar e seguir para `RedirectActivationTo`. Garantir `pool.Dispose()` em todos os caminhos. Proteger também `RedirectActivationTo` contra exceção (a instância principal morreu entre a leitura de `INSTANCE_ACTIVE` e o redirecionamento): em caso de falha, cair no fluxo normal (esta instância vira a principal) em vez de morrer. Não mudar o modelo de ativação do upstream.
- Opções descartadas: (a) trocar o semáforo por um evento manual: muda o protocolo entre processos e o `App.xaml.cs`; (b) aumentar o máximo do semáforo: faz a principal acordar várias vezes e abrir janelas duplicadas.

**Parte B (FR-004; AC-4): abertura direta sem identidade de pacote.**
Não se sabe ainda **quem** abre o `KyraFiles.exe` direto. Primeiro descobrir (T4): o que o instalador cria que aponta para o `KyraFiles.exe` (atalhos, entrada de Aplicativos, variável PATH, associação de arquivos, `kyrafiles-dev:`), e comparar com o que o Windows registrou nas 4 falhas instaladas. Resolver a causa no instalador (atalhos e aliases devem ativar o pacote, p. ex. `shell:AppsFolder\KyraFilesDev_...!App` ou `kyrafiles-dev:`), e só se isso não bastar avaliar um pequeno executável lançador. **Não** mexer na inicialização do WindowsAppSDK (já tentado e registrado como inútil).

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001, FR-002 | `src/KyraFiles.App/Program.cs` (cctor, linhas 57-79) |
| FR-003 | mesmo trecho: manter `IsFileDialogLaunch()` e a condição `!isNew && !IsFileDialogLaunch()` intactas |
| FR-004 | `installer/KyraFilesSetup.iss` e, se preciso, o lançador `KyraFiles.App.Launcher` |
| NFR-001 | medição em T5 |

**Arquivos a mexer:** `src/KyraFiles.App/Program.cs`; `installer/KyraFilesSetup.iss` (só T4/T5); `tools/perf/` (script de disparo); `docs/agents/evidence/F035/`.
**A NÃO mexer:** `App.xaml.cs`, `MainWindow.xaml.cs`, `SystemTrayIcon.cs` (consumidores do semáforo), a lógica de `ExplorerWindowRedirector` (F012) e `Directory.Build.props`.

## 4. Riscos
- Esconder uma exceção que tinha outra causa: por isso o `catch` é só de `SemaphoreFullException`, com log.
- Duas janelas principais por corrida: o teste AC-1/AC-2 conta janelas.
- Teste de ativação mexe no app em uso do Alexandre: só em instância própria, em outro perfil, nunca no app dele.

## 5. Estratégia de teste (por AC, com asserção; script sai ≠ 0 se falhar)

Script `tools/perf/test_f035_activation.ps1`: inicia o KyraFiles de teste, dispara N aberturas pelo protocolo `kyrafiles-dev:` e pelo `KyraFiles.exe` do pacote, e lê o resultado pela UI Automation (abas), pelo log de eventos 1026 (nenhuma exceção nova após o horário de início) e pela contagem de processos.

| AC | Como se prova |
|---|---|
| AC-1 | 5 disparos <1 s: 5 abas esperadas, 0 eventos 1026, 1 processo principal; 20 rodadas (critério de sucesso) |
| AC-2 | app fechado + 2 disparos: 1 janela com 2 pastas |
| AC-3 | disparo com `-outputpath`: diálogo continua diálogo; a pasta abre na principal |
| AC-4 | executar o `KyraFiles.exe` instalado direto: abre normalmente ou mostra mensagem; 0 eventos 1026 `REGDB_E_CLASSNOTREG` |
| AC-5 | principal estacionada (janela fechada na bandeja): disparo traz a janela à frente |
| **Controle negativo** | rodar T1 **antes** da correção: tem de reproduzir o `SemaphoreFullException` (prova que o teste consegue reprovar) |
