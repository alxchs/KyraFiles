# F069 — Sair para o Explorer após quedas seguidas — SPEC

Status: **APROVADA PARA EXECUÇÃO**  
Metodologia: Spec-Driven Development (`METODO.md` §6)  
Origem: Item 1 de `docs/agents/FILA-KYRAFILES.md` (resiliência operacional do substituto do Explorer)

---

## 1. Por quê

O KyraFiles substitui o Windows Explorer como gerenciador de arquivos padrão do usuário. No entanto, se o KyraFiles encontrar um problema grave no momento da inicialização (falha em runtime, DLL corrompida, travamento de XAML ou exceção fatal de startup) e fechar repentinamente logo após abrir, o usuário não pode ficar desamparado sem conseguir abrir suas pastas ou navegar pelo sistema de arquivos.

Hoje, o mecanismo de inicialização e o lançador já possuem fallback quando o aplicativo está desinstalado ou o executável ausente (F012). Porém, quando o aplicativo existe fisicamente mas sofre quedas repetidas durante ou imediatamente após a abertura, o sistema entra em um ciclo frustrante onde o usuário clica e nada abre.

Esta especificação define um mecanismo de **guarda contra falhas sucessivas (Crash Guard)**: se o aplicativo sofrer 2 quedas em um intervalo menor ou igual a 60 segundos, o sistema assume que o aplicativo está instável e abre imediatamente o Windows Explorer tradicional na pasta solicitada. O usuário mantém acesso ininterrupto aos seus dados, o evento é logado, e na próxima abertura manual o aplicativo oferece diagnosticar e tentar novamente.

---

## 2. Histórias de Usuário

- **US1 (Resiliência Imediata de Pastas):** Como usuário, se o KyraFiles fechar repentinamente duas vezes seguidas ao tentar abrir pastas, quero que o Windows Explorer assuma a abertura automaticamente na pasta pedida para que meu trabalho não seja interrompido.
- **US2 (Tolerância a Falhas Isoladas):** Como usuário, se o KyraFiles fechar apenas uma única vez (ou quedas espaçadas por mais de 60 segundos), quero que o sistema não acione o fallback antecipado, permitindo que eu continue usando o KyraFiles normalmente.
- **US3 (Transparência e Diagnóstico):** Como usuário, ao abrir o KyraFiles manualmente após o fallback ter sido acionado, quero ver um aviso claro em português explicando o que aconteceu, oferecendo reativar o KyraFiles ou continuar com o Explorer temporariamente.
- **US4 (Rastreabilidade):** Como suporte/desenvolvedor, quero que os motivos das quedas (código de saída, data/hora e tempo de execução) sejam salvos em arquivo de log local para auditoria.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Cenário | Dado | Quando | Então |
| :--- | :--- | :--- | :--- | :--- |
| **AC-1** | **Duas quedas em 60 s acionam o Explorer** | O aplicativo fechou de forma anormal 1 vez há menos de 60 segundos | Uma nova tentativa de abertura de pasta ocorre e o aplicativo fecha anormalmente novamente em menos de 10s | O sistema abre imediatamente o Windows Explorer na pasta solicitada, sem deixar o usuário sem resposta. |
| **AC-2** | **Aviso na próxima abertura manual** | O fallback do Explorer foi ativado devido a duas quedas recentes | O usuário clica manualmente no atalho do KyraFiles para abri-lo | o KyraFiles abre e exibe um diálogo explicativo informando o ocorrido e oferecendo tentar novamente ou verificar o estado. |
| **AC-3** | **Queda isolada não aciona fallback** | Não houve quedas recentes (ou a última ocorreu há mais de 60s) | Ocorre uma queda isolada ao abrir uma pasta | O fallback não é ativado na primeira ocorrência; o estado de alerta de 1ª queda é registrado para monitorar os próximos 60s. |
| **AC-4** | **Janela de 60 segundos expira** | Uma queda ocorreu há 61 segundos ou mais | Uma nova queda ocorre agora | O contador não atinge o limite de 2 quedas em 60s; o evento anterior é descartado por tempo expirado. |
| **AC-5** | **Registro de log do motivo** | Ocorreu qualquer evento de queda ou acionamento de fallback | O sistema processa o estado de guarda | A data/hora exata, código de saída e tempo de execução são persistidos no log local (`crash_guard.log`). |
| **AC-6** | **Inicialização bem-sucedida limpa o alerta** | O aplicativo foi aberto após uma queda isolada anterior | O aplicativo permanece ativo e estável por mais de 10 segundos | O alerta de queda anterior é considerado superado e o histórico de quedas recentes é redefinido. |

---

## 4. O que conta como "Queda" (Critérios Objetivos)

1. Encerramento do processo com código de saída diferente de 0 dentro dos primeiros 10 segundos de vida.
2. Término abrupto do processo antes de completar a inicialização estável (ativação da janela principal).
3. Exceção fatal de processo disparada via `AppDomain.CurrentDomain.UnhandledException`.

