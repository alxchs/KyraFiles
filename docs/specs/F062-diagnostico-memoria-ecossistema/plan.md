# F062 — Diagnóstico de Consumo de Memória do Ecossistema na Página Sobre — PLAN

Status: **RASCUNHO PARA APROVAÇÃO**
Dependência: `spec.md` aprovada.

---

## 1. Arquitetura e Componentes

### 1.1 Modelo de Dados (`KyraFiles.Shared/Helpers/ProcessMemoryReport.cs`)
Criar classe imutável de resultado em `KyraFiles.Shared` (testável via unit tests puros):
```csharp
public sealed record ProcessMemoryItem(
    string ProcessName,
    int Id,
    long WorkingSetBytes,
    long PrivateBytes,
    bool IsRunning
);

public sealed record EcosystemMemoryReport(
    DateTimeOffset Timestamp,
    IReadOnlyList<ProcessMemoryItem> Items,
    long TotalWorkingSetBytes,
    long TotalPrivateBytes
);
```

### 1.2 Provedor de Diagnóstico (`KyraFiles.Shared/Helpers/EcosystemMemoryDiagnostics.cs`)
Método estático puro e thread-safe:
- Varre os nomes alvos: `Files`, `KyraFiles.App.Server`, `KyraFiles.HotkeyService`, `KyraFiles.Watchdog`, `KyraFiles.App.IndexService`.
- Para cada processo, captura `WorkingSet64` e `PrivateMemorySize64` dentro de bloco `try/catch` para evitar corridas de processo saindo.
- Agrupa por processo e calcula os totais consolidados.
- Gera string formatada para área de transferência.

### 1.3 ViewModel (`AboutViewModel.cs`)
- Adicionar propriedades observáveis:
  - `EcosystemMemorySummary`: string formatada do total (ex: "Consumo total: 184,2 MB (3 processos ativos)").
  - `IsLoadingMemory`: indicador de carregamento.
- Comandos:
  - `RefreshMemoryUsageCommand`: dispara a coleta em background.
  - `ShowMemoryDetailsCommand`: abre diálogo `ContentDialog` com a discriminação em lista.
  - `CopyMemoryReportCommand`: copia para o clipboard.

### 1.4 Interface XAML (`AboutPage.xaml`)
- Inserir um `SettingsCard` na seção `App Info` da `AboutPage.xaml`:
  - Header: `"Ecosystem memory usage"` / `"Consumo de memória do ecossistema"`
  - Description: `x:Bind ViewModel.EcosystemMemorySummary`
  - HeaderIcon: `FontIcon Glyph="&#xE9D9;"` (ícone de diagnóstico/desempenho)
  - Botão de detalhes com Flyout ou Diálogo detalhado.

---

## 2. Testes Unitários (`KyraFiles.App.Search.Index.Tests` ou `KyraFiles.App.UnitTests`)
- `EcosystemMemoryDiagnosticsTests.cs`:
  - Teste de formatação de bytes (`FormatBytes` para KB, MB, GB).
  - Teste de cálculo de totais a partir de lista simulada de processos.
  - Teste de geração de relatório de texto para cópia.
  - Teste de tratamento de processo ausente / inativo.

---

## 3. Riscos e Mitigações
- **Processo encerrando durante a consulta:** Protegido com tratamento explícito de exceções Win32 / `InvalidOperationException`.
- **Custo de CPU na página Sobre:** A medição roda sob demanda (ou uma única vez em background ao abrir a página), sem timer de polling contínuo para respeitar a contenção de recursos do projeto (F021).

