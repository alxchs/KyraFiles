# ADR-008 — Escolha do Motor de Script Embutido (MoonSharp vs Jint)

**Status:** Proposed  
**Data:** 2026-10-27  
**Responsável:** AGY (executora) / Alexandre (decisor)  
**Rastreabilidade:** F060 (M4 Scripting), `spec.md` (FR-M4, M4-AC-1..6), `origem/SPEC_original_v1.2.md` (SCRIPT-001 a 012, §5.5 e Gate §10.7).

---

## 1. Contexto

O módulo **M4 (Scripting)** da feature **F060** visa fornecer um mecanismo interno de automação para tarefas frequentes e repetitivas sobre arquivos (filtragem por extensão/nome/data, renomeio, cópia, movimentação pela fila e logs), eliminando a dependência obrigatória de scripts externos em PowerShell ou AutoHotkey para tarefas básicas.

Conforme a especificação:
- O interpretador deve rodar embutido em processo, sem dependências nativas externas;
- A API exposta ao script deve ser estritamente restrita (expondo apenas abstrações seguras de domínio como `fs`, `log` e futuramente `queue`);
- O ambiente deve prevenir travamentos acidentais ou hostis (timeouts determinísticos, cancelamento, limites de instruções ou memória);
- **Gate de Dependências (§10.7 da SPEC original):** Nenhum pacote NuGet pode ser introduzido no `src/` da aplicação sem aprovação prévia do Alexandre, documentando licença, compatibilidade, manutenção e vulnerabilidades. O spike atual foi conduzido isoladamente em `tools/perf/ScriptEngineSpike` sem tocar em nada de `src/` ou `tests/`.

---

## 2. Candidatos Avaliados

Fontes públicas consultadas em 27/10/2026 via [nuget.org](https://www.nuget.org/) e repositórios oficiais:

### Candidato A: MoonSharp (Lua)
- **Repositório oficial:** [https://github.com/moonsharp-devs/moonsharp](https://github.com/moonsharp-devs/moonsharp)
- **Pacote NuGet:** [https://www.nuget.org/packages/MoonSharp/](https://www.nuget.org/packages/MoonSharp/)
- **Licença:** 3-clause BSD (BSD-3-Clause) — permissiva, compatível com MIT/projeto Files.
- **Versão estável mais recente e data:** `2.0.0` (14 de outubro de 2016). *(Nota: versão preliminar `3.0.0-beta.1` lançada em julho de 2026).*
- **Alvo de framework:** .NET Standard 1.6 / 2.0, .NET Framework 3.5+. Funciona sob `net10.0` pela camada de compatibilidade do .NET Standard.
- **Dependências externas:** Nenhuma (zero dependências no pacote).
- **Manutenção ativa:** Baixa / recentemente reativada. Ficou praticamente estagnado entre 2016 e 2024; assumido em 2025/2026 por Max Hayman (Kinetic Games), que gerou a prévia 3.0.0-beta.1 em julho de 2026.
- **CVEs conhecidos:** Nenhum CVE formal ou advisory aberto contra o núcleo da biblioteca.

### Candidato B: Jint (JavaScript)
- **Repositório oficial:** [https://github.com/sebastienros/jint](https://github.com/sebastienros/jint)
- **Pacote NuGet:** [https://www.nuget.org/packages/Jint/](https://www.nuget.org/packages/Jint/)
- **Licença:** 2-clause BSD (BSD-2-Clause) — permissiva, compatível com MIT/projeto Files.
- **Versão estável mais recente e data:** `4.17.0` (07 de outubro de 2026).
- **Alvo de framework:** Multi-targeting explícito: `net472`, `netstandard2.0`, `netstandard2.1`, `net8.0` e compilação nativa para `net10.0`.
- **Dependências externas:** Nenhuma (zero dependências externas).
- **Manutenção ativa:** Muito ativa. Atualizações contínuas semanais, mantido ativamente por Sébastien Ros e comunidade, com lançamentos estáveis mensais regulares.
- **CVEs conhecidos:** Nenhum CVE crítico no motor central do Jint. (Incidentes como CVE-2026-41171 em Squidex decorreram de funções HTTP customizadas do Squidex expostas ao Jint sem validação de SSRF, e não do interpretador Jint em si).

---

## 3. Protótipo e Medições Experimentais

Foi desenvolvido um protótipo console fora da aplicação em `tools/perf/ScriptEngineSpike` (isolado de `src/`).
O spike foi executado sob .NET 10.0.301 Release (`x64`).

### Comando executado
```powershell
dotnet run --project tools/perf/ScriptEngineSpike/ScriptEngineSpike.csproj -c Release
```

Evidência em CSV gerada em:  
`docs/agents/evidence/F060/script_engine_spike_20261027_115847.csv`

### Tabela Comparativa de Resultados

| Critério | Teste / Cenário | MoonSharp (Lua) | Jint (JavaScript) | Observações |
|---|---|---|---|---|
| **(a) Trivial** | Carga e execução de script trivial (`10 + 32`) | **188,00 ms** (Result=42) | **705,25 ms** (Result=42) | MoonSharp tem inicialização a frio mais rápida para chunks simples; Jint analisa o runtime ECMAScript completo. |
| **(b) Timeout** | Laço infinito interrompido em até 2 s | **2.000,10 ms** (70.723 yields) | **2.017,50 ms** (Timeout nativo) | Jint possui `options.TimeoutInterval(TimeSpan)` nativo. MoonSharp **não** possui timeout nativo no `DoString`: exige empacotar o script em `Coroutine` com `AutoYieldCounter` e controlar o tempo em C#. |
| **(b.2) Cancel** | Interrupção por `CancellationToken` | Não suportado nativamente | **507,49 ms** (`ExecutionCanceledException`) | Jint integra diretamente com `CancellationToken` do .NET. |
| **(c) Limite Memória** | Bloquear script que aloca memória excessiva (> 2 MB) | **Não suportado** (sem medidor de memória) | **114,74 ms** (`MemoryLimitExceededException`) | Jint aborta preventivamente assim que a alocação do script ultrapassa a cota informada (`LimitMemory(2_000_000)`). |
| **(c.2) Limite Instruções**| Bloquear laço por contador de instruções/statements | **26,43 ms** (51 ciclos de yield) | **5,72 ms** (`StatementsCountOverflowException`) | Jint tem `options.MaxStatements(500)`. MoonSharp alcançado via contagem manual de yields de corrotina. |
| **(d) API Normal** | Acesso ao objeto `fs` e `log` expostos | **178,65 ms** (2 logs registrados) | **77,88 ms** (2 logs registrados) | Ambos executam normalmente chamadas controladas ao host. |
| **(d) Fuga 1** | Tentar `os.execute` (Lua) / `System.IO` (JS) | **PASS** (bloqueado: `nil` em `Preset_HardSandbox`) | **PASS** (bloqueado: `ReferenceError: System is not defined`) | Em MoonSharp `Preset_HardSandbox` remove `os.execute`. Em Jint o acesso a tipos CLR é desligado por padrão. |
| **(d) Fuga 2** | Tentar `io.open` (Lua) / `Process.Start` (JS) | **PASS** (bloqueado: `io` é `nil`) | **PASS** (bloqueado: `ReferenceError: System is not defined`) | Bloqueados com sucesso em ambos os motores. |
| **(d) Fuga 3** | Tentar `require` ou `import` de módulos | N/A (Lua usa `require` bloqueável) | **PASS** (bloqueado: `require` indefinido, `import` desabilitado) | Jint recusa módulos externos por padrão. |
| **(d) Fuga 4** | Fuga via construtor/reflexão (`Function("return process")`) | N/A | **PASS** (bloqueado: retorna `undefined`, sem escape para o host) | Construtores dinâmicos do JS não acessam o processo hospedeiro. |
| **(e) Pico Memória** | Pico de Working Set do processo no teste | **52 MB** | **53 MB** | Consumo de memória equivalente e desprezível em ambos os interpretadores. |

---

## 4. Análise de Segurança e Fuga de Sandbox

Os testes de fuga de sandbox foram executados obrigatoriamente para validar o isolamento:

1. **MoonSharp:**
   - Com o preset padrão (`CoreModules.Preset_Default`), módulos perigosos como `io` e `os.execute` ficam acessíveis ao script.
   - Para garantir a segurança mínima, é **obrigatório** usar `CoreModules.Preset_HardSandbox`, que suprime totalmente `io`, `debug`, `os.execute` e carregamento de código dinâmico.
   - **Vulnerabilidade de execução:** MoonSharp **não** oferece suporte nativo a `CancellationToken` nem a `TimeoutInterval` no método síncrono `script.DoString(...)`. Se um usuário rodar `while true do end`, a thread do .NET trava indefinidamente sem possibilidade segura de cancelamento. Para interromper, é necessário transformar todo script em uma `Coroutine`, definir `AutoYieldCounter = 1000`, e realizar um laço manual de `Resume()` com checagem de relógio em C#.
   - **Falta de controle de memória:** MoonSharp não possui API para limitar o consumo de memória alocada por tabelas e strings do script Lua.

2. **Jint:**
   - O modelo de segurança do Jint é restrito por padrão (*opt-in*). Tipos CLR do .NET (`System`, `System.IO`, `System.Diagnostics.Process`) não são visíveis e disparam `ReferenceError` imediato, a menos que o desenvolvedor invoque deliberadamente `.AllowClr()`.
   - APIs de ambiente como `require`, `import` e variáveis globais do Node/Browser inexistem por padrão.
   - Possui mecanismos de contenção de primeiro nível no próprio construtor:
     - `options.TimeoutInterval(TimeSpan.FromSeconds(2))`
     - `options.CancellationToken(cancellationToken)`
     - `options.LimitMemory(maxBytes)`
     - `options.MaxStatements(maxStatements)`
   - Não requer contornos com corrotinas para timeouts: o próprio despachante de instruções do Jint confere os limites e lança exceções especializadas capturáveis (`TimeoutException`, `MemoryLimitExceededException`, `StatementsCountOverflowException`, `ExecutionCanceledException`).

---

## 5. Decisão Recomendada

**Recomenda-se a adoção do Candidato B: Jint (JavaScript).**

### Justificativas:
1. **Governança e Manutenção:** Jint possui manutenção contínua e ativa (versão estável de outubro de 2026), com testes automatizados e multi-targeting para .NET moderno. O MoonSharp teve sua última versão estável oficial em 2016 e está sob transição recente de mantenedor com versões beta.
2. **Robustez de Contenção (Sandboxing):** Jint oferece suporte de primeira classe a **limite de memória** (`LimitMemory`), **limite de tempo** (`TimeoutInterval`), **cancelamento com token** (`CancellationToken`) e **limite de instruções** (`MaxStatements`). No MoonSharp, timeouts dependem de controle manual de corrotina e não há suporte a limite de memória.
3. **Segurança por Padrão:** Em Jint, a reflexão e o acesso a classes do .NET são rigorosamente desabilitados por padrão.
4. **Ergonomia e Familiaridade:** JavaScript é amplamente conhecido por desenvolvedores e usuários avançados, facilitando a criação de filtros e manipulações de arquivos em comparação à sintaxe de Lua (indexação em 1, tabelas sem métodos nativos de array).

---

## 6. O que NÃO fazer (Regras Duras de Implementação)

Caso a recomendação seja aceita e aprovada pelo Alexandre:

1. **PROIBIDO expor `System.IO`, `Process`, `WebClient`/`HttpClient` ou `Type`/Reflexão:**
   - Em Jint, **NUNCA** chamar `.AllowClr()` ou `.AllowClrWrite()`.
   - O script só deve interagir com instâncias estritamente controladas de classes de fachada (`SafeFs`, `SafeLog`, `SafeQueue`), cujos métodos realizem sanitização e validação de caminhos.
2. **SIMULAÇÃO (Dry-Run) OBRIGATÓRIA na primeira execução:**
   - Todo script novo ou editado deve obrigatoriamente rodar primeiro em modo simulação (`dryRun = true`), exibindo na UI o plano exato de alterações (quais arquivos seriam renomeados/copiados/movidos) antes de permitir a execução real.
3. **PROIBIDO rodar scripts sem Timeout e sem Limite de Memória:**
   - Toda instância de `Engine` criada na aplicação deve conter obrigatoriamente `TimeoutInterval(TimeSpan.FromSeconds(5))` e `LimitMemory(...)` configurados.
4. **NÃO executar scripts em thread de UI:**
   - Toda execução deve rodar em background via `Task.Run` com `CancellationToken` repassado ao motor e vinculado ao cancelamento da UI/Status Center.

---

## 7. Gate de Dependências (§10.7) e Próximos Passos

- Conforme as regras da SPEC e o processo de SDD, **nenhuma dependência NuGet foi instalada no projeto principal (`src/`)**.
- A inclusão do pacote `Jint` (versão 4.17.0, licença BSD-2-Clause, 0 dependências) em `Directory.Packages.props` e `src/KyraFiles.Shared/KyraFiles.Shared.csproj` fica formalmente **condicionada à aprovação deste ADR pelo Alexandre**.
- Uma vez aprovado o ADR pelo Alexandre, a tarefa M4-T1 poderá ser iniciada para estruturar o `ScriptEngine` em `KyraFiles.Shared`.

---

## 8. Evidências Anexas

- Código do Spike: `tools/perf/ScriptEngineSpike/Program.cs` e `ScriptEngineSpike.csproj` (executável descartável fora do app).
- Medições brutas: `docs/agents/evidence/F060/script_engine_spike_20261027_115847.csv`.
