# F009 - Fonte das Áreas Fixas Separada da Fonte dos Resultados

> **Nota de Substituição (08/11/2026):** Esta especificação foi substituída pela **F089** (`docs/specs/F089-fontes-nao-aplicadas/`).
> A implementação original da F009 armazenava strings em vez de instâncias de `FontFamily` nos recursos do WinUI 3 e a herança de estilos nos layouts era sobrescrita por `TextBlockStyles.xaml`. A F089 corrige a raiz desses problemas em `AppResourcesService`, nos 3 layouts (`DetailsLayoutPage`, `GridLayoutPage`, `ColumnLayoutPage`) e em `DataGridHeader`.
