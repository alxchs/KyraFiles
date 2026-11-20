# Plano Técnico — F081: Identidade KyraFiles

## 1. Arquitetura e Estrutura Técnica

### 1.1 Migração de Dados do Usuário (Sub-spec B)
- **Caminho de Armazenamento Atual**:
  - `%LOCALAPPDATA%\KyraFiles\` ou pasta local de configurações (`LocalCache` / `LocalState`).
  - Arquivos principais: `user_settings.json`, histórico de navegação, favoritos e snapshots de volume.
- **Estratégia de Migração Segura**:
  - Implementar um componente de serviço `UserSettingsMigrationService` acionado na inicialização (antes da carga final de configurações).
  - Se a nova pasta de dados do KyraFiles não contiver `user_settings.json`, verificar a existência de `user_settings.json` na pasta de dados anterior (`KyraFiles` / `Files`).
  - Copiar recursivamente de forma atômica e não-destrutiva os arquivos de configuração para a pasta `KyraFiles`, mantendo intacta a pasta anterior (backup implícito).
  - Registrar no log operacional a data e o status da migração.

### 1.2 Atualização Textual e Uniformidade da Marca (Sub-spec A e C)
- **Arquivos de Recursos Localizados (`.resw`)**:
  - Atualizar strings em `pt-BR`, `en-US`, `es-ES`, `de-DE` substituindo menções visíveis de "KyraFiles" por "KyraFiles".
  - Garantir que a tela Sobre (About) apresente:
    - Nome do produto: **KyraFiles**
    - Versão e carimbo de data/hora oficial
    - Seção de créditos mantendo expressamente:
      - `Baseado no projeto open-source KyraFiles contributors (Licença MIT)`
      - `Copyright (c) KyraFiles contributors e Contribuidores`
- **Catálogo de Ajuda e Documentação Offline**:
  - Atualizar `HelpCatalog.cs` para referenciar o ecossistema KyraFiles.
  - Atualizar os títulos e cabeçalhos em `src/KyraFiles.App/Docs/` (`pt-BR`, `en-US`, `es-ES`, `de-DE`).

### 1.3 Paleta Visual e Diretrizes Institucionais (Sub-spec A)
- **Cores Oficiais**:
  - Primária: Azul Noite / Profundo (`#0F172A` / `#1E293B`)
  - Acento Principal: Azul Ciano Eletro (`#0284C7` / `#38BDF8`)
  - Tons Neutros: Ardósia / Grafite (`#334155` / `#64748B` / `#F8FAFC`)
  - Proibição absoluta de vermelho, amarelo ou símbolos avermelhados.

### 1.4 Ícones e Logo (Sub-spec D)
- Marcada como `WAITING`. Os recursos de imagem existentes são preservados provisoriamente até que a arte oficial do KyraDB seja disponibilizada pelo usuário.

## 2. Riscos e Mitigações
- **Risco 1: Corrupção durante a migração de configurações**:
  - *Mitigação*: Cópia atômica (leitura -> validação do JSON -> gravação no novo local). O diretório original nunca é apagado ou renomeado.
- **Risco 2: Quebra de caminhos de atalhos e protocolo**:
  - *Mitigação*: Suporte a fallback seguro no lançador e nas rotas de inicialização.

