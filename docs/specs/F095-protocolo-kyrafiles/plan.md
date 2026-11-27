# F095 — PLAN (retroativo)

Status: **APROVADO retroativamente** (Claude, 26/11/2026). Descreve o que já está no código (CONFIRMED por leitura em 26/11/2026).

## Requisito → código
| Requisito | Código (CONFIRMED) |
|---|---|
| FR-1 | `src/KyraFiles.Shared/Helpers/ProtocolUriParser.cs` (`Parse`, `ProtocolAction`); chamadas em `MainWindow.xaml.cs`, `Program.cs` (`cmdProtocolArgs`), `NavigationHelpers.cs`, `BaseOpenInNewWindowAction.cs` |
| FR-2 | `ProtocolUriParser.ProtocolScheme` / `ProtocolUriPrefix`; `Constants.Protocol` removido em `a6fc46b0b` |
| FR-3 | `installer/KyraFilesSetup.iss` (purga do pacote antigo, `RemoveBackslashUnlessRoot`, versão `{#MyAppVersion}.0` por igualdade) |
| FR-4 | `src/KyraFiles.Shared/Helpers/TextEncodingHelper.cs` (`DetectEncoding`); `BaseStorageFile.ReadTextAsync` + `PrependSampleStream` |
| AC-9 | `.github/scripts/Configure-AppxManifest.ps1` com `-creplace` só nos literais `"kyrafiles"`, `"kyrafiles:`, `"kyrafiles-dev"`, `"kyrafiles-dev:` |

## Não mexer
- Os `.cpp` de Launcher, OpenDialog, SaveDialog e HotkeyService continuam usando o literal `kyrafiles:` (o script de canal troca esse literal).

## Testes por AC
- AC-1 a AC-5: `SmokeTests.ProtocolUriParser_*` (6 testes).
- AC-7: `SmokeTests.TextEncodingHelper_DetectEncoding_Windows1252AndUtf8`.
- AC-8: revisão de código (`PrependSampleStream`); nenhuma origem real sem seek testada.
- AC-9: simulação das trocas do script sobre `src/**/*.cs|*.cpp` em memória.
- AC-6: execução do instalador 4.3.33 (não refeita nesta reconciliação).
