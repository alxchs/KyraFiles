# Especificação — F081: Identidade KyraFiles

## 1. Contexto e Motivação
O aplicativo evoluiu significativamente com melhorias de estabilidade (CrashGuard), velocidade, busca de alta performance, login do GitHub descentralizado, verificação de integridade criptográfica e documentação offline multilíngue. O nome oficial do produto passa a ser **KyraFiles**, consolidando a família de utilitários do ecossistema.

A transição de identidade exige consistência absoluta na apresentação visual e textual, garantindo que usuários existentes não percam suas configurações, abas, preferências ou personalizações durante a migração, e que a nova marca transmita solidez, modernidade e excelência operacional.

## 2. Escopo e Sub-Especificações
- **Sub-spec A: Decisão de Paleta e Apresentação da Marca**:
  - Padronização do nome do produto como **KyraFiles** em todas as interfaces, diálogos, telas Sobre, cabeçalhos, dicas e documentação.
  - Definição da paleta de cores institucional sóbria, elegante e sem qualquer presença de vermelho ou combinações de vermelho com amarelo/estrela amarela (em conformidade estrita com as preferências globais do projeto).
- **Sub-spec B: Identidade de Pacote e Migração Transparente de Configurações**:
  - Criação de mecanismo de migração automática: ao iniciar sob a nova identidade, se a pasta/perfil de configurações de dados do usuário anterior for detectada, importar e preservar integralmente o estado, histórico, favoritos, temas e abas salvas sem intervenção manual.
- **Sub-spec C: Atualização Textual e Manutenção de Créditos**:
  - Atualização dos textos visíveis na interface e strings de recursos em todos os idiomas suportados (Português, Inglês, Espanhol, Alemão).
  - Preservação estrita dos avisos originais de copyright e termos da licença MIT (`Copyright (c) Files Community`).
- **Sub-spec D: Logotipo e Ícones**:
  - `WAITING`: Aguardando a arte oficial do KyraDB a ser fornecida pelo Alexandre. Nenhum ícone inventado será gerado.

## 3. Critérios de Aceite (AC)
- **AC-1 (Migração Transparente)**: Ao executar o KyraFiles pela primeira vez em um ambiente com configurações anteriores, todas as preferências (tema, layout, pastas fixadas, configurações avançadas) são migradas sem perda de dados.
- **AC-2 (Uniformidade da Nova Marca)**: Todos os textos visíveis ao usuário final (título da janela, tela Sobre, menus, caixas de diálogo, notificações e documentação) apresentam o nome **KyraFiles**, sem resquícios do nome intermediário anterior.
- **AC-3 (Preservação de Direitos e Licença)**: O aviso de copyright e a licença MIT em favor do Files Community permanecem intactos nos locais regulamentares e na tela de créditos/licença.
- **AC-4 (Paleta e Identidade Visual Conforme Diretrizes)**: A paleta e elementos visuais não contêm tons vermelhos, combinações de vermelho com amarelo ou símbolos indesejados. Cores base: tons modernos de azul profundo, grafite/ardósia e acentos neutros/ciano.
- **AC-5 (Integridade da Compilação e Recursos)**: A compilação Release é concluída com 0 erros, sem regressão de inicialização ou aumento de consumo de memória.

