# Medicao Tecnica: Entrega de Entrada via PostMessage

**Data:** 2026-10-25 10:47:49  
**Status:** MEDICAO REALIZADA (APENAS MEDICAO, SEM ADOTAR)  
**Escopo:** Janela propria de teste criada exclusivamente pelo script (PostMessage_Measure_Window), sem tocar em nenhum processo de usuario.  

## 1. Motivacao e Ajuste (c) do Claude
Esta medicao atende a solicitacao de revisao (c): registrar em docs/agents/evidence/test-input-guard/ uma medicao de entrega por PostMessage direto na janela, **so em janela criada pelo proprio teste**, dizendo se aceita ou ignora (apenas medicao, sem adotar).

## 2. Resultados Medidos em Janela Propria de Teste

| Cenario de Teste | Mensagem Win32 | Retorno API | Texto Resultante | Comportamento Observado |
|---|---|---|---|---|
| WM_CHAR direto no HWND de controle Edit próprio | WM_CHAR (0x0102, wParam=0x5A 'Z') | True | 'Z' | **ACEITA** |
| WM_KEYDOWN/UP direto no HWND sem passar pelo TranslateMessage do loop | WM_KEYDOWN (0x0100) + WM_KEYUP (0x0101, wParam=0x41) | True | 'aa' | **ACEITA** |
| WM_CHAR postado com janela em segundo plano (background) | WM_CHAR (0x0102, wParam=0x57 'W') | True | 'W' | **ACEITA** |

## 3. Analise Tecnica e Conclusao

1. **Em controles Win32 nativos (Edit control):**
   - WM_CHAR direto via PostMessage e **ACEITO**, inserindo o caractere no controle mesmo sem foco do sistema.
   - WM_KEYDOWN/WM_KEYUP direto via PostMessage e **ACEITO** pelo controle nativo Edit (produzindo o texto 'aa' nos testes), pois a implementacao interna de WndProc do controle Edit/TextBox processa as mensagens de tecla diretamente e insere os caracteres correspondentes na caixa, mesmo sem passagem previa por TranslateMessage no loop da aplicacao.

2. **Aplicabilidade ao WinUI 3 / KyraFiles:**
   - O WinUI 3 utiliza a arvore XAML com InputKeyboardSource e seu proprio despachante de foco (FocusManager).
   - Elementos XAML como TextBox, AutoSuggestBox e itens de grid nao possuem HWNDs Win32 filhos individuais -- compartilham o HWND de nivel superior da janela.
   - Portanto, PostMessage de baixo nivel nao alcanca com precisao nem confiabilidade os controles filhos internos do WinUI 3, alem de nao acionar os manipuladores de eventos de interacao XAML de alto nivel.

3. **Decisao:**
   - **NAO ADOTAR PostMessage como mecanismo de automacao.**
   - Para campos de texto: utilizar estritamente **UI Automation (ValuePattern)** via Set-ElementText.
   - Para atalhos de teclado (VK_*): utilizar estritamente **Send-GuardedKey** com verificacao instantanea e continua de foco via Assert-ForegroundOwned.
