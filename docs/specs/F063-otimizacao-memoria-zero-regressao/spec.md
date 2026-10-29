# F063 — Otimização de Consumo de Memória sem Degradação de Latência (P0) — SPEC

Status: **APROVADO (Prioridade Zero determinada pelo Alexandre em 29/10/2026)**.

---

## 1. Por quê
O diagnóstico de memória do ecossistema (F062) reportou um pico de consumo do `Files.exe` de até **540 MB (Working Set) / 417 MB (Private Bytes)**. Embora em repouso e após garbage collection o consumo caia para cerca de ~238 MB, o usuário manifestou grande preocupação com o impacto percebido ("receio de não usarem por roubar tanta memória").
Em paralelo, a diretriz mandatória do Alexandre exige **equilíbrio absoluto: zero lentidão**. Qualquer redução de memória que provoque travamentos (jank), engasgos de interface ou lentidão de retorno ao restaurar janelas é estritamente proibida.

A investigação no código revelou que o método `TryEmptyWorkingSetWhenIdle` em `App.xaml.cs` já previa o esvaziamento do Working Set (inclusive recebendo o handle do processo), mas **nunca executava a chamada à API `K32EmptyWorkingSet`**, limitando-se a rodar GC e apenas quando o app fechava todas as janelas (modo bandeja/headless). Durante a operação normal (janela minimizada ou ociosa em segundo plano), o Windows mantém dezenas ou centenas de megabytes de páginas XAML/DirectX em RAM física sem necessidade.

---

## 2. Histórias de Usuário
- **Como usuário**, quero que quando o Files Custom for minimizado ou ficar ocioso em segundo plano, ele libere suavemente sua memória RAM física desnecessária, reduzindo o Working Set para a faixa de 40 MB a 80 MB.
- **Como usuário**, quero que ao clicar no ícone do Files na barra de tarefas para restaurá-lo, a janela abra **instantaneamente** (< 50 ms), sem engasgos, sem congelamento e sem travamento de disco.
- **Como desenvolvedor**, quero que a liberação de memória seja segura, não bloqueie a thread de UI e respeite operações ativas (cópia de arquivos, indexação, carregamento de pastas).

---

## 3. Cenários de Aceite (AC)

| ID | Dado | Quando | Então |
|---|---|---|---|
| **F063-AC-1** | O Files Custom está com janelas abertas e com Working Set elevado (> 200 MB) | A janela é minimizada pelo usuário | Após breve intervalo de estabilização (2 segundos), o Working Set é desalocado suavemente, caindo para menos de 80 MB no Gerenciador de Tarefas |
| **F063-AC-2** | A janela do Files está minimizada com Working Set reduzido | O usuário clica para restaurar a janela | A janela restaura com fluidez imediata, sem engasgo percebido; o tempo de restauração é inferior a 50 ms |
| **F063-AC-3** | O Files está em segundo plano (não minimizado, mas sem foco do usuário) | Fica inativo/ocioso por mais de 60 segundos | Um ciclo suave de GC e Working Set trim é acionado em thread de background com prioridade baixa, liberando memória sem travar a interface |
| **F063-AC-4** | Uma operação de arquivo longa (ex: cópia, movimentação, descompactação) está em andamento | A janela é minimizada | O gerenciador de memória detecta operação ativa em `FileOperationsHelpers` e adia o esvaziamento até a conclusão da transferência, evitando qualquer interferência na taxa de transferência |
| **F063-AC-5** | O usuário abre o diálogo de diagnóstico no About (F062) | Após o ciclo de redução em repouso | O Working Set reportado do `Files.exe` reflete a memória reduzida de forma fidedigna |

---

## 4. Requisitos
- **FR-001:** Adicionar suporte nativo à chamada Win32 `PInvoke.K32EmptyWorkingSet` vinculada ao handle do processo corrente.
- **FR-002:** Implementar `MemoryManagerService` (ou helper dedicado) que escuta os eventos de minimização de janela (`WindowStateChanged` / `VisibilityChanged` em `MainWindow`) e temporizador de ociosidade em segundo plano (> 60s).
- **FR-003:** Salvaguarda de I/O ativo: verificar `FileOperationsHelpers` antes de disparar o esvaziamento.
- **NFR-001 (Zero-Jank / Strict Performance):** A chamada de GC e esvaziamento NUNCA deve ser executada na thread de UI (deve rodar em `Task.Run` com prioridade de thread em segundo plano).
- **NFR-002 (Critério de Aceite Mandatório de Latência):** O tempo de restauração da janela minimizada medido por script de automação deve ser indistinguível do baseline antes da alteração (meta: < 50 ms).

---

## 5. Critérios de Sucesso
- Working Set do `Files.exe` cai de ~230-500 MB para < 80 MB após minimização.
- Latência de restauração comprovada por medição automatizada inferior a 50 ms.
- Zero regressão de CPU em repouso (Processo a 0% de CPU).
