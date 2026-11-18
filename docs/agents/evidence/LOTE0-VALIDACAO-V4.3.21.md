# Lote 0 — Checklist Consolidado de Validação em Lote (v4.3.21)

- **Data:** 2026-11-18
- **Versão base testada:** `4.3.21.0` (`a667deb0d`)
- **Objetivo:** Encerrar em 1 única passada consolidada (em vez de 24 ciclos isolados) todas as SPECs cujo código já está integrado na `main`, separando apenas o que tiver bug real em ticket/correção pontual.
- **Autorização:** Alexandre (18/11/2026 — D-TRAIN-1).

---

## 1. Resolvidas administrativamente neste Lote 0 (sem teste extra necessário)

| SPEC | Legado | Ação executada | Evidência / Motivo |
|---|---|---|---|
| `SPEC-0013` | `F015` | `cancelled` (`invalidas`) | Conflita com `F021` (`bloqueio-indexservice`, gate obrigatório) e substituída por `F023` (busca híbrida). |
| `SPEC-0072` | `F088` | `completed` (`finalizadas`) | Absorvida integralmente pela `F091` / v4.3.19.0 (`tools/check-legacy-names.ps1` = 0 violações). |
| `SPEC-0054` | `F058` | `not_started` (`fila`) | Já aprovada em 12/11/2026; destravada para o **Lote 1 (`v4.3.22`)**. |

---

## 2. Lote Recente da v4.3.21 (5 SPECs — Validação Automática + Visual Consolidada)

Estas 5 SPECs já estão com lógica/testes unitários verdes na `v4.3.21` e aguardam apenas 1 rodada única de verificação em runtime/tela:

- [ ] **F090 (`SPEC-0074`, 5 pts) — Escala da interface (v1 conservadora — D-F090-1):**
  - Lógica/unitários: `CONFIRMED` (19/19 `UiScaleMath`, build 0/0).
  - Prova visual: alternar 80% → 100% → 125% → 150% em Configurações > Aparência, conferir nitidez, drag da barra de título e janelas secundárias (`f090_scale_test.ps1`).
- [ ] **F062 (`SPEC-0057`, 5 pts) — Diagnóstico de memória do ecossistema:**
  - Lógica/unitários: `CONFIRMED` (8/8 `EcosystemMemoryDiagnosticsTests`, pt-BR/en-US completos).
  - Prova visual: abrir Configurações > Sobre e confirmar exibição do consumo do ecossistema sem processos alheios.
- [ ] **F063 (`SPEC-0058`, 8 pts) — Otimização de memória com zero regressão:**
  - Lógica/script: `CONFIRMED` (`measure_f063_memory_restore.ps1` alinhado a `KyraFiles`).
  - Prova em runtime: rodar bateria de 10 rodadas (C7) + captura Sobre (C8) confirmando RAM contida e Gate F021.
- [ ] **F060 Onda A (`SPEC-0055`, 13 pts — M1/M2/M5; Onda B SFTP/Script no backlog — D-F060-2):**
  - Lógica/unitários: `CONFIRMED` (78/78 testes verdes).
  - Prova visual: abrir Editor Hexadecimal (M2), Dividir/Combinar arquivo (M5) e Busca por frase (M1) em pasta de teste.
- [ ] **F070 (`SPEC-0063`, 3 pts) — GitHub Login OAuth:**
  - Lógica/API: `CONFIRMED` (Client ID real configurado em `Constants.cs`, HTTP 200 OK em `github.com/login/device/code` em 14/11/2026).
  - Prova visual: acionar login GitHub na UI e confirmar exibição do código de dispositivo (AC-1).

---

## 3. Lote Legado em Uso Diário na `main` (19 SPECs — Aceite Consolidado)

Código está em produção diária desde out/2026 (v4.3.2 a v4.3.21). Validação em 1 passada única de fumaça + confirmação de uso real (qualquer item com defeito vira bugfix isolado sem travar os demais):

| # | SPEC | Legado | Pts | Verificação rápida consolidada | Estado |
|--:|---|---|--:|---|---|
| 1 | `SPEC-0003` | `F004` | 3 | Menu clássico visível com `Alt+A/E/V/H` e itens habilitados corretamente | [ ] |
| 2 | `SPEC-0004` | `F005` | 2 | Atalhos `Alt+P` / `Alt+Shift+P` abrem/fecham Preview e Details em 1 passo | [ ] |
| 3 | `SPEC-0009` | `F011` | 3 | Seletor de motor de busca em Configurações > Pastas persiste e roteia | [ ] |
| 4 | `SPEC-0012` | `F014` | 5 | `F1` abre Ajuda contextual integrada (aprovada retroativamente em 18/11) | [ ] |
| 5 | `SPEC-0014` | `F017` | 5 | `Win+E` abre KyraFiles consistentemente (`verify_win_e_health.ps1` = OK) | [ ] |
| 6 | `SPEC-0017` | `F020` | 2 | Endereço repetido foca aba existente; Configurações abre no índice 0 (aprovada em 18/11) | [ ] |
| 7 | `SPEC-0019` | `F022` | 3 | Startup limpo + instalador sem processos órfãos | [ ] |
| 8 | `SPEC-0020` | `F023` | 8 | Busca híbrida rápida sem travar UI e sem crash ao pressionar `Esc` | [ ] |
| 9 | `SPEC-0022` | `F026` | 5 | Seleção múltipla / teclado coalescida sem pico de CPU | [ ] |
| 10 | `SPEC-0025` | `F029` | 3 | Setas cima/baixo fluidas no modo Detalhes como Explorer | [ ] |
| 11 | `SPEC-0026` | `F030` | 3 | Preview pausado durante navegação com tecla segurada | [ ] |
| 12 | `SPEC-0028` | `F032` | 3 | Fixar aba (`Pin Tab`) persiste e protege contra fechamento acidental | [ ] |
| 13 | `SPEC-0029` | `F033` | 3 | "Copiar para" / "Mover para" no menu de contexto com histórico de pastas | [ ] |
| 14 | `SPEC-0030` | `F034` | 2 | Lista/Grade sem checkbox por padrão | [ ] |
| 15 | `SPEC-0031` | `F035` | 5 | Múltiplas aberturas seguidas sem crash (`SemaphoreFullException` corrigido na v4.3.7). **Nota aberta:** AC-5 (trazer janela estacionada ao primeiro plano) permanece como bugfix rastreado separado | [ ] |
| 16 | `SPEC-0056` | `F061` | 5 | Saúde permanente do Win+E (`verify_win_e_health.ps1` = `[OK]`) | [ ] |
| 17 | `SPEC-0059` | `F065` | 5 | Busca por nome e caminho completo funcionando | [ ] |
| 18 | `SPEC-0070` | `F086` | 3 | Instância única redirecionando novas aberturas para a janela ativa | [ ] |
| 19 | `SPEC-0073` | `F089` | 3 | Fontes escolhidas em Aparência aplicadas de fato nas áreas da UI | [ ] |
