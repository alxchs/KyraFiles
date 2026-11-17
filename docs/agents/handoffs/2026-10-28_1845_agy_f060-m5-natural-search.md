# Handoff: F060 M5 — Busca por Frase em Linguagem Natural (Natural Search)

- **Data/Hora:** 2026-10-28 18:45 (horário local)
- **Agente:** AGY (Google DeepMind)
- **Módulo:** F060 M5 (Busca por Frase / Natural Search)
- **Branch:** `feature/f060-m5-natural-search`
- **Estado Geral:** T0 a T2 CONCLUÍDAS e REVISADAS; T3 (Prova de Tela) PRONTA PARA EXECUÇÃO. M5-T4 bloqueada até F042.

---

## 1. Estado Atual por Critério de Aceite (M5-AC-n)

| ID | Cenário | Estado Lógica / Backend | Estado Tela / UI | Evidência / Prova |
|---|---|---|---|---|
| **M5-AC-1** | Frase estruturada (ex.: "todos os arquivos ini salvos nos últimos 12 dias") gera chips de extensão e data e executa busca pós-filtrada | **CONFIRMED** | **NOT TESTED** | `NaturalSearchParser` (tabela de 40+ frases testadas) + `NaturalSearchPostFilter` desacoplado em background thread; testes verdes |
| **M5-AC-2** | Edição ou remoção de chip refiltra os resultados em memória sem redisparar varredura de disco | **CONFIRMED** | **NOT TESTED** | `RefilterSearchResultsAsync` reprocessa `_unfilteredSearchResults` em background thread de forma instantânea |
| **M5-AC-3** | Frases ambíguas ou sem critério reconhecível não inventam filtros e exibem aviso discreto | **CONFIRMED** | **NOT TESTED** | Parser mantém texto restante sem forçar filtros espúrios; `NaturalSearchUnrecognizedNotice` exibe aviso |
| **M5-AC-4** | Filtros de tamanho e data de criação ("maiores que 100 MB", "fotos de ontem") | **CONFIRMED** | **NOT TESTED** | Testes unitários comprovam conversão precisa de tamanhos e intervalos de datas relativos |
| **M5-AC-5** | Frases equivalentes em inglês ("all ini files saved in the last 12 days") geram filtros idênticos | **CONFIRMED** | **NOT TESTED** | Vocabulário bilíngue testado em `NaturalSearchParserTests` com datas determinísticas |
| **M5-AC-6** | Salvar busca favorita | **BLOQUEADA** | **BLOQUEADA** | Depende formalmente da F042 (Saved Searches) para persistência |
| **Comportamento com flag desligada** | Flag `EnableNaturalLanguageSearch` desativada por padrão mantém a busca 100% inalterada | **CONFIRMED** | **NOT TESTED** | Código em `BaseShellPage.SubmitSearch` desvia apenas se a flag estiver explicitamente ativa |

Comandos de verificação de testes:
```powershell
dotnet test tests\KyraFiles.App.Search.Index.Tests --filter NaturalSearch
```
Saída real: 19 testes aprovados de NaturalSearch, 0 falhas; 86 aprovados na suíte completa.

---

## 2. Roteiro de Execução da Prova de Tela (M5-T3)

A prova de tela deve ser executada após as 23:59, sem roubo de foco do usuário, via automação UI Automation / script PowerShell.

### 2.1 Pré-condições e Ambiente Isolado
1. Criar pasta temporária isolada de teste com arquivos com atributos controlados:
   `$lab = "$env:TEMP\agy\$(New-Guid)\f060_m5_lab"`
   `New-Item -ItemType Directory -Path $lab -Force`
   - Arquivo 1: `config.ini` (modificado há 5 dias, tamanho 10 KB)
   - Arquivo 2: `velho.ini` (modificado há 30 dias, tamanho 10 KB)
   - Arquivo 3: `relatorio.txt` (modificado há 2 dias, tamanho 50 KB)
   - Arquivo 4: `video_pesado.mp4` (tamanho 150 MB)
   - Arquivo 5: `foto_pequena.jpg` (tamanho 2 MB)

### 2.2 Passo a Passo dos Cenários de Tela

#### Cenário 1: Comportamento Neutro — Flag Desligada (Linha de Base)
1. Abrir instância de teste.
2. Conferir nas Configurações -> Geral que "Busca em linguagem natural" está **DESLIGADA** (padrão).
3. Na caixa de busca (Omnibar), digitar: `todos os arquivos ini salvos nos últimos 12 dias`.
4. Pressionar Enter.
5. **Verificação esperada:** NENHUM chip aparece. A busca se comporta como busca literal de texto padrão (procura arquivos contendo o termo).

#### Cenário 2: Ativação e Frase com Chips (M5-AC-1)
1. Abrir Configurações -> Geral -> Ligar "Busca em linguagem natural (experimental)".
2. Voltar à pasta `$lab`.
3. Na caixa de busca, digitar: `todos os arquivos ini salvos nos últimos 12 dias`.
4. Pressionar Enter.
5. **Verificação esperada:**
   - Barra horizontal de chips surge acima da lista de resultados (`SearchHeader`).
   - Aparecem dois chips:
     - `Extension: .ini`
     - `Modified: last 12 days`
   - Lista exibe exclusivamente o arquivo `config.ini` (pois `velho.ini` tem mais de 12 dias e `relatorio.txt` não é `.ini`).

#### Cenário 3: Edição e Remoção de Chips em Memória (M5-AC-2)
1. Clicar no botão "X" do chip `Modified: last 12 days` para removê-lo.
2. **Verificação esperada:** O chip de data desaparece. Sem recarregar o disco, a lista agora exibe `config.ini` e `velho.ini` (ambos são `.ini`).
3. Clicar no chip `Extension: .ini`, acionar flyout de edição e alterar para `.txt`.
4. **Verificação esperada:** A lista refiltra imediatamente exibindo apenas `relatorio.txt`.

#### Cenário 4: Frase Ambígua / Termos Não Compreendidos (M5-AC-3)
1. Na caixa de busca, digitar: `coisas bonitas e importantes`.
2. Pressionar Enter.
3. **Verificação esperada:** Nenhum filtro espúrio de extensão, data ou tamanho é criado. Um aviso discreto surge informando que os termos não foram mapeados para filtros estruturados, e o texto segue como busca regular.

#### Cenário 5: Filtro de Tamanho e Data em Inglês (M5-AC-4 e M5-AC-5)
1. Digitar: `files larger than 100 MB`.
2. Pressionar Enter.
3. **Verificação esperada:**
   - Surge o chip `Size: > 100 MB`.
   - Lista exibe exclusivamente `video_pesado.mp4`.

