# F062 — Diagnóstico de Consumo de Memória do Ecossistema na Página Sobre — SPEC

Status: **APROVADA pelo Alexandre em 12/11/2026** ("Specs aprovadas"). Estado anterior: **RASCUNHO PARA APROVAÇÃO** (Solicitado pelo Alexandre em 28/10/2026: *"Dentro do about quero uma opcao pa ver memoria que o files está consumindo, considerando todos os módulos"*).

---

## 1. Por quê
O Files Custom é composto por múltiplos processos cooperativos: o processo principal de interface WinUI 3 (`Files.exe`), o servidor COM fora de processo (`Files.App.Server.exe`), o serviço leve de interceptação do atalho Win+E (`Files.HotkeyService.exe`), o guardião do serviço (`Files.Watchdog.exe`) e o serviço indexador opcional (`Files.App.IndexService.exe`).
Hoje o usuário precisa abrir o Gerenciador de Tarefas do Windows para inspecionar quanto cada processo consome. Ter um cartão dedicado na tela "Sobre" (About) que calcula e exibe em tempo real o consumo de memória consolidado e discriminado por processo permite ao usuário e aos desenvolvedores auditar imediatamente o impacto na RAM de todo o ecossistema.

---

## 2. Histórias de Usuário
- **Como usuário**, quero entrar na página "Sobre" do Files Custom e ver em um clique quanto de memória RAM o aplicativo e todos os seus processos em segundo plano estão gastando juntos, para saber se o programa está leve.
- **Como desenvolvedor/suporte**, quero poder ver a discriminação exata por processo (Working Set e Memória Privada) e copiar esses dados para a área de transferência para anexar a relatórios de desempenho e issues.

---

## 3. Cenários de Aceite (AC)

| ID | Dado | Quando | Então |
|---|---|---|---|
| **F062-AC-1** | O usuário abre Configurações > Sobre | Visualiza a seção de Informações do Aplicativo | Há um cartão "Consumo de memória" exibindo o total consolidado (Working Set) de todos os processos do Files Custom ativos |
| **F062-AC-2** | O cartão de consumo de memória | O usuário clica em "Detalhes" (ou expande) | Exibe lista com cada processo (`Files`, `Files.App.Server`, `Files.HotkeyService`, `Files.Watchdog`, `Files.App.IndexService`), seu PID, Working Set (RAM física) e Private Bytes (memória privada) |
| **F062-AC-3** | Algum processo auxiliar não está em execução (ex: IndexService bloqueado) | O usuário consulta os detalhes | O processo ausente é listado claramente como "Inativo (0 MB)", sem gerar erros ou exceções |
| **F062-AC-4** | A janela de detalhes de memória | O usuário clica em "Copiar" ou "Atualizar" | "Copiar" envia o texto estruturado com data/hora e valores para o clipboard; "Atualizar" recalcula os valores instantaneamente |
| **F062-AC-5** | Múltiplas instâncias ou processos com o mesmo nome | É feita a contagem | Soma todas as instâncias existentes sem duplicar PIDs |

---

## 4. Requisitos
- **FR-001:** Consulta direta via `Process.GetProcessesByName` ou APIs nativas de processo do Windows para: `Files`, `Files.App.Server`, `Files.HotkeyService`, `Files.Watchdog`, `Files.App.IndexService`.
- **FR-002:** Formatação clara de memória utilizando KB, MB ou GB com 1 casa decimal.
- **FR-003:** Botão de cópia para clipboard com formato legível para markdown/relatórios.
- **NFR-001:** A consulta não pode travar a thread de UI (executar em background `Task.Run` com retorno rápido < 50 ms).
- **NFR-002:** Tratamento de segurança para processos que encerrarem durante a inspeção (`ArgumentException` ou `InvalidOperationException` descartadas de forma segura).

---

## 5. Critérios de Sucesso
- Total reportado bate exatamente com a soma dos processos visíveis no Gerenciador de Tarefas do Windows (`tasklist` ou `Get-Process`).
- Zero impacto no tempo de inicialização da página Sobre.

