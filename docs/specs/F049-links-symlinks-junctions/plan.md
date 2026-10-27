# F049 — Links simbólicos, junctions e hardlinks — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** `ListedItem` tem a propriedade `IsSymLink` (`src/Files.App/Data/Items/ListedItem.cs:693`, `:880`, `:913`) e `IsLinkItem` (atalho de internet, `.url`, linha 536). **INFERRED**: `IsSymLink` hoje cobre atalhos `.lnk`/symlink de listagem; T0 confirma o que ela realmente marca e se separa junction de symlink.
- **CONFIRMED** não há ação de criar link: a busca por `SymbolicLink|Junction|CreateHardLink` em `src/` não achou nada (24/10/2026). Existe só `CreateShortcut` (`.lnk`).
- **INFERRED** o tamanho de pasta (`FolderSizeProvider`) segue links; T0 verifica e mede.
- **INFERRED** a exclusão pode recursar dentro de uma junction (risco de apagar o destino); precisa de teste de controle antes de qualquer correção.

## 2. Abordagem
1. **Criar:** ação "Criar link…" com três escolhas: simbólico (`File/Directory.CreateSymbolicLink`, disponível no .NET), junction (sem API no .NET: usar a chamada de reparse point do Windows ou `mklink /J`; T0 decide por medição de custo e confiabilidade) e hardlink (`CreateHardLinkW`). Diálogo pergunta o local e o nome; se faltar permissão para simbólico, explica e oferece junction (pasta) ou hardlink (arquivo) (AC-5).
2. **Mostrar:** listagem marca itens com atributo de reparse point **já devolvido pela enumeração** (sem abrir cada arquivo) e lê o destino só dos itens marcados, em segundo plano (NFR-001). Coluna "Destino do link" nova, desligada por padrão.
3. **Excluir sem seguir (AC-3):** garantir que excluir um link remova só o link. Se a rotina atual seguir o link, corrigir só esse ponto.
4. **Contar tamanho sem duplicar (AC-6):** o cálculo de tamanho ignora o conteúdo de links.
5. **Copiar link relativo (AC-7):** diálogo de escolha (copiar o link ou o conteúdo).
Opções descartadas: executar `mklink` em `cmd` para tudo (processo por chamada, mensagens de erro em texto); editar o destino de um link existente (fora de escopo pela decisão da spec).

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 | nova ação `CreateLinkAction` + diálogo; helper nativo em `Helpers/Win32/` |
| FR-002 (visual por tipo) | ícone/sobreposição e coluna no layout de detalhes |
| FR-003; AC-3, AC-6 | rotina de exclusão e `FolderSizeProvider` (ajuste mínimo, com teste) |
| NFR-001 | leitura do destino só dos itens com atributo de reparse point |
| AC-4 (link quebrado) | listagem trata destino ausente sem exceção |

**Arquivos a mexer:** nova ação, diálogo e viewmodel; helper em `Helpers/Win32/`; `ListedItem` (campo do destino, só leitura); definição de colunas (a coluna nova); `FolderSizeProvider`; item de menu; `Strings/en-US`. **A NÃO mexer:** `CreateShortcutAction`, F033, `Directory.Build.props`.

## 4. Riscos
- **Perda de dados** se a exclusão ou a cópia seguirem uma junction: por isso o controle negativo vem **antes** de qualquer correção (T1).
- Permissão: link simbólico exige modo desenvolvedor ou administrador; a mensagem e o recuo para junction/hardlink são requisito (AC-5).
- Ciclos de links: a varredura de tamanho precisa de limite de profundidade.

## 5. Estratégia de teste (por AC, com asserção)
Estrutura de teste criada pelo script em `%TEMP%\agy\<GUID>\...` (pasta com arquivos, link simbólico, junction, hardlink, link quebrado, ciclo). Testes automáticos: criação por tipo (AC-1), destino lido (AC-2), exclusão só do link verificada contando os arquivos do destino antes e depois (AC-3), link quebrado sem exceção (AC-4), tamanho sem duplicar (AC-6). UI (menu, coluna, diálogo de permissão) com UIA e o helper revisado, em instância própria. `resultado.csv` em `docs/agents/evidence/F049/`.

## Atualização de 25/10/2026 (resultado da T0, revisão do Claude)
- **CONFIRMED** o enumerador marca `IsSymLink` só para `IO_REPARSE_TAG_SYMLINK`; **junction (`IO_REPARSE_TAG_MOUNT_POINT`, 0xA0000003) aparece como pasta comum**. A T3 precisa distinguir os tipos de reparse point.
- **CONFIRMED** `CachedSizeProvider.cs:61` **já ignora reparse points** no tamanho de pasta: o AC-6 está em boa parte atendido; a T4 só prova (com o teste), não implementa.
- **INFERRED** (era dado como confirmado na T0) que a exclusão pela Shell não segue o link: a T1 é o teste que decide.
