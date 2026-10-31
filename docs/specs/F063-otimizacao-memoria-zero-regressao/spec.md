# F063 — Otimização de Consumo de Memória sem Degradação de Latência (P0) — SPEC

Status: **RASCUNHO** (voltou a rascunho por decisão do Alexandre em 29/10/2026, Q13: "Volta a rascunho"; o Claude aprova depois das provas das 09:00 de 31/10). A versão anterior estava marcada "APROVADO" pela própria executora.

---

## 1. Por quê
O diagnóstico de memória do ecossistema (F062) reportou um pico de consumo do `Files.exe` de até **540 MB (Working Set) / 417 MB (Private Bytes)**. Embora em repouso e após garbage collection o consumo caia para cerca de ~238 MB, o usuário manifestou grande preocupação com o impacto percebido ("receio de não usarem por roubar tanta memória").
Em paralelo, a diretriz mandatória do Alexandre exige **equilíbrio absoluto: zero lentidão**. Qualquer redução de memória que provoque travamentos (jank), engasgos de interface ou lentidão de retorno ao restaurar janelas é estritamente proibida.

A liberação usada (esvaziar o conjunto de trabalho) reduz a **memória ocupada na RAM**, não o consumo total do processo (memória privada comprometida). A promessa ao usuário fala só da RAM (Q3, 29/10/2026).

---

## 2. Histórias de Usuário
- **Como usuário**, quero que quando o Files Custom for minimizado, ocultado ou mandado para a bandeja, ele devolva ao Windows a RAM que não está usando.
- **Como usuário**, quero que ao restaurar a janela ela volte sem lentidão perceptível em relação a antes da otimização.
- **Como usuário**, quero que o Files **visível** (mesmo sem foco, atrás de outra janela) nunca perca a RAM: ao voltar para ele com alt-tab, nada pode ficar mais lento (Q1, 29/10/2026).
- **Como usuário**, quero que a liberação nunca atrapalhe uma cópia, movimentação ou compactação em andamento.

---

## 3. Cenários de Aceite (AC)

| ID | Dado | Quando | Então |
|---|---|---|---|
| **F063-AC-1** | Files com janela aberta e conjunto de trabalho acima de 200 MB | a janela é minimizada ou ocultada | depois de cerca de 2 s, o conjunto de trabalho privado cai para menos de 80 MB; a memória privada comprometida é registrada à parte, sem promessa de queda |
| **F063-AC-2** | janela minimizada com a RAM liberada | o usuário restaura a janela | o tempo até o primeiro quadro desenhado fica, na mediana e no p95 de 10 rodadas, a no máximo 10 ms da linha de base sem a otimização (Q4 e Q5, 29/10/2026) |
| **F063-AC-3** | janela visível sem foco (atrás de outra) | passa qualquer tempo | **nada é liberado** (o gatilho por perda de foco foi removido, Q1) |
| **F063-AC-4** | cópia, movimentação ou compactação longa em andamento | a janela é minimizada | a liberação espera a operação por no máximo 30 s; se a janela for restaurada antes, desiste na hora; se a operação continuar depois do prazo, a liberação é pulada (nunca uma espera sem fim, Q2) |
| **F063-AC-5** | diagnóstico de memória no Sobre (F062) | depois de uma liberação | o número mostrado é fiel ao que o Windows reporta, com rótulo que diz se é RAM ocupada ou memória privada (A2 da revisão de 30/10) |

---

## 4. Requisitos
- **FR-001:** liberar a RAM do próprio processo ao minimizar, ocultar ou ir para a bandeja.
- **FR-002:** não liberar por perda de foco nem por tempo ocioso com a janela visível.
- **FR-003:** antes de liberar, esperar operações de arquivo ativas com prazo (30 s) e cancelamento pela restauração da janela.
- **NFR-001 (zero travamento):** a liberação nunca roda na thread de interface.
- **NFR-002 (latência):** restauração até o primeiro quadro com mediana e p95 dentro de ±10 ms da linha de base, 10 rodadas, medida por automação que não rouba o foco (Q12), só a partir das 09:00 de 31/10 ou com o Alexandre ausente.

---

## 5. Critérios de Sucesso
- Conjunto de trabalho privado do `Files.exe` abaixo de 80 MB depois de minimizar, medido.
- Restauração dentro de ±10 ms da linha de base (mediana e p95), medida.
- CPU em repouso sem regressão.
- Textos ao usuário falam em "memória ocupada na RAM", nunca em "consumo total".

---

## 6. Histórico
- 29/10/2026 — criada e marcada "APROVADO" pela executora (AGY).
- 30/10/2026 — Claude: volta a RASCUNHO (Q13); remove o gatilho de 60 s por perda de foco (Q1, AC-3 reescrito); espera de operações com prazo e cancelamento (Q2, AC-4); promessa limitada à RAM (Q3); latência até o primeiro quadro, ±10 ms, 10 rodadas (Q4, Q5); rótulo do Sobre (A2).
