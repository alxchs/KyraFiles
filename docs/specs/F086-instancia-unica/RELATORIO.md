# F086 - Relatorio parcial

Data: 02/11/2026. Estado por AC (METODO 7):

| AC | Estado | Evidencia |
|---|---|---|
| AC-1 | OBSERVED | App aberto; `kyrafiles.exe` executado 2 vezes seguidas: `Get-Process KyraFiles` = 1 antes e 1 depois, mesmo PID (25252). |
| AC-2 | NOT TESTED | Abertura com caminho nao exercitada. |
| AC-3 | NOT TESTED | Corrida de duas aberturas simultaneas nao exercitada. |
| AC-4 | NOT TESTED | Opcao desligada nao exercitada. |
| AC-5 | NOT TESTED | Processo principal travado nao exercitado. |
| AC-6 | NOT TESTED | Dialogo de abrir/salvar nao exercitado. |

Medicao: n = 1 rodada, sem comparativo antes/depois de memoria. Memoria privada total dos processos `KyraFiles*` na hora: 290 MB (`PrivateMemorySize64`, soma). Nao ha baseline da versao anterior (NFR-1 e comparativo de performance seguem NOT TESTED).

## Defeitos achados ao colocar para funcionar (cada um impedia o app de abrir)

1. `Themes/Generic.xaml` dos Controls carregava `ms-appx:///KyraFiles.App.Controls/...`; o assembly agora e `KyraFiles.App.Controls`. Sintoma: processo encerra em ~2 s com `0x802B000A` (XAML parse failed).
2. `Package.appxmanifest` declarava `windows.appExecutionAlias` duas vezes (`kyrafiles.exe` e `kyrafiles.exe`); o Windows recusa o registro (`0x80080204`). Removido o bloco do `kyrafiles.exe`; versao 4.3.11.0.
3. Registrar a mesma versao de novo com manifesto diferente exige `Remove-AppxPackage -PreserveApplicationData` antes (`0x80073CFB`).
