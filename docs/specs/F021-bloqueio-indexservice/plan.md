# F021 — Desarmar IndexService e Bloqueio de Recursos do KyraFiles — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Arquitetura e Decisões de Design

1. **Abordagem de Bloqueio Não Invasiva:**
   - Em vez de remover de imediato os arquivos de código-fonte de `src/KyraFiles.App.Search.Index` (o que poderia quebrar referências da solução ou branches paralelas como `feature/f015-t17-correcoes`), adotamos um **bloqueio total nas bordas de entrada**:
     1. Desarma o arranque no `AppLifecycleHelper.cs` (elimina a chamada cega `serviceProvider.GetService<IndexedSearchProvider>()`).
     2. Desarma o disparador do construtor em `IndexedSearchProvider.cs` (não executa o `Task.Run(EnsureReadyAndConfiguredAsync)`).
     3. Define `IndexedSearchProvider.IsAvailable => false`, garantindo que `SearchProviderFactory.GetCurrentProvider()` e qualquer chamada downstream trate o provedor como indisponível.
     4. No `SearchProviderFactory.cs`, permite que `SearchEnginePreference` (Everything ou Native) assuma o comando da busca do KyraFiles.
     5. Em `VolumeIndexBenchmarkTests.cs`, marca com `[Ignore]` o teste de 5 milhões de nós.

2. **Garantia de Everything Operante:**
   - Com `IndexedSearchProvider.IsAvailable == false`, `SearchProviderFactory` direciona as consultas para `EverythingSearchProvider`.
   - Se o Everything estiver instalado e a CLI `es.exe` estiver presente ou fallback ativo, a busca opera normalmente sem recorrer ao indexador problemático.

3. **Geração do Exe Release Oficial e Instalação:**
   - Compilação em modo Release x64 do projeto principal (`KyraFiles.App.csproj`).
   - Atualização da instalação local em `%LOCALAPPDATA%\Programs\KyraFiles` usando `installer/Update-KyraFiles.ps1`.
   - Limpeza de qualquer pasta ou binário de debug temporário.

4. **Governança de Agentes (AGY + Claude):**
   - Criação do script `tools/perf/verify_no_index_service.ps1`.
   - Adição da regra de pré-requisito mandatório em `AGENTS.md` e `docs/agents/PLAYBOOK.md`.

---

## 2. Arquivos Impactados

| Arquivo | Propósito da Modificação |
|---|---|
| `src/KyraFiles.App/Helpers/Application/AppLifecycleHelper.cs` | Remover a inicialização em background do `IndexedSearchProvider`. |
| `src/KyraFiles.App/Services/Search/IndexedSearchProvider.cs` | Remover o disparo de processo no construtor e definir `IsAvailable => false`. |
| `src/KyraFiles.App/Services/Search/SearchProviderFactory.cs` | Respeitar a indisponibilidade do `index` e rotear para o motor configurado. |
| `tests/KyraFiles.App.Search.Index.Tests/VolumeIndexBenchmarkTests.cs` | Adicionar atributo `[Ignore]` ao teste destrutivo de 32 GB. |
| `tools/perf/verify_no_index_service.ps1` | Script automatizado de verificação de processos, CPU e limpeza. |
| `AGENTS.md` | Adicionar a regra obrigatória de validação de recursos do KyraFiles. |
| `docs/agents/PLAYBOOK.md` | Adicionar o teste na Definição de Pronto (DoD) de qualquer tarefa no KyraFiles. |

