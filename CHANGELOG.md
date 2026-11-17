# CHANGELOG (KyraFiles)

Só mudanças em relação ao KyraFiles oficial. Versão = versão upstream + revisão custom (definir em §34 após analisar o versionamento do KyraFiles).

## 4.3.7 (26/10/2026)

- Corrigido (F035): abrir o KyraFiles várias vezes seguidas (atalho, "Abrir com", clique em pastas) às vezes encerrava a segunda instância com erro e a pasta pedida se perdia. `Program.cs`: `pool.Release()` protegido contra `SemaphoreFullException` e redirecionamento da ativação protegido. Medido: 3 de 20 rodadas com crash antes; 0 de 20 depois.

## Não publicado

- 2026-11-14 (F057 / F056 / F036 / F070):
  - **F057 — Atalhos de botões em caixas de diálogo:**
    - Letras de acesso/atalho sublinhadas nos botões de todas as caixas de diálogo (Alt + letra).
    - Algoritmo inteligente sem duplicatas na mesma caixa, com normalização de acentos.
    - 8 testes unitários aprovados em `DialogAccessKeysTests.cs`.
  - **F056 — Copiar caminho em vários formatos:**
    - Submenu "Copiar caminho como..." no menu de contexto com formatos Windows, WSL (/mnt/c/...), JSON, C# literal, URL de arquivo e relativo ao repositório git.
    - Suporte a multi-seleção ordenada com quebra de linha.
    - Preferências em Configurações > Geral para habilitar/desabilitar formatos individualmente.
    - 13 testes unitários aprovados em `PathFormatterTests.cs`.
  - **F036 — Fila de cópias com pausar e retomar (Fases A e B):**
    - `OperationQueue`: controle de concorrência por destino físico (limite configurável padrão 2) com operações de Delete e Rename independentes fora da fila.
    - Velocidade com média móvel exponencial (EMA alpha=0.3) e estimativa de tempo restante coerente em `StatusCenterItemProgressModel`.
    - Resiliência com `LastFailedItems` e botão de reintento exclusivo dos itens com falha no cartão de status (`StatusCenter.xaml`).
    - Proteção ao fechar janela com operações ativas ou em fila interceptando `AppWindow.Closing` com diálogo modal de 3 opções.
    - Pausa e retomada entre arquivos com `PauseGate` thread-safe, e comportamento honesto para arquivo único (botão desabilitado com dica explicativa).
    - 28 testes automatizados aprovados (0 falhas) e benchmarks de memória (NFR-001) e sobrecarga em CSV.
  - **F070 — Integração GitHub OAuth Device Code Flow (T6):**
    - Client ID real (`Ov23liTPMwjatYdDHekc`) configurado e validado contra a API real do GitHub Device Code Flow (RFC 8628).
- 2026-11-13: adotado o Protocolo de Controle de Produção v2 (`docs/production/`, `docs/specs/SPECS.md` regerado). Só documentação.
- 2026-09-28: adicionados `MASTER_SPEC.md`, `AGENT_CONTEXT.md`, `STATUS.md`, `DECISIONS.md`, `CHANGELOG.md`, `docs/agents/`, `.claude/CLAUDE.md`, `GEMINI.md`; seção de regras no fim de `AGENTS.md`. Sem mudança de código.
