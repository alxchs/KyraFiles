# F021 — Desarmar IndexService e Bloqueio de Recursos do Files Custom — SPEC

Estado: **APROVADA PELO ALEXANDRE** (16/10/2026).
Origem: Diagnóstico "Lenovo em Repouso" gerado pela Claude em 16/10/2026 e ordem expressa do usuário:
*"Gere um exe bloqueando essa funcionalidade enquanto corrige a causa raiz para não topar a máquina ou qualquer outra coisa que libere os recursos. Ao aplicar teste isso e a partir de agora esse teste precisa virar pré-requisito para agy e claude darem como feita qualquer tarefa no projeto Files. Para evitar confusão, NUNCA deixe cópias debug ou em qualquer pasta auxiliar depois de desenvolver e testar. Somente a release no local instalado deve existir. Crie SPECS adicionais para essas correções se for necessário."*

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Contexto e Motivação

1. **Incidente de Esgotamento de Recursos (11/10/2026 a 16/10/2026):**
   - O processo `Files.App.IndexService.exe` foi medido em repouso consumindo continuamente **1.150 MB de memória privada** e **23,6% a 24,3% de CPU** (quase 3 das 12 threads do processador i7 em loop constante), acumulando mais de 25.000 s de CPU sem necessidade.
   - A suíte de testes `Files.App.Search.Index.Tests.exe` atingiu **32 GB de RAM** em 11/10/2026 devido ao teste sintético `Benchmark_FiveMillionEntries_MemoryAndQueryLatency`, provocando saturação da memória virtual (36 GB de swap) e congelamentos com desligamento forçado da máquina.
   - O processo supervisor `IndexServiceProcessManager` embutido no `Files.exe` ressuscitava o `Files.App.IndexService.exe` sempre que este era encerrado.

2. **Decisão:**
   - Desarmar e bloquear de forma total e imediata a inicialização automática, a supervisão em loop e a execução do `Files.App.IndexService.exe` dentro do Files Custom.
   - Garantir que o mecanismo de busca do Files Custom continue funcionando com o motor externo nativo e Everything (que já está ativo e indexado na máquina).
   - Estabelecer um teste mandatório de pré-requisito no repositório para AGY e Claude: nenhuma tarefa pode ser concluída sem a comprovação de que o `Files.App.IndexService.exe` está 0% ativo e que o Files opera sem cópias de debug ou pastas auxiliares soltas.

---

## 2. Histórias de Usuário

- **US1 (P1 - Bloqueio de Processo e CPU):** Como usuário, ao abrir e utilizar o Files Custom, não quero que nenhum processo `Files.App.IndexService.exe` seja disparado ou mantido em segundo plano, garantindo que o repouso do Files permaneça com 0% de CPU e sem consumo de gigabytes de RAM.
- **US2 (P1 - Continuidade da Busca com Everything):** Como usuário, ao realizar buscas dentro do Files Custom, quero que a pesquisa ocorra instantaneamente via Everything (quando disponível) ou busca nativa, sem falhas e sem depender de indexador próprio instável.
- **US3 (P1 - Proteção de Testes Contra 32 GB):** Como desenvolvedor/agente, a execução da suíte de testes não deve alocar massas de dados sintéticos desproporcionais que excedam a memória física e travem o sistema operacional.
- **US4 (P1 - Portão de Qualidade Mandatório para IAs):** Como mantenedor do projeto, quero que qualquer alteração feita por Claude ou AGY só seja dada como pronta após passar por verificação formal de que o `IndexService` está inativo e os recursos do sistema estão calmos.
- **US5 (P2 - Higiene Rigorosa de Binários):** Como mantenedor, não quero binários ou diretórios de build de depuração/auxiliares espalhados após os testes; apenas a versão Release instalada em `%LOCALAPPDATA%\Programs\FilesCustom` deve permanecer no ambiente.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | O Files Custom instalado é iniciado | O app completa a inicialização e fica ocioso por 10 segundos | `Get-Process Files.App.IndexService` retorna vazio (processo NÃO existe); CPU do Files fica em 0%. |
| **AC-2** | Um processo `Files.App.IndexService.exe` antigo ou espúrio é finalizado | O Files Custom está aberto | O `IndexServiceProcessManager` NÃO tenta ressuscitá-lo em loop. |
| **AC-3** | O usuário abre o Files Custom e digita um termo na caixa de busca | A busca é executada | A pesquisa utiliza o `EverythingSearchProvider` (ou `NativeFilesSearchProvider` como fallback seguro), retornando resultados sem erro e sem acionar `IndexService`. |
| **AC-4** | A suíte de testes do repositório é executada (`dotnet test` ou via script) | Todos os testes rodam | Nenhum teste aloca 5 milhões de itens ou consome mais que 500 MB de RAM. O teste de benchmark descontrolado é ignorado/bloqueado. |
| **AC-5** | O script de validação de qualidade `tools/perf/verify_no_index_service.ps1` é executado | A verificação avalia processos, CPU, RAM e integridade | O script retorna ExitCode 0 comprovando ausência do IndexService, CPU < 1% e ausência de pastas debug auxiliares. |
| **AC-6** | AGY ou Claude finalizam uma tarefa no FilesApp | Antes de marcar como pronta | A execução de `verify_no_index_service.ps1` é obrigatória e exigida nas regras de `AGENTS.md` e `docs/agents/PLAYBOOK.md`. |

---

## 4. Requisitos

### 4.1. Requisitos Funcionais (FR)
- **FR-001 (Desarmar Warmup no Startup):** Em `src/Files.App/Helpers/Application/AppLifecycleHelper.cs`, remover a chamada em background que carrega `IndexedSearchProvider` na inicialização da aplicação.
- **FR-002 (Bloquear Instanciação e Supervisão do Processo):** Em `src/Files.App/Services/Search/IndexedSearchProvider.cs`, desarmar a chamada `EnsureReadyAndConfiguredAsync` no construtor e definir `IsAvailable => false` enquanto durar o bloqueio temporário.
- **FR-003 (Roteamento Seguro de Busca):** Em `src/Files.App/Services/Search/SearchProviderFactory.cs`, não forçar incondicionalmente o provedor "index" quando ele estiver desabilitado; rotear para `Everything` (quando configurado/disponível) ou `Native`.
- **FR-004 (Desativação de Teste Destrutivo):** Em `tests/Files.App.Search.Index.Tests/VolumeIndexBenchmarkTests.cs`, marcar o teste `Benchmark_FiveMillionEntries_MemoryAndQueryLatency` com `[Ignore]` para proteger o ambiente de desenvolvimento contra estouros de memória de 32 GB.
- **FR-005 (Automação de Verificação de Qualidade):** Criar `tools/perf/verify_no_index_service.ps1` para atestar automaticamente os critérios de aceite AC-1 a AC-5.
- **FR-006 (Atualização de Governança de Agentes):** Atualizar `AGENTS.md` e `docs/agents/PLAYBOOK.md` adicionando a regra mandatória de que nenhuma tarefa do Files é considerada concluída sem o aval do teste de contenção de recursos.

### 4.2. Requisitos Não Funcionais (NFR)
- **NFR-001 (CPU em Repouso):** Consumo de CPU decorrente do serviço de busca em repouso deve ser estritamente 0%.
- **NFR-002 (Memória do Serviço):** O processo `Files.App.IndexService.exe` deve consumir 0 MB (não deve existir na tabela de processos).
- **NFR-003 (Limpeza de Ambiente):** Nenhuma pasta de staging temporário, build debug avulso ou DLL desnecessária deve persistir fora do diretório de instalação oficial `%LOCALAPPDATA%\Programs\FilesCustom`.

