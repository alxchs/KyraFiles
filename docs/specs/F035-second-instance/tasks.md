# F035 — Segunda instância e abertura direta — TASKS

Status: **APROVADAS** (Claude, 24/10/2026; concluída e unificada na v4.3.31).

- [x] **T1 — Reproduzir o bug (controle negativo):** `test_f035_activation.ps1` comprovando a falha em 10/20 rodadas no código sem proteção.
- [x] **T2 — Corrigir o semáforo:** `Program.cs` com proteção contra `SemaphoreFullException` em `pool.Release()` e redirecionamento protegido.
- [x] **T3 — Provar AC-1, AC-2, AC-3:** Validação dos cenários de abertura em paralelo e proteção de processos sem exceções 1026.
- [x] **T4 — Descobrir quem abre o `KyraFiles.exe` direto:** Causa identificada e atalhos alinhados com o pacote AppX.
- [x] **T5 — Corrigir a abertura direta e provar AC-4:** Validação de inicialização sem `REGDB_E_CLASSNOTREG`.
- [x] **T6 — Eliminação do fechamento silencioso e liveness check (v4.3.31):** Remoção do `Environment.Exit(0)` precoce no construtor estático e checagem de processo ativo (`IsInstanceProcessAlive`), fechando definitivamente os critérios de AC-1 a AC-5.
