# R-teclas-seguradas-medicao — setas, Home e End seguradas: medir antes de mexer

Status: APROVADO (Claude, 25/10/2026), **prioridade P1 de desempenho** (regra do Alexandre: desempenho passa na frente de feature). Pergunta original do Alexandre: o app consegue detectar tecla de seta/Home/End segurada e despachar só o que pinta, ignorando o resto até soltar? **Só mede; não muda código de produto.** Roda numa **janela de AWAY** (segura tecla de verdade; usa entrada).

## O que o código já faz (CONFIRMED por leitura em 25/10/2026, `DetailsLayoutPage.xaml.cs`)
- **Detecta:** repetição = segundo `KeyDown` da mesma tecla sem `KeyUp` entre eles (`heldNavigationKey`); `IsNavigationKeyHeld` confirma pelo estado real da tecla.
- **Descarta:** aceita a próxima repetição só depois de o quadro anterior ter sido apresentado (`navigationStepPending` + `CompositionTarget.Rendering`); as demais recebem `e.Handled = true`. Vale para ↑, ↓, PgUp e PgDn.
- **Adia:** `IsNavigationKeyHeld` suspende a carga do painel de prévia (F030) e os efeitos colaterais da seleção são coalescidos por temporizador (F026), esvaziados ao soltar ou ao chegar outra tecla.
## Lacunas (CONFIRMED pela contagem de ocorrências no código)
- **Ícones (`GridLayoutPage`) e Colunas (`ColumnLayoutPage`): nenhum** desses mecanismos (0 ocorrências de `heldNavigationKey`, `navigationStepPending`, `ExtendNavigationDeferral`).
- **Home e End** são tratados no Detalhes, mas **fora** da detecção de repetição (só ↑ ↓ PgUp PgDn entram): segurar Home ou End repete a seleção e a rolagem a cada repetição.
## Limites do que dá para fazer (por arquitetura, INFERRED)
Não dá para mandar o Windows ou o XAML ignorarem eventos: layout, medida e virtualização das linhas e eventos de acessibilidade continuam por quadro. Dá para descartar as **repetições da tecla** e adiar o **nosso** trabalho (já feito no Detalhes). Também **não se deve** ignorar outras teclas (Shift, Enter, Esc, Delete): o código libera os efeitos pendentes quando chega outra tecla, de propósito.

## Medição (um suspeito por vez, mesmos scripts antes e depois — METODO §11)
Pasta de teste com 1.600 itens (a da F029: `C:\FilesUXLab\perf\1600`, **somente leitura**), instância própria do KyraFiles com PID guardado.
Matriz: layouts **Detalhes, Ícones, Colunas** × teclas **↓, ↑, Home, End** segurados por **5 s**, **3 rodadas** cada, mais o **Explorer do Windows** (`CabinetWClass`) e a **v4.3.5** como referência.
Métricas (por rodada, em CSV): linhas alcançadas em 5 s; quadros apresentados e intervalo entre quadros (média e p95); uso de CPU e memória do processo; e se a seleção termina **no item esperado** ao soltar (sem "rabo" de passos depois do `KeyUp`).
Entrega: `docs/agents/evidence/F058-teclas-seguradas/medicao.csv` e `relatorio.md` **gerado do CSV**, com a tabela comparativa e a leitura em uma frase por layout: "igual ao Explorer", "abaixo" ou "acima".
## Pré-requisito
O helper precisa de **tecla segurada** com a trava de foco repetida a cada intervalo. Antes da medição, a AGY acrescenta `Send-GuardedKeyHold` ao `Send-GuardedInput.ps1` (assertiva de foco a cada 50 ms enquanto a tecla está pressionada; solta e aborta se o foco sair; `try/finally` para sempre soltar a tecla), com teste próprio em janela própria. Sem modificadores.
## Decisão depois da medição (do Claude, com o Alexandre)
Se Ícones/Colunas ficarem **abaixo** do Detalhes, ou Home/End mostrarem "rabo" ou lentidão: nasce a **F058 (teclas seguradas em todos os layouts)**, com spec, plan e tasks, portando o mesmo mecanismo do Detalhes para os outros layouts e incluindo Home/End. Se estiver tudo igual ao Explorer, **não se mexe no código**.
