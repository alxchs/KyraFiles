# F053 — Codificação e fim de linha — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Misturar CRLF/LF e UTF-8 com/sem BOM é fonte comum de erro (o ambiente já exige CRLF nos arquivos do repositório). Hoje só dá para descobrir abrindo o arquivo num editor.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero ver colunas "Codificação" e "Fim de linha" para arquivos de texto.
- **US2 (P2)** Como desenvolvedor, quero converter os selecionados em lote (CRLF↔LF, com/sem BOM, UTF-8).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta com arquivos de texto | ligo as colunas | cada arquivo mostra, p.ex., "UTF-8 BOM / CRLF"; binários mostram vazio |
| AC-2 | arquivo com fim de linha misto | olho a coluna | mostra "Misto" |
| AC-3 | 5 arquivos LF selecionados | "Converter para CRLF" | os 5 mudam; o conteúdo textual não muda; a data de modificação muda como esperado |
| AC-4 | arquivo em uso | converto | é pulado e listado; os demais seguem |
| AC-5 | conversão feita | `Ctrl+Z` | os arquivos voltam byte a byte |
| AC-6 | pasta com 10 mil arquivos | rolo a lista | a lista continua fluida; colunas preenchem ao longo do tempo sem travar |
| AC-7 | arquivo que não é texto (imagem) | converto | recusa e explica |

## 4. Requisitos
- **FR-001** Detectar por leitura do começo do arquivo (limite configurável), com cache.
- **FR-002** Conversão atômica por arquivo (escrever em temporário e trocar), com desfazer.
- **FR-003** Tipos a considerar configuráveis (por extensão).
- **NFR-001** Ler no máximo os primeiros KB de cada arquivo, em segundo plano (medido).

## 5. Casos de borda
- Arquivo vazio; UTF-16; Latin-1 sem marca; arquivo com acentos mal codificados; somente leitura.

## 6. Fora de escopo
- Converter entre codificações com perda de caracteres sem aviso; editar o conteúdo.

## 7. Critérios de sucesso
- Achar de relance quais arquivos de uma pasta estão em LF e converter todos em menos de 30 s.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: converter também a codificação (Latin-1→UTF-8), ou só fim de linha e BOM? Recomendação: só fim de linha e BOM nesta versão.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
