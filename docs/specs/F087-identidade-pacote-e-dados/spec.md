# F087 - Identidade do pacote e pasta de dados com o nome KyraFiles

Status: RASCUNHO (pedido do Alexandre, 02/11/2026: "as configuracoes precisam passar para um caminho oficial com o novo nome do app como se nunca tivesse existido o nome antigo")

## Estado atual (CONFIRMED em 02/11/2026)
- `Package.appxmanifest` ainda tem `Identity Name="KyraFiles"`; por isso os dados ficam em `%LOCALAPPDATA%\Packages\KyraFiles_sxng2gqmgahde\LocalState`.
- Existe um alias `kyrafiles.exe` e o protocolo `kyrafiles-dev:` usados por Win+E, launcher C++, dialogos e HotkeyService.
- F081 ja migra configuracoes de pastas antigas para a pasta de dados atual (copia sem sobrescrever).

## Necessidade
Identidade e dados com o nome KyraFiles; nada com o nome antigo depois da migracao, incluindo pacote registrado, alias, protocolo, pasta de dados, entrada de inicio automatico e atalho.

## Pontos a decidir antes de aprovar
- [NEEDS CLARIFICATION] trocar o nome de identidade muda a familia do pacote: o Windows trata como app novo (Win+E, associacoes). Confirmar que e aceitavel e que o pacote antigo sai no mesmo passo.
- [NEEDS CLARIFICATION] manter o protocolo `kyrafiles-dev:` como alias de compatibilidade ou remove-lo (o launcher C++ e os dialogos precisam ser recompilados).
