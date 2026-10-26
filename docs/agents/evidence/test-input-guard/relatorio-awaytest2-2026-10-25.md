# Relatorio de Validacao e Provas do Input Guard (Send-GuardedInput)

**Data:** 2026-10-25 17:42:35  
**Status Geral:** FAILED  

## 1. Metodologia de Prova
O teste foi executado exclusivamente com janelas proprias e isoladas criadas em tempo de execucao (GuardTest_AppA e GuardTest_AppB), sem tocar em nenhum processo de usuario (R4 e R5).
O caso positivo (envio de tecla real) foi condicionado a variavel de ambiente ALEXANDRE_AWAY=1. Como a flag nao estava ativa, o caso positivo foi registrado honestamente como NOT RUN (aguarda o Alexandre), garantindo que nenhuma tecla foi disparada no ambiente de trabalho.

## 2. Resultados das Assercoes (Gerado a partir de esultado.csv)

| Caso de Teste | Esperado | Observado | Status | Comando |
|---|---|---|---|---|
| Positivo_FocoAlinhado | Tecla entregue exclusivamente a A; Caixa A='K' | Caixa A='' | **FAIL** | `Send-GuardedKey -VirtualKey 0x4B -TargetPid 43160` |
| Negativo_FocoDesalinhado | Com janela B em foco, envio para A aborta citando expressamente PID de B; Caixa A vazia; Caixa B vazia | Abortou citando PID de B ('SEGURANCA: Foco violado! Janela em primeiro plano pertence ao PID 24016, mas o processo exclusivo do teste e PID 43160. Abortando automacao para proteger janelas do usuario.'); Caixa A=''; Caixa B='' | **PASS** | `Send-GuardedKey -VirtualKey 0x58 -TargetPid 43160` |
| Negativo_ProcessoEncerrado | Abortar imediatamente informando que o processo encerrou ou nao existe | Abortou com mensagem: 'FALHA SEGURA: Processo PID 35484 nao existe ou ja foi encerrado.' | **PASS** | `Set-GuardedTargetPid -TargetPid 35484; Send-GuardedKey -VirtualKey 0x41` |
| Negativo_LiberacaoModificadorEmFalha | Induzir falha apos descida do modificador e provar via try/finally que Shift/Ctrl/Alt voltam a estar soltos (GetAsyncKeyState) | Falha induzida capturada ('FALHA_INDUZIDA_TESTE: Simulando falha imediatamente apos descida do modificador para testar try/finally.'); try/finally liberou modificador; GetAsyncKeyState confirma Shift/Ctrl/Alt soltos | **PASS** | `Send-GuardedKey -VirtualKey 0x43 -Modifier 0x10 -TargetPid 43160 -InjectFaultAfterModifier` |
| Negativo_ProcessoNaoAutorizado | Abortar impedindo anexacao a processo cujo caminho de executavel nao coincide com o autorizado | Abortou com mensagem: 'SEGURANCA: O executavel do PID 24016 ('C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe') nao coincide com o executavel autorizado iniciado pelo script ('C:\Windows\System32\notepad.exe').' | **PASS** | `Set-GuardedTargetPid -TargetPid 24016 -ExpectedExecutablePath 'C:\Windows\System32\notepad.exe'` |

## 3. Descobertas (DISCOVERY)
- **DISCOVERY 1 (Ativacao de Janela em Primeiro Plano):** Sem a chamada Win32 de ativacao forcada de primeiro plano (estritamente proibida no helper), janelas criadas em segundo plano nao roubam foco de janelas do usuario no Windows. Para o teste positivo completo rodar de ponta a ponta sem intervencao humana, o teste devera ser executado em sessao isolada (Windows Sandbox conforme SANDBOX.md) ou quando o operador mantiver a janela em foco sob ALEXANDRE_AWAY=1.

## 4. Conclusao
Houve reprovacao em um ou mais casos de teste.
