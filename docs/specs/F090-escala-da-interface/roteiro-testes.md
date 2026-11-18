# F090 — Roteiro Formal de Testes (Escala da Interface)

> **Pré-requisito de execução:** rodar na máquina com Visual Studio + Windows SDK + Windows App SDK.
> **Regra de foco:** não roubar foco da janela do usuário; automação sem entrada global crua (`Test-NoRawInput.ps1`).
> **Temporários:** qualquer arquivo intermediário deve ir para `%TEMP%\agy\<GUID>\desenv\utils\KyraFiles\KyraFiles\`.

---

## 1. Preparação e Build Oficial (Release x64)

1. Confirmar branch `feature/f090-escala` atualizada sobre a `main`.
2. Compilar em Release x64 com `0 Error(s)` e `0 Warning(s)`:
   ```powershell
   msbuild -restore src\KyraFiles.App\KyraFiles.App.csproj -p:Configuration=Release -p:Platform=x64 -v:quiet -clp:ErrorsOnly
   ```
3. Registrar e montar o pacote dev sem roubar foco:
   ```powershell
   .\Open-KyraFilesDev.ps1 -Configuration Release -NoLaunch
   ```
4. Rodar os testes lógicos de matemática de escala:
   ```powershell
   powershell -NoProfile -File tests\test-ui-scale-math.ps1
   ```
   Esperado: **19/19 PASS**.
5. Rodar a trava de automação segura:
   ```powershell
   powershell -NoProfile -File tools\perf\Test-NoRawInput.ps1
   ```
   Esperado: exit `0`.

---

## 2. Casos de Teste por Critério de Aceite (`AC-1` a `AC-8` + `NFR-1` a `NFR-3`)

| Caso | Requisito | Ação | Critério de Aprovação |
|---|---|---|---|
| **TC-01** | AC-1, FR-1, FR-3 | Abrir **Configurações > Aparência**, localizar card **Escala da interface** e alterar de `100% (padrão)` para `125%`. | `MainWindow` e janela modal de Configurações crescem exatamente 25% imediatamente (sem reiniciar), sem cortes à direita/embaixo e sem barra de rolagem horizontal nova na janela. |
| **TC-02** | AC-2 | Alterar escala para `80%`. | Textos, ícones, barra de endereço, abas e barra lateral reduzem 20% com nitidez; mais linhas ficam visíveis na lista de arquivos; nenhuma faixa vazia na janela. |
| **TC-03** | AC-6 | Retornar para `100% (padrão)`. | Layout retorna exatamente ao tamanho original (`Width`/`Height = NaN`, `Stretch`, `Scale = 1`). |
| **TC-04** | AC-1, AC-7 | Alterar escala para `150%`, maximizar a janela, restaurar e redimensionar até próximo ao mínimo (`416x316`). | Interface preenche 100% da área cliente em todas as dimensões, sem faixas vazias nem recorte. |
| **TC-05** | AC-3, FR-2 | Definir `125%`, fechar o KyraFiles completamente e reabrir. | O app já abre na primeira tela em `125%`, e `"Escala da interface"` exibe `125%` selecionado. |
| **TC-06** | FR-2, AC-4 | Com escala em `125%`, abrir: (a) **Configurações**, (b) **Personalizar barra**, (c) **Propriedades** de um arquivo (e reabrir uma segunda vez para testar o cache de janela), (d) **Editor Hexadecimal**. | Todas as 4 janelas secundárias abrem na escala `125%` e respeitam alterações ao vivo enquanto abertas. |
| **TC-07** | AC-5 | Em `80%` e `125%`: (a) arrastar a janela pela barra de título inclusive no canto direito (os últimos 20% da largura); (b) clicar/duplo-clique/selecionar arquivos com o mouse; (c) abrir menu de contexto com botão direito na metade inferior da tela e perto do rodapé. | Arraste da barra de título funciona em 100% da largura útil; cliques acertam o item sob o cursor; menu de contexto abre na posição correta sem deslocamento. |
| **TC-08** | FR-4 | Combinar `Densidade = Ultra-compacto` com `Escala = 125%` e depois `80%`. | Densidade e escala coexistem sem sobrescrever uma à outra. |
| **TC-09** | AC-8, NFR-2 | Testar em monitor com escala do Windows em `150%` (ou arrastar janela entre monitores de DPI diferentes) com escala do app em `125%`. | Texto permanece nítido e a compensação de layout se ajusta ao novo `RasterizationScale` sem corte. |
| **TC-10** | NFR-3 | Navegar até o `ComboBox` de Escala via teclado (`Tab` / setas) e inspecionar `AutomationProperties.Name` via UI Automation. | Controle acessível por teclado e com nome localizado `"Escala da interface"` / `"Interface scale"`. |
| **TC-11** | NFR-1 | Rodar automação `tools\perf\f090_scale_test.ps1` e `pwsh tools\perf\verify_no_index_service.ps1`. | `KyraFiles.App.IndexService.exe` inativo, CPU em repouso `< 5%`, tempo de startup e RAM em `150%` equivalentes ao baseline `100%`/`main`. |

---

## 3. Execução Automatizada de Evidências (Tela + Métricas)

```powershell
pwsh -NoProfile -File tools\perf\f090_scale_test.ps1 -WaitSeconds 6
pwsh -NoProfile -File tools\perf\verify_no_index_service.ps1
```

Saídas geradas:
- Capturas `PrintWindow` em `docs\agents\evidence\f090\scale_80.png`, `scale_100.png`, `scale_125.png`, `scale_150.png`.
- Tabela consolidada `docs\agents\evidence\f090\resultado.csv` (uma linha por escala/cenário, com CPU, RAM, dimensões de janela e status).
