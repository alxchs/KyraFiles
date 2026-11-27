# F095 — Protocolo único `kyrafiles:` e prévia de texto confiável — SPEC (retroativa)

ID estável: SPEC-0079 (alias F095).
Status: **APROVADA retroativamente** (Claude, 26/11/2026). A entrega entrou na `main` antes da spec (`f191572b0`, 24-25/11; correções `a6fc46b0b`, 26/11). Esta spec registra o comportamento que o código já tem e que foi auditado; não acrescenta nada.

## 1. Por quê
- O app atendia a mais de um nome de protocolo (o atual e os herdados), e cada ponto de entrada interpretava o endereço do seu jeito. Um endereço com formato inesperado podia abrir o lugar errado ou nada.
- Uma instalação nova deixava para trás o pacote de desenvolvimento antigo, e o Windows ficava com dois registros do app.
- A prévia de texto mostrava acentos trocados em arquivos antigos (código-fonte salvo no padrão do Windows, não em UTF-8).

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero que qualquer atalho, Win+E ou outra janela que abra o KyraFiles por endereço caia sempre no mesmo lugar previsível.
- **US2 (P1)** Como usuário, quero que instalar ou desinstalar não deixe uma segunda cópia antiga registrada no Windows.
- **US3 (P2)** Como usuário, quero ver acentos corretos na prévia de arquivos de texto antigos.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | app instalado | abro `kyrafiles:` (Win+E) | abre a tela inicial |
| AC-2 | app instalado | abro `kyrafiles:?folder=C%3A%5CWindows` ou `kyrafiles:C%3A%5CUsers` | abre a pasta indicada |
| AC-3 | app instalado | abro `kyrafiles:?tab=<pasta>&dx=..&dy=..` | abre a aba na pasta e posição |
| AC-4 | outro programa repassa comando por `kyrafiles:?cmd=...` | o app recebe | executa o comando repassado, interpretado pelo mesmo leitor dos outros casos |
| AC-5 | endereço vazio, de outro esquema ou malformado | o app recebe | abre a tela inicial, sem erro |
| AC-6 | máquina com o pacote antigo de desenvolvimento | instalo ou desinstalo | o pacote antigo some e fica exatamente 1 registro, com a versão exata do instalador |
| AC-7 | arquivo de texto em UTF-8 com ou sem marca, ou no padrão antigo do Windows | abro a prévia | os acentos aparecem certos |
| AC-8 | arquivo lido de origem que não permite voltar ao início | abro a prévia | o texto começa do primeiro byte (nada se perde) |
| AC-9 | geração de pacote de canal (preview/stable) pela automação | troca o nome do protocolo | só os endereços de protocolo mudam; nomes de código continuam intactos |

## 4. Requisitos
- **FR-1** Um único leitor de endereço para todos os pontos de entrada (AC-1 a AC-5).
- **FR-2** Um único nome de protocolo definido em um só lugar (AC-1, AC-9).
- **FR-3** O instalador remove o pacote antigo e confere registro único com versão exata (AC-6).
- **FR-4** A prévia detecta a codificação por marca ou amostra e não perde a amostra (AC-7, AC-8).
- **NFR-1** Sem novo aviso de compilação e sem regressão de desempenho.

## 5. Fora de escopo
- Arquivo no padrão antigo do Windows cujos primeiros 4 KB são só ASCII pode ser lido como UTF-8 (limite aceito na auditoria de 25/11).
