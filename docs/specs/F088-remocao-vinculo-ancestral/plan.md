# F088 - Remoção do vínculo visível com o ancestral (KyraFiles Community) - PLANO (RASCUNHO, não commitado)

Autora: Claude, 03/11/2026. Pedido do Alexandre: remover toda referência ao nome antigo.
Evidência levantada em 03/11/2026, somente leitura. Estados: CONFIRMED = comando rodado; INFERRED = não visto na tela.

Reaproveita, sem duplicar: F083 (marca e ícone), F084 (renomeação de módulos), F087 (identidade do pacote e pasta de dados, ainda RASCUNHO). Este plano é o guarda-chuva e fecha o que sobra.

## Decisões do Alexandre (03/11/2026)

- Uso pessoal, mas seguir com o plano inteiro: **zero referência visual** ao ancestral. Ficam só os arquivos de licença e os avisos de copyright (MIT) dentro dos arquivos.
- Depois deste plano vem uma **refatoração total** (estrutura de pastas, classes, variáveis, constantes, objetos, lógica). É outra spec (F091), não entra aqui.
- Por isso o namespace NÃO é renomeado em F088: vai junto com a refatoração (F091), em uma passada só, para não renomear duas vezes.
- Ponto de atenção: os 249 arquivos com `SPDX-License-Identifier: MPL-2.0` continuam MPL. O `LICENSE-MPL` e o cabeçalho deles não podem ser tratados como MIT. Para uso pessoal não há obrigação de publicar; só não apagar o aviso. Decisão pendente: manter `LICENSE-MPL` na raiz (recomendado).
- Cabeçalho "Copyright (c) KyraFiles Community": permanece (MIT exige se algum dia distribuir).

## Fases (uma branch por fase, saindo da colab atualizada, §18)

| Fase | O que | Risco | Prova |
|---|---|---|---|
| 0 | Concluída: eliminação de configurações Win32 nos .vcxproj nativos C++, correção de caminhos em todos os workflows (.github/), padrões em .gitignore, documentação em AGENTS.md e encerramento de processos em Open-KyraFilesDev.ps1 destravando a execução. | baixo | CONFIRMED: build C++ x64 ok, build C# 0 erros/warnings, 194 testes (193 ok, 0 falhas) |
| 1 | **F087**: Identity `KyraFiles`, Publisher `CN=Files`, PublisherDisplayName `Yair A`, alias e protocolo `kyrafiles-dev`, pasta `%LOCALAPPDATA%\Files`, chaves HKCU `kyrafiles-dev`. Migrar dados antes de trocar a identidade | **alto** (Windows trata como app novo; Win+E, associações) | `Get-AppxPackage`; dados migrados; Win+E |
| 2 | Texto visível: URLs `files.community` (docs, privacidade, blog de versão), `GitHubRepoUrl`, placeholder do CloneRepoDialog, links de issues em comentários | baixo | `git grep` sem ocorrências fora de LICENSE e NOTICE |
| 3 | Assets: pasta `KyraFilesOpenDialog` e `KyraFilesOpenDialog` duplicadas, DLLs `KyraFiles.App.*Dialog*.dll`, `KyraFiles.App.BackgroundTasks.dll/.winmd`, `SetFilesAsDefault.reg` | médio (COM/registro) | pasta instalada sem `Files.*` |
| 4 | Saída de build dentro de `src/` (`x64/Release`, `Debug/*.tlog`): redirecionar para `bin/obj` e ignorar no `.gitignore` | baixo | `git status` limpo após build |
| 5 | Nomes internos: semáforo `Files-{Env}-Instance`, mutex do redirector, processos `KyraFiles.App.Server` | médio (instância única, F086) | teste de instância única |
| 6 | ~~Namespace~~ movido para a refatoração F091 | - | - |
| 7 | Interface: revisão visual do que remete ao KyraFiles (ver abaixo) | médio | captura de tela, tela por tela, só depois das 23:59 (§16) |
| 8 | Licenças: manter `LICENSE-MIT` e `LICENSE-MPL` e os cabeçalhos; sem tela Sobre citando o ancestral (decisão do Alexandre; uso pessoal) | baixo | `git grep` visual sem "KyraFiles Community" fora de licenças |

## Fase 0: revisão do commit `e810d41e0` (03/11/2026, Claude)

A AGY relatou "0 arquivos com nomes legados". Conferido por `git grep -P '(?<![A-Za-z])Files\.App\.(Launcher|OpenDialog|SaveDialog|Server|BackgroundTasks)'` (o filtro sem o lookbehind dá falso positivo, pois `KyraFiles.App.X` contém `KyraFiles.App.X`).

- CONFIRMED: nenhum **arquivo** com nome legado fora de `bin/obj`; `colab` é ancestral da branch (2 commits à frente, ff possível).
- CONFIRMED, **resíduos de conteúdo** (afirmação da AGY está incorreta):
  - 4 workflows `.github/workflows/cd-*.yml` (linha 37/38): `LAUNCHER_PROJECT_PATH = src\KyraFiles.App.Launcher\KyraFiles.App.Launcher.vcxproj`, caminho que não existe mais. CI/CD nunca rodou (regra 4 da verificação), mas ficaria quebrado.
  - `.gitignore` linhas 7, 8 e 11: padrões `KyraFiles.App.OpenDialog*.dll`, `KyraFiles.App.SaveDialog*.dll`, `KyraFiles.App.Launcher.exe` (não cobrem os nomes novos).
  - `Open-KyraFilesDev.ps1` linhas 38 e 67: encerra processos `Files`, `KyraFiles.App.Server`, `KyraFiles.App.IndexService` pelo nome antigo; não derruba o `KyraFiles.App.Server` e o exe trava no próximo build. **Efeito real.**
  - `AGENTS.md` linhas 46 e 47: árvore de projetos com nomes antigos.
  - `STATUS.md`: menções históricas; manter como registro, não é resíduo.
- CONFIRMED, **x64 incompleto**: `KyraFiles.App.Launcher.vcxproj` ainda declara `Debug|Win32` e `Release|Win32` (linhas 9 a 19, 72 em diante). Conferir também OpenDialog e SaveDialog. O relatório diz que 32-bit foi eliminado; só foi parcialmente.
- RESOLVIDO (04/11/2026, AGY): Todos os resíduos da Fase 0 eliminados.
  - Workflows `.github/workflows/`: caminhos de projetos e solução corrigidos.
  - `.gitignore`: padrões adicionados (`KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`, `*.locked.*`).
  - `Open-KyraFilesDev.ps1`: encerramento de `KyraFiles`, `KyraFiles.App.Server`, `KyraFiles.App.IndexService` implementado com limpeza de arquivos locked, destravando a execução.
  - `.vcxproj` nativos: configurações Win32 removidas de Launcher, OpenDialog e SaveDialog.
  - `AGENTS.md`: estrutura de código atualizada.
  - Evidências: Build nativo C++ x64 OK (0 erros), build C# WinUI x64 OK (0 erros, 0 warnings), 194 testes unitários executados (193 aprovados, 1 ignorado justificado, 0 falhas).
- NOT TESTED por mim: build C++ x64, build C# e os 193 testes (valem `INFERRED`, vêm do relatório da AGY). Exe novo com data posterior ao commit também não conferido.
- Integração na colab (squash/rebase + ff-only) **não feita**: depende do `/merge` do Alexandre e dos resíduos acima corrigidos.

## Regras de execução
- Primeiro listar todo uso (`git grep`) e só depois renomear. Um tipo de ocorrência por commit.
- Zero regressão de desempenho (§18.5); `verify_no_index_service.ps1` ao fim de cada fase.
- Exe novo só vale com data posterior ao commit (`ls --time-style=full-iso`) e nota de liberação.
- Nenhum push, merge na main ou `--force` sem pedido do Alexandre.
- Fase 1 só começa com backup das configurações e aprovação do F087.

## Fora de escopo
Reescrever a arquitetura, trocar o toolkit visual, apagar o histórico git, remover os arquivos de licença.

## Pendências para a spec (não aprovadas)
- [RESOLVIDO 03/11] namespace: vai para F091 (refatoração).
- [NEEDS CLARIFICATION] manter `kyrafiles-dev:` como alias de compatibilidade ou remover (já em F087).
- [NEEDS CLARIFICATION] "referências visuais": só marca, textos, links, ícones e editora, ou também o layout (abas, barra lateral, barra de endereço)? Sem resposta, a Fase 7 cobre só marca, textos, links, ícones e editora.
