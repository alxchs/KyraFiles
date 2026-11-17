# Brief de reprodução — dois crashes do KyraFiles 4.3.7 (SOMENTE REPRODUZIR E DOCUMENTAR; NÃO ALTERE `src/`)

Criado pelo Claude em 26/10/2026. Prioridade P0/P1 (crash perde trabalho). **Janela de AWAY: 12:30 a 13:00 de 26/10/2026 (30 minutos, orçamento duro).** `ALEXANDRE_AWAY=1`; o KyraFiles instalado do Alexandre estará fechado; entrada **só** por `tools/perf/Send-GuardedInput.ps1` (já na `main`, commit `601d63a89`). Ler antes: `docs/agents/SITUACAO.md`, lições 12 a 20 de `INSTRUCTIONS_OPERARY.md`, regra R13.

## Crash A — queda nativa com a prévia aberta (relatado pelo Alexandre, 26/10 08:11:45)
- **Cenário relatado:** modo com o **painel de prévia aberto**, navegando com ↑ e ↓ em `D:\DriveD\Downloads`; tecla ↓ SEGURADA continuamente, subindo e depois descendo (relato corrigido pelo Alexandre em 26/10: o vídeo não tocou); a seleção passou **por um mesmo vídeo por poucos décimos de segundo** e o app caiu.
- **Evidência (OBSERVED):** evento 1000 às 08:11:45: `Faulting module: Microsoft.UI.Input.dll` (WindowsAppRuntime 2.5.1.0), **`0xc0000409`** (falha rápida nativa), offset `0x9f853`; **sem** evento 1026 (não é exceção .NET). Dump de 48 MB: `%LOCALAPPDATA%\CrashDumps\KyraFiles.exe.15768.dmp` (**não apagar**). Relatório WER `AppCrash_FilesDev_4.3.7.0_...` em `C:\ProgramData\Microsoft\Windows\WER\ReportArchive`.
- **Hipóteses (INFERRED, nenhuma confirmada):** (a) o painel de prévia cria e destrói o player de mídia a cada item e a destruição concorrente com entrada de teclado derruba o componente de entrada; (b) a F030 (prévia suspensa na tecla segurada) não cobre toque rápido repetido; (c) defeito do WindowsAppSDK sem relação com o KyraFiles.
- **O que fazer:** montar uma pasta de teste em `%TEMP%\agy\<GUID>\...` com **vídeos curtos gerados** (ou copiados **somente para leitura** de uma origem existente; nunca arquivos do Alexandre alterados) e arquivos de outros tipos misturados; abrir o painel de prévia; SEGURAR ↑ e depois ↓ pelo helper por 1 a 3 s (repetição automática real), soltar, inverter a direção e repetir; variar também toques isolados de 30 a 300 ms; 20 rodadas por configuração; **contar eventos 1000/1026 do `KyraFiles.exe` no log do Windows** (independente do script). **Controle:** repetir sem o painel de prévia aberto. Registrar taxa de queda por configuração.

## Crash B — `GridLayoutPage` (26/10 00:59:04, durante a bateria da F035)
- **Evidência (OBSERVED):** evento 1026 gerenciado; pilha: `ResourceDictionary.ContainsKey` → `GridLayoutPage_obj61_Bindings.LookupConverter` → `Update_FileTagsUI` → `PropertyChanged`. Ocorreu enquanto janelas fechavam ou reabriam.
- **O que fazer:** só se sobrar tempo depois do crash A: abrir uma pasta em modo **Grade** com itens que tenham etiquetas, fechar e reabrir a janela em sequência (10 a 20 rodadas) e contar os 1026 com essa pilha. Sem sobra de tempo, não faça; registre `NOT TESTED`.

## Regras da janela
1. **Orçamento 28 minutos; pare no tempo.** Uma execução por configuração, **um CSV por execução com data e hora**. Não repita para "dar resultado".
2. **Não altere `src/`.** Se reproduzir, capture a pilha completa (log de eventos) e, se possível, registre um dump novo com o caminho; **não** tente corrigir.
3. Instância própria do app de teste (layout do worktree, restore da solução inteira); **ao fim** restaurar o registro de `KyraFiles` para `...\Programs\KyraFiles` (`Remove-AppxPackage -PreserveApplicationData` + `Add-AppxPackage -Register`), fechar instâncias de teste, apagar `bin`/`obj` e `%TEMP%\agy`.
4. Handoff com as **três partes da R13** (o que deu certo, o que deu errado, o ponto do projeto), relido a partir da fonte; `NOT TESTED` onde não deu tempo.
5. **Nunca** apagar `KyraFiles.exe.15768.dmp` nem arquivos de `D:\DriveD\Downloads`.

## Critério de aceite
Para cada crash: **reproduzido** (taxa, cenário, pilha) ou **não reproduzido** (o que foi tentado e quantas rodadas), com `resultado-AAAAMMDD-HHMM.csv` e a contagem do log do Windows. Decisão de correção fica com o Claude.

