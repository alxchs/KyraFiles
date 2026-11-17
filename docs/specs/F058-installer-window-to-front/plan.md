# F058 — PLAN

Status: rascunho do Claude (26/10/2026), aguardando a spec aprovada.

## 1. Arquitetura atual (lida em 26/10/2026)
- CONFIRMED: instalador Inno Setup `installer/KyraFilesSetup.iss`, `PrivilegesRequired=lowest`, `CloseApplications=yes` com filtro dos cinco executáveis do KyraFiles, `RestartApplications=no`; `[Run]` abre `kyrafiles.exe` com `postinstall skipifsilent`. Nenhum código de primeiro plano no `.iss` (`git grep -i foreground -- installer` vazio).
- CONFIRMED: `installer/Update-KyraFiles.ps1` inicia o Watchdog oculto (linha 193) e não trata janela.
- NOT TESTED / a descobrir na T0: **quem inicia o instalador na atualização** (o diálogo "Sobre e atualizações" da F018 ou o script) e com que parâmetros; se o processo pai fecha antes de o instalador abrir (perde o direito de ativar a janela).

## 2. Abordagem (PROPOSED, validar na T0)
1. No `.iss`, em `InitializeWizard`/`CurShepChanged`, trazer o `WizardForm` para a frente pela API do Windows (permissão de primeiro plano, `SetForegroundWindow` dentro do instalador, que é a janela dele) e, se recusar, `FlashWindowEx` na barra de tarefas (FR-004).
2. Quem **inicia** o instalador (app ou script) chama `AllowSetForegroundWindow` para o processo filho antes de sair; é o que a regra de primeiro plano do Windows exige (FR-002, AC-2).
3. Trava de 1 s: botões do assistente ignoram clique/tecla até um temporizador de 1 s (FR-003, AC-5).
4. Perguntas do `CloseApplications` e mensagens de erro/reinício passam pelo mesmo ajuda de primeiro plano (AC-6).
5. Segunda instância do instalador: mutex do Inno (`SetupMutex`) e ativação da primeira (AC-8).
Descartadas: janela `TopMost` permanente (rouba o foco e atrapalha); `SendKeys`/Alt para enganar o sistema (frágil).

## 3. Mexer e não mexer
- Mexer: `installer/KyraFilesSetup.iss`; o ponto do app/script que inicia o instalador (definido na T0); testes do instalador.
- Não mexer: conteúdo dos arquivos instalados, lógica de atualização, `src/` além do ponto que inicia o instalador.

## 4. Estratégia de teste
Regra de 26/10/2026: tela **só depois das 23:59**, sem roubar foco nem trazer janela de teste para frente (METODO §16). Os ACs de primeiro plano **medem justamente o primeiro plano**, então o teste observa `GetForegroundWindow` (só leitura) depois de iniciar o instalador na sessão de teste, sem forçar nada. Instalar só em pasta de teste (`/DIR=`), nunca por cima do app do Alexandre; controle: o instalador atual (sem a mudança) tem de reprovar AC-1/AC-2.
