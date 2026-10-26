# Revisão do Claude — F035 T2 e T3 (26/10/2026)

Estado: **T2 aceita; T3 PARCIAL**. Não é prova suficiente para publicar a correção.

## T2 (`7fa9d7eb8`): coerente
`Program.cs`: `pool.Release()` protegido contra `SemaphoreFullException`, redirecionamento protegido, `Dispose` garantido. Comentários `// Files Custom:` presentes. Observação: o `catch` genérico em volta do redirecionamento deixa a instância seguir como principal se a principal não responder; isso pode criar uma segunda principal em falha transitória (melhor do que perder o pedido, mas fica registrado como risco).

## T3: o que o `resultado.csv` mostra e o que NÃO prova
- Controle negativo: crash reproduzido em **10 de 20** rodadas no código sem correção (30 eventos 1026). Válido.
- **AC-1: 20/20 sem evento 1026.** O teste só **conta eventos 1026**; **não verifica que as 5 pastas viraram abas**, que é o que o AC-1 pede ("as 5 pastas aparecem e nenhum processo termina"). Fica **PASS parcial: ausência de morte provada, entrega das pastas NÃO provada.**
- AC-2: o CSV diz "4 abas" para 2 aberturas; o esperado é "uma janela com as 2 pastas". Resultado não explicado: **precisa de conferência**.
- AC-3: PASS (processo de diálogo isolado).
- **AC-5: FAIL** ("janela visível: False, 0 eventos 1026"). **Causa não determinada.** Hipóteses: (a) o teste não acha a janela (busca por classe `WinUIDesktopWin32WindowClass` em janelas de topo); (b) a janela estacionada realmente não volta ao primeiro plano; (c) comportamento anterior à F035. **O AC-5 nunca foi rodado no build SEM correção**: falta o controle que diria se o defeito já existia. Registrado como DISCOVERY.
- **AC-4: não testado** (T5, não pedido).

## Limitações do método
- A bateria foi executada várias vezes seguidas e o CSV foi sobrescrito: a evidência é **a última rodada**.
- O script foi ampliado (157 linhas alteradas) durante a T3 para achar a janela; commitado junto, pois gerou os resultados.
- `tools/perf/Send-GuardedInput.ps1` foi **copiado** para este branch na T1 (o original vive em `feature/test-input-guard`): quando os dois forem para a `main`, o segundo `rebase` precisa tratar o arquivo duplicado.
- O app instalado foi desregistrado/registrado para o layout do worktree durante os testes; o Claude restaurou o registro para `...\Programs\FilesCustom` (dados preservados) em 26/10/2026.

## Para fechar a T3 (próximo passo)
1. AC-1: o teste deve **contar as abas** (UI Automation) e conferir as 5 pastas.
2. AC-2: explicar o número de abas e conferir as 2 pastas.
3. AC-5: rodar no **build sem correção** (controle) e no corrigido; se falhar nos dois, é defeito anterior e/ou do teste: abrir DISCOVERY separado (não bloqueia a correção do semáforo).
4. Não sobrescrever o CSV: um arquivo por execução, com data e hora.
