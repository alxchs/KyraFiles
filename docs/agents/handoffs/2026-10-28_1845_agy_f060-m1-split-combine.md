# Handoff: F060 M1 — Dividir e Juntar Arquivos (Split/Combine)

- **Data/Hora:** 2026-10-28 18:45 (horário local)
- **Agente:** AGY (Google DeepMind)
- **Módulo:** F060 M1 (Dividir/Juntar)
- **Branch:** `feature/f060-m1-split-combine`
- **Estado Geral:** T1 a T4 CONCLUÍDAS e REVISADAS; T5 (Prova de Tela) PRONTA PARA EXECUÇÃO.

---

## 1. Estado Atual por Critério de Aceite (M1-AC-n)

| ID | Cenário | Estado Lógica / Backend | Estado Tela / UI | Evidência / Prova |
|---|---|---|---|---|
| **M1-AC-1** | Arquivo de 10 MB dividido em partes de 4 MB gera 3 partes sequenciais (.001, .002, .003) e manifesto `.manifest.json` com original intacto | **CONFIRMED** | **NOT TESTED** | Testes unitários `SplitEngineTests.cs` (5 testes verdes, commit `22c4b8f2b`); `SplitFileAction` implementada |
| **M1-AC-2** | Juntar partes conferindo resumos CRC32 e SHA-256 e recompor arquivo idêntico ao original | **CONFIRMED** | **NOT TESTED** | Testes unitários `CombineEngineTests.cs` (commit `6d4037982`); arquivo montado em `.tmp` e promovido apenas após validação |
| **M1-AC-3** | Rejeição imediata de conjunto com parte ausente, duplicada, truncada ou alterada | **CONFIRMED** | **NOT TESTED** | `CombineValidationException` lançada antes da escrita; teste de controle comprovou rejeição sem corrupção |
| **M1-AC-4** | Casos de borda: arquivo vazio, menor que uma parte, ou múltiplo exato | **CONFIRMED** | **NOT TESTED** | Coberto por bateria de testes de limites em `SplitEngineTests`; manifesto v1 padronizado |
| **M1-AC-5** | Política de colisão de destino no combine (Recusar [padrão], Sobrescrever, Renomear) | **CONFIRMED** | **NOT TESTED** | `CombineCollisionPolicy` testada; `GetFor_CombineFilesDialog` oferece seleção ao usuário |
| **M1-AC-6** | Respeito à falta de espaço em disco e I/O com interrupção limpa e sem resíduos | **CONFIRMED** | **NOT TESTED** | Teste com stream que falha no meio não deixa arquivo final com nome de pronto; remoção limpa do `.tmp` |
| **NFR-002** | Memória invariante em arquivo de 4 GB (pico constante) | **CONFIRMED** | N/A | Medição com arquivo de 4 GB (`measure_split_combine_20261027_073241.csv`): pico de 35,69 MB (256 MB) vs 35,39 MB (4 GB), razão 0,99x |

Comandos de verificação de testes:
```powershell
dotnet test tests\KyraFiles.App.Search.Index.Tests --filter SplitCombine
```
Saída real: 85 testes aprovados, 0 falhas, 1 ignorado.

---

## 2. Roteiro de Execução da Prova de Tela (M1-T5)

A prova de tela deve ser executada após as 23:59, sem roubo de foco do usuário, via automação UI Automation / script PowerShell.

### 2.1 Pré-condições e Ambiente Isolado
1. Criar pasta temporária isolada de teste:
   `$lab = "$env:TEMP\agy\$(New-Guid)\f060_m1_lab"`
   `New-Item -ItemType Directory -Path $lab -Force`
2. Gerar arquivo de teste controlado de 10 MB (10.485.760 bytes):
   `$testFile = Join-Path $lab "dados_teste.bin"`
   `$bytes = New-Object byte[] 10485760; (New-Object Random 42).NextBytes($bytes); [IO.File]::WriteAllBytes($testFile, $bytes)`
   `$hashOriginal = (Get-FileHash $testFile -Algorithm SHA256).Hash`

### 2.2 Passo a Passo dos Cenários de Tela

#### Cenário 1: Dividir Arquivo (M1-AC-1)
1. Navegar na instância de teste para `$lab`.
2. Selecionar `dados_teste.bin`.
3. Abrir menu de contexto (clique direito) -> Localizar item "Dividir em partes...".
4. Confirmar que o diálogo `GetFor_SplitFileDialog` abre.
5. Selecionar tamanho de parte customizado: `4 MB` (ou 4.194.304 bytes).
6. Marcar opção "Calcular resumo SHA-256".
7. Clicar em "Dividir".
8. **Verificação:** Card de progresso aparece no Status Center. Ao término, a pasta contém:
   - `dados_teste.bin` (original intacto, mesmo hash)
   - `dados_teste.bin.001` (4.194.304 bytes)
   - `dados_teste.bin.002` (4.194.304 bytes)
   - `dados_teste.bin.003` (2.097.152 bytes)
   - `dados_teste.bin.manifest.json`

#### Cenário 2: Juntar Arquivo (M1-AC-2 e M1-AC-5)
1. Renomear o original para `dados_teste.bin.orig` para evitar colisão imediata.
2. Selecionar `dados_teste.bin.manifest.json`.
3. Abrir menu de contexto -> Clicar em "Juntar partes...".
4. Confirmar que o diálogo `GetFor_CombineFilesDialog` abre exibindo pasta de destino e política `Refuse`.
5. Clicar em "Juntar".
6. **Verificação:** Card no Status Center progride até 100%. Arquivo `dados_teste.bin` é recriado.
   Conferir integridade: `(Get-FileHash "$lab\dados_teste.bin" -Algorithm SHA256).Hash -eq $hashOriginal` (deve ser True).

#### Cenário 3: Controle Negativo — Parte Corrompida (M1-AC-3)
1. Modificar propositalmente 1 byte no meio de `dados_teste.bin.002`:
   `$p2 = "$lab\dados_teste.bin.002"; $b = [IO.File]::ReadAllBytes($p2); $b[100] = $b[100] -bxor 0xFF; [IO.File]::WriteAllBytes($p2, $b)`
2. Apagar o arquivo montado anteriormente: `Remove-Item "$lab\dados_teste.bin" -Force`.
3. Selecionar `dados_teste.bin.manifest.json` -> "Juntar partes...".
4. **Verificação esperada:** O processo recusa a operação, o Status Center exibe erro em linguagem amigável indicando que a parte 2 está corrompida/adulterada, e nenhum arquivo `dados_teste.bin` é deixado no disco.

#### Cenário 4: Controle Negativo — Colisão de Destino (M1-AC-5)
1. Recriar `dados_teste.bin` vazio no destino.
2. Tentar juntar com a política padrão "Recusar".
3. **Verificação esperada:** Operação recusa a sobrescrita e avisa que o arquivo já existe no destino.

### 2.3 Registro de Evidências
- Gravar saída em `docs/agents/evidence/F060/m1_tela_resultado.csv` no formato:
  `Cenario,Passou,TempoMs,Detalhe`
- Captura de tela PNG das janelas de diálogo e Status Center via `tools/perf/` (`PrintWindow`).

