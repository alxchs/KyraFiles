# ADR-007 — Escolha da Biblioteca para Conectividade SFTP e Diretrizes de Segurança

**Status:** Proposed  
**Data:** 2026-10-27  
**Responsável:** AGY (executora) / Claude (arquiteto)  
**Requisitos relacionados:** F060 (M3 — SFTP: M3-AC-1 a M3-AC-6, FR-M3, NFR-003, NFR-004); SPEC original v1.2 (§4 MOD-REMOTE, §10.7 Gate de dependências, REM-003, REM-005, REM-006, REM-012).

---

## 1. Contexto

O KyraFiles já possui suporte a conexões de rede FTP por meio da biblioteca `FluentFTP` (v55.0.0), mapeado em `src/KyraFiles.App.Storage/Ftp/*` e `src/KyraFiles.App/Utils/Storage/StorageItems/FtpStorage*`. No entanto, a revisão arquitetural realizada na tarefa M3-T0 evidenciou as seguintes realidades no código atual:
1. **O FluentFTP não suporta SFTP:** O protocolo SFTP (SSH File Transfer Protocol) é um subsistema do SSH-2 (porta 22 por padrão), enquanto FTP/FTPS é um protocolo completamente distinto baseado em canais de controle e dados TCP (portas 21/990). O autor do FluentFTP afirma oficialmente que a biblioteca não suporta e não suportará SFTP, recomendando o uso de bibliotecas especializadas como `SSH.NET`.
2. **FTPS existente inativo:** O código atual do KyraFiles reconhece os esquemas `ftps://` e `ftpes://` para apontar a porta 990, porém instancia o cliente `AsyncFtpClient` sem habilitar `FtpConfig.EncryptionMode` (permanecendo em `None`). Portanto, FTPS não negocia TLS e opera apenas em texto puro.
3. **Fragilidade de credenciais no FTP:** As credenciais atuais de FTP são mantidas exclusivamente em um dicionário estático em memória (`FtpManager.Credentials`), sem persistência e sem o uso de DPAPI ou do Windows Credential Locker (`PasswordVault`).

Para atender ao módulo M3 da F060 com qualidade de produção, é indispensável selecionar uma biblioteca para SFTP que seja aderente à stack .NET 10, de código aberto permissivo, estável e que respeite padrões rígidos de segurança.

---

## 2. Opções Avaliadas

### Opção A: `SSH.NET` (versão 2026.0.0) — **Recomendada**
- **Tipo:** Biblioteca puramente gerenciada (.NET IL) sob licença MIT.
- **Mantenedor:** Renci ([https://github.com/sshnet/SSH.NET](https://github.com/sshnet/SSH.NET)).
- **Alvos:** Suporte nativo a `.NET 10.0` (`net10.0`), `.NET 9.0`, `.NET 8.0`, `.NET Standard 2.0` e `.NET Framework 4.6.2`.
- **Prós:**
  - Código aberto consolidado, sem custo de licença ou royalties.
  - Implementação completa do protocolo SSH-2 e SFTP (versões 1 a 6).
  - Autenticação flexível por senha, chave privada (OpenSSH, PEM, PKCS#8), frases-senha e múltiplos métodos combinados.
  - Não requer binários nativos de C++ vinculados à arquitetura (100% gerenciado, compatível de imediato com x64 e ARM64).
  - Suporte a verificação de chave de host (`HostKeyReceived`), listagem assíncrona, streams de leitura/escrita e cancelamento via `CancellationToken`.
- **Contras:**
  - Depende do `BouncyCastle.Cryptography` (>= 2.7.0) no .NET 10 para operações criptográficas avançadas, o que impacta o tamanho do binário (~6 MB).

### Opção B: `FluentFTP`
- **Tipo:** Biblioteca gerenciada para FTP/FTPS.
- **Avaliação:** Descartada imediatamente. A própria documentação oficial do FluentFTP declara expressamente que SFTP não é suportado e recomenda o SSH.NET.

### Opção C: `Rebex SFTP` ou `Chilkat SFTP`
- **Tipo:** Bibliotecas comerciais proprietárias de código fechado.
- **Avaliação:** Descartadas. Ambas exigem aquisição de licenças comerciais e pagamento de royalties, sendo incompatíveis com o modelo de código aberto do KyraFiles (licença MIT).

### Opção D: `libssh2` via P/Invoke
- **Tipo:** Biblioteca nativa em linguagem C com wrappers gerenciados.
- **Avaliação:** Descartada. A política de desenvolvimento do projeto (`AGENTS.md`) rejeita novos usos de `DllImport` e P/Invoke ad-hoc sem justificativa extrema. Exigiria compilar e empacotar DLLs nativas para x64 e ARM64, aumentando a complexidade de build, o risco de vazamentos de memória nativa e falhas de runtime difíceis de rastrear.

---

## 3. Critérios de Avaliação

| Critério | SSH.NET (2026.0.0) | Rebex / Chilkat | libssh2 (P/Invoke) | FluentFTP |
|---|---|---|---|---|
| **Licença** | MIT (Permissiva) | Comercial Paga | BSD (Código) / Incompat. wrappers | MIT |
| **Suporte a SFTP** | Sim (Completo) | Sim | Sim | Não |
| **Compatibilidade .NET 10** | Nativa (`net10.0`) | Variável | Exige wrappers | Nativa |
| **Arquitetura (x64/ARM64)** | 100% Gerenciado (Any CPU) | Depende do pacote | Requer binários nativos por ABI | 100% Gerenciado |
| **Conformidade com AGENTS.md** | Total (sem P/Invoke) | Parcial | Reprovado (viola DllImport) | Total |
| **Manutenção e Comunidade** | Ativa / Ampla | Comercial | Dependente de bindings | Ativa (mas só FTP) |

---

## 4. Decisão Recomendada

Adotar o pacote NuGet **`SSH.NET` versão `2026.0.0`** (ou a versão de linha 2024.2.0 se houver restrição futura) para a implementação do módulo M3 (SFTP) do KyraFiles.

A integração deverá ser implementada em componentes dedicados a SFTP (ex.: `SftpStorageFolder`, `SftpStorageFile`, `SftpHelpers`), mantendo o provedor FTP existente isolado e sem regressões.

---

## 5. Riscos e Mitigações

1. **Impacto no tamanho do pacote (NFR-004 e regra de 5 MB):**
   - *Risco:* A inclusão da dependência transitória `BouncyCastle.Cryptography` adiciona aproximadamente 6,0 MB a 7,2 MB em disco (DLL descompactada).
   - *Mitigação:* Justificativa técnica formal documentada: o SSH-2 exige primitivas de cifras e curvas elípticas (como Ed25519 e chaves OpenSSH novas) que não estão expostas uniformemente na BCL básica do .NET. O crescimento de ~6 MB é o menor custo técnico viável para prover SFTP nativo e seguro sem introduzir DLLs nativas de C++.
2. **Histórico de CVEs:**
   - *Risco:* Vulnerabilidades passadas no protocolo SSH ou no SSH.NET (CVE-2022-29245, Terrapin CVE-2023-48795, CVE-2026-48798).
   - *Mitigação:* A versão estável mais recente `2026.0.0` inclui a mitigação estrita do Terrapin attack (`strict kex`), corrige o gerador de números aleatórios e saneia o download no SCP. Além disso, o KyraFiles utilizará apenas a API de SFTP (`SftpClient`), que não é vulnerável ao CVE-2026-48798 (exclusivo do `ScpClient`).
3. **Trimming e Native AOT:**
   - *Risco:* A biblioteca BouncyCastle contém classes de algoritmos carregadas dinamicamente que podem gerar alertas de trimming.
   - *Mitigação:* Como o KyraFiles não compila a UI principal como Native AOT total (usa WinUI 3 com compilação JIT gerenciada padrão), a compatibilidade é plena. Caso haja rotinas de trimming no pipeline, as diretivas necessárias devem ser preservadas sem contornos arbitrários.

---

## 6. O que NÃO FAZER (Diretrizes Mandatórias de Segurança)

Para atender estritamente aos requisitos `REM-003`, `REM-005`, `REM-012`, `M3-AC-3`, `M3-AC-4` e `M3-AC-6`:

> [!CAUTION]
> **1. NUNCA aceitar Host Key sem confirmação explícita do usuário:**
> É terminantemente PROIBIDO implementar eventos de validação de chave de host com retorno incondicional `e.CanTrust = true`.
> Toda conexão inédita a um servidor SFTP deve apresentar ao usuário o fingerprint criptográfico da chave do host (SHA-256) em diálogo modal e exigir consentimento explícito (TOFU — *Trust On First Use*).
> Se a chave do host mudar em relação à previamente armazenada, a conexão DEVE ser sumariamente RECUSADA com aviso enfático de potencial ataque Man-in-the-Middle (MitM), exigindo ação deliberada do usuário para redefinir a confiança.

> [!CAUTION]
> **2. NUNCA guardar senhas ou passphrases em texto puro:**
> É terminantemente PROIBIDO armazenar senhas, frases-senha de chaves privadas ou credenciais de autenticação em texto legível, seja em arquivos de configuração, registro do Windows, JSON local ou variáveis globais estáticas desprotegidas.
> Qualquer credencial que necessite ser lembrada entre sessões deve ser persistida através do **Windows Credential Locker** (`Windows.Security.Credentials.PasswordVault`) ou protegida por **DPAPI** (`ProtectedData.Protect` com `DataProtectionScope.CurrentUser`).

> [!CAUTION]
> **3. NUNCA registrar senhas, chaves ou conteúdos de arquivos em logs:**
> Em conformidade com `NFR-003`, nenhuma credencial, passphrase, chave privada em formato PEM/OpenSSH ou conteúdo confidencial de arquivos transferidos pode ser emitido em logs (`App.Logger`, Sentry, EventViewer ou console).

---

## 7. Evidências e Referências

- **Especificação da Feature:** `docs/specs/F060-diferenciais-sftp-script-hex-split/spec.md` (M3 — SFTP).
- **Evidência de Análise Local:** `docs/agents/evidence/F060/m3-t0.md`.
- **Repositório Oficial SSH.NET:** [https://github.com/sshnet/SSH.NET](https://github.com/sshnet/SSH.NET)
- **Pacote NuGet SSH.NET:** [https://www.nuget.org/packages/SSH.NET/](https://www.nuget.org/packages/SSH.NET/)
- **Vulnerabilidades:**
  - NIST CVE-2022-29245: [https://nvd.nist.gov/vuln/detail/CVE-2022-29245](https://nvd.nist.gov/vuln/detail/CVE-2022-29245)
  - Terrapin Attack CVE-2023-48795: [https://terrapin-attack.com/](https://terrapin-attack.com/)
  - NIST CVE-2026-48798: [https://nvd.nist.gov/vuln/detail/CVE-2026-48798](https://nvd.nist.gov/vuln/detail/CVE-2026-48798)
- **Documentação FluentFTP sobre SFTP:** [https://github.com/robinrodricks/FluentFTP#faq](https://github.com/robinrodricks/FluentFTP#faq)
