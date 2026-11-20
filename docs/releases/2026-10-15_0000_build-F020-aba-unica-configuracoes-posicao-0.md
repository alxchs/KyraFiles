# Build 2026-10-15 00:00 — F020 Aba única por endereço e Configurações na primeira posição

- **Executável:** `src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\KyraFiles.dll`, registrado por `.\Open-KyraFiles.ps1 -Configuration Release` (pacote `KyraFilesDev_4.2.37.0_x64__sxng2gqmgahde`).
- **Código:** branch `feature/f020-aba-unica-config-primeira-posicao`.
- **Build:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Testes lógicos:** `tools\perf\test_f020_logic.ps1` = `16/16` testes aprovados (CONFIRMED).

## O que foi liberado

| Item | O que muda para o usuário | Estado |
|---|---|---|
| Aba única por endereço | Tentar abrir um caminho já aberto em outra aba (via protocolo, atalhos, menu de contexto ou nova aba) não duplica a aba; transfere o foco diretamente para a aba existente. | CONFIRMED (`NavigationHelpers.cs`, `AddNewTabByPathAsync`, `AddNewTabByParamAsync`) |
| Normalização estrita de caminho | Diferenças de caixa (`c:\windows` vs `C:\WINDOWS`) ou barras (`C:\Windows\` vs `C:/Windows`) são unificadas e tratadas como o mesmo endereço. | CONFIRMED (`test_f020_logic.ps1`, 16/16 casos) |
| Configurações sempre na posição 0 | Ao abrir Configurações (via atalho `Ctrl+,`, barra lateral ou menu), a aba é aberta sempre na primeira posição (extrema esquerda, índice 0). | CONFIRMED (`NavigationHelpers.cs`, forçando índice 0) |
| Reabertura de Configurações | Se a aba de Configurações já estiver aberta em qualquer posição, acioná-la move-a para o índice 0 e coloca-a em foco sem criar aba secundária. | CONFIRMED (`AppInstances.Move(existingIndex, 0)`) |
| Barra lateral não destrói pasta ativa | Clicar no botão de Configurações na barra lateral abre/seleciona a aba no índice 0 sem sobrescrever o conteúdo da pasta em exibição. | CONFIRMED (`SidebarViewModel.cs`) |
| Navegação na barra de endereços (Omnibar) | Digitar ou navegar para um endereço já presente em outra aba transfere o foco para a aba existente, mantendo a aba atual intacta. | CONFIRMED (`NavigationToolbarViewModel.cs`) |
| Duplicar aba protegido | O comando Duplicar Aba (`Ctrl+Shift+K`) respeita a regra de unicidade e não cria duplicata de um caminho já aberto. | CONFIRMED (`DuplicateSelectedTabAction.cs`) |

## Como testar

1. Feche o KyraFiles e execute `.\Open-KyraFiles.ps1 -Configuration Release`.
2. Abra uma pasta qualquer (ex: `C:\Windows`).
3. Tente abrir novamente `C:\Windows` (via atalho ou menu): observe que o app foca a aba existente sem criar uma segunda aba.
4. Pressione `Ctrl+,` para abrir as Configurações: observe que a aba abre no índice 0 (extrema esquerda), deslocando as abas de pastas para a direita.
5. Em outra aba de pasta, digite no endereço o caminho já aberto na primeira aba: o foco alterna para a primeira aba e a pasta da aba atual não é perdida.
