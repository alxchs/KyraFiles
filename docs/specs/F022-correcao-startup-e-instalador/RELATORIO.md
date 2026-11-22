# Relatório de Conclusão e Aceite: SPEC-0019 (F022 — Correção do Crash de Startup e Estabilização do Instalador)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 3 pts  
**Evidências:** Inno Setup `installer/KyraFilesSetup.iss`, `AppLifecycleHelper.cs`, nota de release `2026-10-15_0205_build-KyraFilesSetup-4.2.37-baseline.md`

---

## 1. Escopo e Implementação
Resolução das falhas que derrubavam o processo após a instalação do pacote:
- Injeção de dependências completa em `AppLifecycleHelper.ConfigureHost()` para `IHelpService` e `HelpViewModel`.
- Script Inno Setup desinstalando instâncias prévias antes do registro MSIX para evitar conflito de `InstallLocation`.
- Configuração correta de atalhos em `[Icons]` e inicialização pós-instalação via alias registrado.
- Estabilização da inicialização do app permanecendo ativo sem encerramento silencioso.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | `IHelpService` e `HelpViewModel` registrados no contêiner de DI | `CONFIRMED` | Registrados em `AppLifecycleHelper.cs` |
| **AC-2** | Instalador Inno Setup realiza registro limpo sem falhas COM | `CONFIRMED` | `installer/KyraFilesSetup.iss` |
| **AC-3** | Atalhos abrem a aplicação normalmente via pacote WinUI 3 | `CONFIRMED` | Alias `kyrafiles.exe` ativo |
| **AC-4** | Janela inicializa estável e permanece aberta além dos 10 segundos | `CONFIRMED` | Validado no pipeline de deploy e testes |
