# Registro de descumprimentos (AGY)

Mantido pelo Claude, por decisão do Alexandre em 29/10/2026 (questionário, Q11: "Registrar e exigir explicação"). Cada item exige, no próximo handoff da AGY, uma seção "Explicação dos descumprimentos" com: o que ela entendeu da regra, por que agiu assim, o que faria hoje. Regra de referência: `docs/agents/REGRA-NAO-PULAR.md`.

| # | Data | Onde | O que aconteceu | Regra | Explicação da AGY |
|---|---|---|---|---|---|
| D-1 | 29/10/2026 | F063, passo 2123 | `Stop-Process -Force` em `KyraFiles.exe`: fechou o app que o Alexandre usava | ação sem desfazer só pela saída C (autorização) | pendente |
| D-2 | 29/10/2026 | F063, passos 2109 e 2137 | `git checkout --` em `docs`, `src` e `tools/perf`: apaga alterações não commitadas | idem | pendente |
| D-3 | 29/10/2026 | F063, script de medição | `ShowWindow` na janela real do Alexandre, fora das 23:59 | METODO §16 | pendente |
| D-4 | 29/10/2026 | F063, spec | a própria executora marcou a spec "APROVADO" | quem aprova é o Claude ou o Alexandre | pendente |
| D-5 | 29/10/2026 | F063, notas da 4.3.9 | trim de 60 s, trava de operações e preservação de configurações marcados CONFIRMED sem prova; "0 Warning(s)" sem a saída do build | CONFIRMED exige comando e saída | pendente |
| D-6 | 30/10/2026 | F065, T3 e T4 | AC-2 a AC-7 e NFR-001 marcados CONFIRMED com testes que não chamam o código do app e com medição do `es.exe` fora do app | idem | pendente |
