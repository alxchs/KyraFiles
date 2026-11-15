# F093 — Compilação e Deploy Otimizados (Modo Rápido e Modo Completo) — SPEC

Status: **APROVADA** pelo Alexandre em 14/11/2026.
Origem: Item 2 da priorização ("implemente modo rápido e modo completo").

## 1. Contexto e Motivação
O ciclo tradicional de build, testes e deploy levava cerca de 13 minutos, devido à execução dos 237 testes unitários (muitos com I/O de disco intenso e arquivos temporários de centenas de MBs), recompilação integral de toda a solução e compressão ultra LZMA2 no Inno Setup.
Para desenvolvimento diário e iterações ágeis, é necessário um método rápido que apenas compile as alterações incrementais, gere o instalador rapidamente e atualize a instalação local em menos de 2 minutos.

## 2. Histórias de Usuário
- **US1 (P1):** Como desenvolvedor/mantenedor, quero rodar um comando ágil (`tools/Quick-Ship.ps1 -Fast`) que compile incrementalmente e atualize minha instalação local em < 2 minutos sem rodar testes pesados.
- **US2 (P1):** Como mantenedor, quero poder rodar o ciclo completo oficial (`tools/Quick-Ship.ps1 -Full`) com validação de testes, compressão ultra e gates de release quando for gerar uma versão oficial.

## 3. Cenários de Aceite (AC)
| ID | Cenário | Resultado Esperado |
|---|---|---|
| AC-1 | Execução em Modo Rápido (`-Fast`) | Pula testes unitários, compilação incremental de `KyraFiles.App.csproj`, Inno Setup com compressão rápida, deploy direto em `%LOCALAPPDATA%\Programs\KyraFiles`. Tempo total < 2 minutos. |
| AC-2 | Execução em Modo Completo (`-Full`) | Executa bateria de testes unitários, compilação limpa, Inno Setup com compressão ultra, validação de gates. |
| AC-3 | Fechamento Gracioso de Processos | Ambos os modos encerram processos em execução antes de atualizar os binários no destino oficial sem travas de arquivo. |
| AC-4 | Verificação de Contenção | Validação do gate F021 (`verify_no_index_service.ps1`) ao final do deploy. |
