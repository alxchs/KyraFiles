# Auditoria da v4.3.33.0 (main @ 9dabbac09), Claude, 25/11/2026

Resposta ao pedido formal de auditoria (commits `f191572b0`, `e811323b3`, `c45367720`, `9dabbac09`).
Branch desta auditoria: `feature/auditoria-v4.3.33` (só docs).

## 1. Entregas de código

### 9dabbac09 — F090, corte em escala < 100%: APROVADO

- A margem negativa em `UiScaleService.ApplyLayoutCompensation` está correta: com `Width`/`Height` explícitos maiores que a janela, o slot de layout fica do tamanho da janela; a margem direita/inferior negativa devolve exatamente o excesso, e o `LayoutClip` deixa de cortar antes do `Scale`. No ramo identidade a margem volta a 0. Vale para as 5 janelas que chamam `ApplyScale` (principal, propriedades, hex, configurações, barra de ferramentas).
- **Reverificação independente (OBSERVED):** instância do Alexandre já aberta, `AppUiScalePercent = 90`, pacote `KyraFiles 4.3.33.0` registrado. Sem abrir janela nova e sem foco: `PrintWindow` + UI Automation somente leitura. 233 elementos visíveis; borda direita/inferior dos elementos vs janela: `ratioW=1,005`, `ratioH=0,992`. A captura mostra barra de status e o botão do painel de detalhes visíveis, sem corte. (A captura não vai para o repositório: tem nomes de arquivos e conta pessoal.)
- O script `tools/perf/f090_scale_test.ps1` **não foi rodado**: ele mata qualquer `KyraFiles` aberto (`Stop-Process -Force`) e havia uma sessão do Alexandre em uso. Ver pendência P3.

### e811323b3 — instalador, `TrimEnd` no Pascal: APROVADO

- `RemoveBackslashUnlessRoot(ExpandConstant('{app}'))` é a função certa do Inno Setup; a comparação com `InstallLocation.TrimEnd('\')` do lado PowerShell fica simétrica.
- Pendência menor: `$pkgs[0].Version.StartsWith('4.3.33')` também aceitaria `4.3.330.x`. Trocar por igualdade com `'{#MyAppVersion}.0'`.

### f191572b0 — F095: APROVADO COM 1 BLOQUEANTE (P1)

- **P1 (bloqueante para qualquer CD): `.github/scripts/Configure-AppxManifest.ps1`.** As linhas novas `-replace "kyrafiles-dev", "kyrafiles-preview" -replace "kyrafiles", "kyrafiles-preview"` estão erradas duas vezes: o `-replace` do PowerShell ignora caixa, então todo `KyraFiles` dos `.cs`/`.cpp` vira `kyrafiles-preview` (`namespace kyrafiles-preview.App`, que não compila); e o segundo replace pega o resultado do primeiro (`kyrafiles-preview-preview`). Reproduzido:
  `"namespace KyraFiles.App; uri kyrafiles-dev:" -replace ... -replace ...` → `namespace kyrafiles-preview.App; uri kyrafiles-preview-preview:`.
  Os 4 workflows `cd-*.yml` chamam esse script. Hoje o lançamento sai por `tools/Publish-Release.ps1` (não passa por ele), por isso a v4.3.33 não foi afetada; mas o primeiro uso de CD quebraria o build inteiro. Correção: trocar só o literal de protocolo, com caixa exata, por exemplo `-creplace '"kyrafiles:', '"kyrafiles-preview:'` e `-creplace 'CustomProtocolScheme = "kyrafiles"', ...`, ou centralizar o esquema numa única constante e trocar só ela.
- `ProtocolUriParser`: bom. Sem `Substring` com tamanho fixo, URI vazia vira `Home`. Observações não bloqueantes:
  - `Program.cs` (`cmdProtocolArgs`) ainda faz o parse antigo de `?cmd=`; deveria usar `ProtocolUriParser.Parse` para não haver dois parsers.
  - `MainWindow.xaml.cs` ficou com a condição `EndsWith("kyrafiles")` duplicada (literal e constante) e já tinha o `Equals(... "KyraFiles.exe")` duplicado.
  - O esquema existe duas vezes (`Constants.Protocol.CustomProtocolScheme` no App e `ProtocolUriParser.ProtocolScheme` no Shared). O App deveria usar a do Shared.
- Codificação de prévia: `DetectEncoding` correto (BOMs, UTF-8 válido com tolerância de sequência cortada no fim do buffer, senão 1252; `CodePagesEncodingProvider` está registrado em `Program.cs:97`, então o 1252 é real, não Latin1). Limite conhecido: arquivo 1252 cujos primeiros 4 KB são só ASCII é detectado como UTF-8 e mostra caractere trocado mais à frente. Aceitável.
- `BaseStorageFile.ReadTextAsync`: se o stream não permitir `Seek`, os primeiros 4 KB lidos para a amostra se perdem (o texto começa no byte 4096). Hoje todos os `OpenReadAsync` devolvem stream com seek (INFERRED, não testado com FTP/zip). Corrigir lendo a amostra num buffer e encadeando (`MemoryStream` da amostra + resto) quando `!CanSeek`.
- Instalador: purga de `FilesDev` volta na instalação e na desinstalação, e o registro é validado (1 pacote, versão igual). Atende à lição de 23/11.

## 2. Planos e tarefas das 5 SPECs impedidas (c45367720)

Conferi no código todos os arquivos e símbolos citados como CONFIRMED (15 arquivos; `CssPreviewRenderer`, `HardLinkResolver`, `OnBatchReceived`, `IsInstantStreaming`, `GetPane`, `Keys.Oem3` etc.): todos existem. Os planos respeitam as decisões de `spec.md §8`.

| SPEC | Decisão | Ajuste principal (seção 6 do `plan.md`) |
|---|---|---|
| F054 prévia técnica (5) | APROVADO com ajustes | `ColorCode` fora da UI thread; árvore/CSV respeitam limite e cancelamento; reaproveitar `DetectEncoding` |
| F039 uso de disco (8) | APROVADO com ajustes | Definir projeto do scanner; nunca acordar o IndexService; NFR-001 ganha número medido |
| F041 comparar pastas (8) | APROVADO com ajustes | Política de sobrescrita explícita; nada apagado; hash em blocos |
| F046 busca na rede (8) | APROVADO com ajustes | `NetShareEnum` via CsWin32 + `DirectPInvoke` de `NETAPI32.dll`; timeout cobre a enumeração inicial |
| F050 terminal (8) | **Só a T0 aprovada** | T0 compara WebView2+xterm.js vs renderizador nativo com números; T1–T3 esperam o Alexandre |

Ordem de execução (METODO §26): F054 → F039 → F041 → F046 → F050-T0. F054 primeiro por ser a menor e de maior valor visual; F046-T0 pode correr em paralelo por ser só medição.

## 3. Homologação das 13 SPECs em `in_validation`: NENHUMA homologada

Motivo comum: a evidência registrada no `production-control.json` é, na maioria, "commit citando a spec no assunto; não reconfirmado", sem estado por `AC-n` com comando e saída. Pela regra de evidência (METODO §7 e item 199 do `INSTRUCTIONS_OPERARY.md`), isso não sustenta `CONFIRMED`.

| SPEC | Situação | O que falta para homologar |
|---|---|---|
| F017 Win+E lançador (5) | tasks 0 abertas; evidência só por commit | Prova por AC na v4.3.33 (Win+E com e sem app aberto, reversão na desinstalação) |
| F020 aba única (2) | idem | Prova por AC (abrir mesmo endereço duas vezes; posição de Configurações) |
| F022 startup/instalador (3) | idem | Prova por AC no instalador 4.3.33 |
| F035 segunda instância (5) | **AC-5 reprova**; AC-2/AC-4 NOT TESTED; T1, T3, T4 abertas | Não homologável. Corrigir AC-5 ou mudar a spec com decisão do Alexandre |
| F060 Onda A (13) | 78/78 unitários; provas de tela M1-T5, M5-T3 abertas | Provas de tela de M1, M2, M5 |
| F061 Win+E sempre pronto (5) | evidência só por commit | Prova por AC |
| F062 diagnóstico memória (5) | **tasks.md diz T1–T3 abertas, mas o controle diz código pronto** | Acertar o `tasks.md` com o código e capturar a tela Sobre |
| F063 memória (8) | script pronto, 10 rodadas (C7) e Sobre (C8) não executadas | Rodar `measure_f063_memory_restore.ps1` |
| F065 busca nome/caminho (5) | evidência só por commit | Prova por AC |
| F070 GitHub OAuth (3) | AC-1 só validado por HTTP 200 | Login de ponta a ponta (depende do Alexandre digitar o código) |
| F086 instância única (3) | AC-2 a AC-6 NOT TESTED | `measure_f086_instances.ps1` ainda "a fazer" |
| F089 fontes (3) | **spec ainda RASCUNHO**; T4 aberta | Aprovar a spec (Alexandre) e rodar T4 |
| F090 escala (5) | 90% OBSERVED sem corte (acima); T0, T4, T5 abertas | 80/125/150%, janelas secundárias, Ctrl+/Ctrl-, NFR-1 |

A F090 avançou (AC de corte em 90% OBSERVED), mas não fecha: T4 (popups e atalhos) e T5 (todas as escalas + desempenho) seguem abertas.

## 4. Pendências

- **P1** Corrigir `Configure-AppxManifest.ps1` (bloqueante de CD). Tarefa para a AGY.
- **P2** Corrigir `BaseStorageFile.ReadTextAsync` para stream sem seek; unificar parser `?cmd=` do `Program.cs`; tirar a condição duplicada e a constante duplicada do esquema; comparar versão exata no instalador. Pacote único, pequeno.
- **P3** `f090_scale_test.ps1` não pode matar instância do usuário: abortar se já houver `KyraFiles` aberto antes do teste (ou usar só a instância que ele próprio abriu), e medir corte por UIA (`ratioW`/`ratioH`), que é o que a F090 prova.
- **P4** Bateria de homologação consolidada (lote), por AC, das SPECs acima, na ordem: F090, F086, F063, F062, F060, F017/F061/F020/F022/F065. F035, F070 e F089 dependem do Alexandre.
- **P5** Árvore local: 3 CSVs de `docs/agents/evidence/F036/` alterados pela bateria. Não são desta auditoria; não foram commitados aqui.
