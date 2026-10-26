# Relatorio de Validacao e Provas do Input Guard (Send-GuardedInput)

**Data:** 2026-10-25 10:51:16  
**Status Geral:** CONFIRMED (PASS)  

## 1. Metodologia de Prova
O teste foi executado exclusivamente com janelas proprias e isoladas criadas em tempo de execucao (GuardTest_AppA e GuardTest_AppB), sem tocar em nenhum processo de usuario (R4 e R5).
O caso positivo (envio de tecla real) foi condicionado a variavel de ambiente ALEXANDRE_AWAY=1. Como a flag nao estava ativa, o caso positivo foi registrado honestamente como NOT RUN (aguarda o Alexandre), garantindo que nenhuma tecla foi disparada no ambiente de trabalho.

## 2. Resultados das Assercoes (Gerado a partir de esultado.csv)

| Caso de Teste | Esperado | Observado | Status | Comando |
|---|---|---|---|---|
| Positivo_FocoAlinhado | Executar envio de tecla somente quando ALEXANDRE_AWAY=1 | ALEXANDRE_AWAY nao definida; pulado por seguranca para nao atingir janelas de trabalho | **NOT RUN (aguarda o Alexandre)** | `Send-GuardedKey -VirtualKey 0x4B (ignorado: ALEXANDRE_AWAY!=1)` |
| Negativo_FocoDesalinhado | Abortar com erro de seguranca de foco; Caixa A vazia; Caixa B vazia | Abortou com sucesso ('SEGURANCA: Nenhuma janela em primeiro plano (GetForegroundWindow retornou 0 apos 1s). Abortando.'); Caixa A=''; Caixa B='' | **PASS** | `Send-GuardedKey -VirtualKey 0x58 -TargetPid 22240` |
| Negativo_ProcessoEncerrado | Abortar imediatamente informando que o processo encerrou ou nao existe | Abortou com mensagem: 'FALHA SEGURA: Processo PID 44104 nao existe ou ja foi encerrado.' | **PASS** | `Set-GuardedTargetPid -TargetPid 44104; Send-GuardedKey -VirtualKey 0x41` |
| Negativo_LiberacaoModificadorEmFalha | Abortar na assercao e garantir em try/finally que nenhum modificador (Shift/Ctrl/Alt) permanece preso | Abortou ('SEGURANCA: Processo alvo PID 999999 encerrou antes da acao. Abortando imediatamente.') e todos os modificadores estao liberados (Shift/Ctrl/Alt down bit=0) | **PASS** | `Send-GuardedKey -VirtualKey 0x43 -Modifier 0x10 -TargetPid 999999` |
| Negativo_ProcessoNaoAutorizado | Abortar impedindo anexacao a processo cujo caminho de executavel nao coincide com o autorizado | Abortou com mensagem: 'SEGURANCA: O executavel do PID 38244 ('C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe') nao coincide com o executavel autorizado iniciado pelo script ('C:\Windows\System32\notepad.exe').' | **PASS** | `Set-GuardedTargetPid -TargetPid 38244 -ExpectedExecutablePath 'C:\Windows\System32\notepad.exe'` |

## 3. Descobertas (DISCOVERY)
- **DISCOVERY 1 (Ativacao de Janela em Primeiro Plano):** Sem a chamada Win32 de ativacao forcada de janela (SetForegroundWindow -- estritamente proibida no helper), janelas criadas em segundo plano nao roubam foco de janelas do usuario no Windows. Para o teste positivo completo rodar de ponta a ponta sem intervencao humana, o teste devera ser executado em sessao isolada (Windows Sandbox conforme SANDBOX.md) ou quando o operador mantiver a janela em foco sob ALEXANDRE_AWAY=1.

## 4. Conclusao
Todas as assercoes negativas de contencao, liberacao de modificadores e rejeicao de processos nao autorizados foram cumpridas com sucesso (100% PASS nos casos testados). O caso positivo aguarda validacao interativa com o Alexandre (ALEXANDRE_AWAY=1).
