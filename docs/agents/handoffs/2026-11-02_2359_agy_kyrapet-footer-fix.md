# Handoff: Ajuste visual do mascote KyraFiles no rodapé das Configurações

Data: 02/11/2026
Autora: AGY
Branch: `feature/kyrapet-settings-footer`

## Contexto e Ajuste Realizado

O usuário solicitou que a mascote/desenho que representa o aplicativo (`KyraLogo.png`) fosse posicionado no rodapé da barra lateral de Configurações, conforme o modelo fornecido, tomando o cuidado estrito para não cortar as extremidades da arte.

### Diagnóstico Técnico das Imagens
- `KyraPet.png` (1000x1000): Possui apenas 2 pixels de margem na base e 16 pixels no topo, sofrendo corte se emoldurado diretamente em containers com cantos arredondados.
- `KyraLogo.png` (1254x1254): Arte completa original contendo o pet (schnauzer), o círculo tecnológico e a tipografia `KyraFiles`, com margens equilibradas (56px no topo, 116px na base e ~180px nas laterais).

### Implementação
1. **Footer do `SidebarView` em `SettingsPage.xaml`:**
   - Adicionado `<controls:SidebarView.Footer>` ao `SettingsSidebar`.
   - Criado container `KyraPetFooterCard` (`Border`) com `CornerRadius="8"`, `Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"`, `BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"` e `BorderThickness="1"`.
   - Imagem configurada com `Width="200"`, `Height="200"`, `Stretch="Uniform"` e `Padding="4"`, garantindo proporção 1:1 perfeita sem corte de bordas, orelhas ou tipografia.
2. **Adaptação Responsiva aos Estados Visuais:**
   - `ExpandedSidebarState`: `KyraPetFooterCard.Visibility = Visible`.
   - `CompactSidebarState`: `KyraPetFooterCard.Visibility = Collapsed` (para não vazar nem sofrer corte na coluna estreita de 48px).

### Verificação
- **Build da Solução:** `mkfile d KyraFiles.slnx` concluído com sucesso (0 erros).
- **Testes Unitários:** 194 testes executados (193 aprovados, 1 ignorado temporariamente, 0 falhas).
- **Commit:** `cbe79ef24` em `feature/kyrapet-settings-footer`.
