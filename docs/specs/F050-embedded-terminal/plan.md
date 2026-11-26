# F050 — Terminal embutido no rodapé — PLAN

Status: APROVADO SÓ ATÉ A T0 (Claude, 25/11/2026; ver "Ajustes da revisão"). **T0 decide implementação vs adiamento** conforme `spec.md §8`. Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026).

## 1. Arquitetura atual (lida no código em 25/11/2026)

- **CONFIRMED** (`src/KyraFiles.App/Actions/Open/OpenTerminalAction.cs`): o app já possui abertura externa de terminal (`wt.exe -d <pasta>` quando Windows Terminal é o padrão, ou fallback) via `IContentPageContext` e atalho `Ctrl+\`` (`Keys.Oem3, KeyModifiers.Ctrl`).
- **CONFIRMED** (`src/KyraFiles.App/Views/ShellPanesPage.xaml.cs`): gerencia os painéis de navegação por aba, mas não possui host de terminal embutido no rodapé nem ponte ConPTY.
- **CONFIRMED** (`spec.md §8`): ponto em aberto decidido pela recomendação — *"decidir após medir o custo do componente; sem medição, adiar."*

## 2. Abordagem

1. **T0 — Prova de custo ConPTY + controle visual (gate de decisão de `spec.md §8`):**
   - Medir impacto em RAM, tempo de abertura (meta ≤ 1 s, NFR-001) e tamanho de pacote de um host ConPTY (`CreatePseudoConsole` via `KyraFiles.App.CsWin32`) com renderizador leve (sem WebView2 pesado se não justificar) vs manter apenas o `OpenTerminalAction` existente.
   - Registrar parecer técnico com números reais; se o custo exceder o orçamento de memória/manutenção, recomendar formalmente o adiamento ao Alexandre antes de codificar a UI completa.
2. **Se aprovado após T0:**
   - **Host ConPTY (`ConPtySession`):** cria pseudo-console via Win32 (`CreatePseudoConsole`, pipes anônimos, `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`), gerencia árvore de processos com Job Object (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`) garantindo 0 processos órfãos ao fechar o app (FR-003).
   - **Rodapé em `ShellPanesPage`:** painel retrátil no rodapé da aba ativa, iniciando na pasta atual (ou pasta válida com aviso se UNC/>260 chars, AC-6), com opção "Acompanhar pasta" que envia `cd` apenas quando não há comando em execução (AC-2, AC-3), seleção de interpretador (PowerShell, cmd, WSL, Git Bash, FR-001) e ação "Colar caminho" (AC-7).
   - **Isolamento de teclado (FR-002):** quando o terminal tem foco, teclas não são interceptadas pelos atalhos do gerenciador, exceto o atalho global de abrir/fechar o rodapé.

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| T0 (medição de custo) | `docs/agents/evidence/F050/cost-measurement.md` |
| FR-003 (ConPTY + Job Object) | Novo `ConPtySession.cs` + `src/KyraFiles.App.CsWin32/NativeMethods.txt` |
| US1, US2, US3, AC-1..AC-7, FR-001, FR-002 | Novo `EmbeddedTerminalControl.xaml(.cs)`, `EmbeddedTerminalViewModel.cs`, integração em `ShellPanesPage.xaml(.cs)` |

**A NÃO mexer:** `OpenTerminalAction.cs` (mantido como opção externa).

## 4. Riscos

- Vazamento de processos filhos (`pwsh.exe`/`cmd.exe`/`bash.exe`) ao fechar aba ou app: mitigar obrigatoriamente com Win32 Job Object (`KILL_ON_JOB_CLOSE`).
- Consumo elevado de RAM por instância se usar WebView2/xterm.js: avaliar em T0 antes de escolher o renderizador.

## 5. Estratégia de teste

| AC | Como se prova |
|---|---|
| AC-1 / NFR-001 | Atalho abre rodapé na pasta atual em ≤ 1 s |
| AC-2 / AC-3 | "Acompanhar pasta" muda diretório ocioso; não interrompe comando ativo |
| AC-4 / AC-5 | Redimensionar reflui texto; fechar com processo ativo pede confirmação |
| AC-6 / AC-7 | UNC/>260 abre fallback com aviso; "Colar caminho" insere caminho entre aspas |
| FR-003 | Fechar app com terminal aberto: 0 processos filhos restantes |

## 6. Ajustes da revisão (Claude, 25/11/2026)

- **Aprovada apenas a T0.** T1 a T3 ficam bloqueadas até o Alexandre decidir com os números da T0 na mão.
- T0 compara pelo menos dois renderizadores com números: (a) WebView2 + xterm.js; (b) renderizador nativo de VT. Para cada um: RAM por instância, tempo até o prompt, tamanho acrescido ao instalador e esforço estimado em pontos.
- Os 8 pontos atuais valem só para o caminho WebView2 + xterm.js. Se a T0 indicar renderizador nativo, reestimar antes de pedir aprovação.
- Qualquer processo de teste da T0 roda em Job Object e é encerrado ao fim; registrar a contagem de processos filhos antes e depois.
