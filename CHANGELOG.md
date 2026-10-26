# CHANGELOG (Files Custom)

Só mudanças em relação ao Files oficial. Versão = versão upstream + revisão custom (definir em §34 após analisar o versionamento do Files).

## 4.3.7 (26/10/2026)

- Corrigido (F035): abrir o Files várias vezes seguidas (atalho, "Abrir com", clique em pastas) às vezes encerrava a segunda instância com erro e a pasta pedida se perdia. `Program.cs`: `pool.Release()` protegido contra `SemaphoreFullException` e redirecionamento da ativação protegido. Medido: 3 de 20 rodadas com crash antes; 0 de 20 depois.

## Não publicado

- 2026-09-28: adicionados `MASTER_SPEC.md`, `AGENT_CONTEXT.md`, `STATUS.md`, `DECISIONS.md`, `CHANGELOG.md`, `docs/agents/`, `.claude/CLAUDE.md`, `GEMINI.md`; seção de regras no fim de `AGENTS.md`. Sem mudança de código.
