# F013 — F3 abre o Agent Ransack na pasta atual — SPEC

Status: **APROVADA** (Claude, 09/10/2026, a pedido do Alexandre).
Metodologia: Spec-Driven Development (`METODO.md` §6).

## 1. Por quê

No Explorer do Alexandre, o F3 abre o Agent Ransack já apontando para a pasta em que ele está (o Agent Ransack se registrou nesse atalho). Ele usa isso no dia a dia e quer o mesmo no Files Custom (pedido de 09/10/2026), já que a busca integrada ao Agent Ransack (F011) ainda tem defeitos abertos na revisão do G1.

## 2. Histórias de usuário

- **US1 (P1):** Como usuário, numa pasta do Files, aperto F3 e o Agent Ransack abre com "Procurar em" igual à pasta atual, pronto para eu digitar o que procuro.
- **US2 (P2):** Como usuário sem o Agent Ransack instalado, o F3 continua abrindo a busca do próprio Files, como antes.

## 3. Cenários de aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| **AC-1** | Agent Ransack instalado; Files numa pasta de disco (inclusive com espaço e acento no caminho) | Aperto F3 | O Agent Ransack abre com essa pasta em "Procurar em". O Files não entra no modo de busca. |
| **AC-2** | Agent Ransack não instalado | Aperto F3 | A caixa de busca do Files abre, como antes desta feature. |
| **AC-3** | Agent Ransack instalado; Files numa localização que não é pasta de disco (Início, Lixeira, Este Computador) | Aperto F3 | A caixa de busca do Files abre (o Agent Ransack não sabe procurar ali). |
| **AC-4** | Qualquer situação | Aperto Ctrl+F | A caixa de busca do Files abre, como antes. |
| **AC-5** | Files em inglês ou em português | Abro a paleta de comandos ou Configurações > Ações | O comando aparece com nome no idioma do app. |

## 4. Requisitos

- **FR-001:** F3 DEVE abrir o Agent Ransack com a pasta atual do painel ativo como local de busca, quando ele estiver instalado e a pasta for de disco ou rede.
- **FR-002:** Nos outros casos, F3 DEVE abrir a busca do Files.
- **FR-003:** Ctrl+F DEVE continuar abrindo a busca do Files.
- **FR-004:** O atalho DEVE poder ser trocado pelo usuário como os demais comandos do Files.
- **NFR-001:** Abrir o Agent Ransack não pode travar a interface do Files.

## 5. Casos de borda

1. Caminho com espaço, acento ou muito longo: repassado entre aspas, sem quebrar.
2. Agent Ransack instalado em `Program Files (x86)`.
3. Dois painéis abertos: vale a pasta do painel ativo.

## 6. Fora de escopo

- Integração da busca do Files com o Agent Ransack (F011; defeitos na revisão do G1).
- Passar texto de busca ou arquivos selecionados para o Agent Ransack.

## 7. Histórico

- **09/10/2026 (Claude):** escrita e aprovada a partir do pedido do Alexandre.
