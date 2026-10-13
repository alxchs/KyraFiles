// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using Files.App.Data.Help;

namespace Files.App.Data.Help
{
	/// <summary>
	/// Central catalog containing all pre-authored help topics, human explanations, and differentiators.
	/// </summary>
	public static class HelpCatalog
	{
		private static readonly List<HelpTopic> _topics =
		[
			new HelpTopic(
				HelpContextId.General,
				"Visão Geral do Files Custom",
				"Introdução",
				HelpBadgeType.ExclusiveFeature,
				"Files Custom Edition",
				"Gerenciador de arquivos moderno, ultrarrápido e projetado para produtividade sem atrito.",
				"O Files Custom é uma evolução focada em eficiência do gerenciamento de arquivos no Windows.\n\n" +
				"Principais Destaques:\n" +
				"• Navegação por Colunas (Miller Columns): Estrutura contínua de pastas sem perder o caminho de vista.\n" +
				"• Substituição Segura do Explorer: Integrado ao Windows com fallback automático em qualquer falha.\n" +
				"• Redimensionamento Independente de Colunas: Cada coluna tem sua própria largura sem encolher as vizinhas.\n" +
				"• Abas e Painel Duplo: Abra quantas pastas precisar sem poluir a barra de tarefas.\n" +
				"• Barra de Menus Clássica: Acesso rápido por teclado (Alt+F, Alt+E, Alt+V, Alt+T, Alt+H).\n" +
				"• Renomeação em Lote Segura: Pré-visualização instantânea e detecção de nomes conflitantes.",
				new List<HelpShortcut>
				{
					new("F1", "Abrir Ajuda Contextual da tela atual"),
					new("Ctrl + T", "Nova Aba"),
					new("Ctrl + W", "Fechar Aba Atual"),
					new("Ctrl + Shift + E", "Alternar Painel Duplo (Dual Pane)"),
					new("Alt + F / E / V / T / H", "Acessar Menus Clássicos"),
				},
				new[] { "geral", "início", "bem-vindo", "introdução", "files custom", "atalhos" }
			),

			new HelpTopic(
				HelpContextId.ColumnsLayout,
				"Navegação em Colunas (Miller Columns)",
				"Layout e Navegação",
				HelpBadgeType.OneCommanderInspired,
				"Inspirado no OneCommander / Mac",
				"Navegue profundamente pela árvore de diretórios em colunas lado a lado, sem perder o contexto.",
				"O Modo Colunas permite inspecionar hierarquias inteiras de pastas de forma contínua e rápida.\n\n" +
				"Como funciona:\n" +
				"• Ao selecionar uma pasta, o conteúdo dela se abre na coluna imediatamente à direita.\n" +
				"• O histórico do caminho permanece visível nas colunas anteriores, permitindo voltar com um simples toque nas setas.\n" +
				"• Pré-visualização imediata de arquivos de texto, imagens e mídias na coluna final.\n\n" +
				"Dicas de Uso com Teclado:\n" +
				"• Seta para a Direita (→): Entra na pasta selecionada.\n" +
				"• Seta para a Esquerda (←): Retorna ao nível superior mantendo a seleção.\n" +
				"• Setas Cima/Baixo (↑/↓): Percorre os itens da coluna ativa rapidamente.",
				new List<HelpShortcut>
				{
					new("Ctrl + 5", "Ativar Modo Colunas"),
					new("Seta Direita (→)", "Expandir pasta na coluna seguinte"),
					new("Seta Esquerda (←)", "Voltar à coluna anterior"),
					new("Enter", "Abrir item selecionado"),
				},
				new[] { "colunas", "miller columns", "onecommander", "mac", "navegação", "pastas", "hierarquia" }
			),

			new HelpTopic(
				HelpContextId.DetailsLayout,
				"Modo Detalhes e Redimensionamento Independente",
				"Layout e Navegação",
				HelpBadgeType.ExplorerAdvantage,
				"Superior ao Windows Explorer",
				"Mais de 300 colunas da Shell do Windows e controle cirúrgico de largura sem empurrar as colunas vizinhas.",
				"No Windows Explorer convencional, redimensionar uma coluna frequentemente distorce ou esconde as outras colunas.\n\n" +
				"No Files Custom:\n" +
				"• Redimensionamento 100% Independente: Ajustar a coluna de Nome não altera as larguras de Data de Modificação, Tipo ou Tamanho.\n" +
				"• Catálogo Completo de Metadados: Acesso a todas as 300+ propriedades da Shell do Windows (Bitrate, Dimensões, Autor, Linhas, Câmera, Hash, etc.).\n" +
				"• Ordenação e Agrupamento Flexíveis: Agrupe por data, extensão ou pasta pai com cabeçalhos recolhíveis.",
				new List<HelpShortcut>
				{
					new("Ctrl + 1", "Modo Detalhes"),
					new("Duplo Clique no Separador", "Ajustar coluna ao conteúdo"),
					new("Clique com Botão Direito no Cabeçalho", "Escolher colunas visíveis"),
				},
				new[] { "detalhes", "colunas", "largura", "tamanho", "propriedades", "metadados", "explorer" }
			),

			new HelpTopic(
				HelpContextId.TabsAndPanes,
				"Abas Inteligentes e Painel Duplo (Dual Pane)",
				"Organização",
				HelpBadgeType.OneCommanderInspired,
				"Produtividade Avançada",
				"Trabalhe com múltiplas pastas na mesma janela sem alternar janelas soltas na barra de tarefas.",
				"O gerenciamento de abas e painéis foi desenhado para operações frequentes de movimentação e cópia de arquivos.\n\n" +
				"Recursos Principais:\n" +
				"• Painel Duplo Horizontal ou Vertical: Visualize duas pastas lado a lado para arrastar ou comparar itens.\n" +
				"• Duplicação de Abas: Duplique instantaneamente sua aba atual mantendo o histórico de navegação.\n" +
				"• Reabrir Aba Fechada: Recupera a última pasta fechada acidentalmente.\n" +
				"• Arraste entre Abas: Arraste arquivos sobre o título de outra aba para soltá-los dentro dela.",
				new List<HelpShortcut>
				{
					new("Ctrl + T", "Nova Aba"),
					new("Ctrl + K", "Duplicar Aba Atual"),
					new("Ctrl + Shift + T", "Reabrir Aba Fechada"),
					new("Ctrl + Shift + E", "Alternar Painel Duplo Lado a Lado"),
					new("Ctrl + Tab", "Próxima Aba"),
				},
				new[] { "abas", "painel duplo", "dual pane", "split", "duplicar", "organização" }
			),

			new HelpTopic(
				HelpContextId.BulkRename,
				"Renomeação em Lote Segura (Bulk Rename)",
				"Organização",
				HelpBadgeType.ExclusiveFeature,
				"Recurso Exclusivo",
				"Renomeie dezenas de arquivos de uma só vez com proteção contra nomes duplicados e erros.",
				"A ferramenta de renomeação em lote do Files Custom permite aplicar padrões ordenados e substituições de texto com segurança total.\n\n" +
				"Recursos:\n" +
				"• Pré-visualização Instantânea: Veja o resultado antes de confirmar a alteração no disco.\n" +
				"• Sequência Numérica Automática: Insira numeração sequencial mantendo ou alterando a extensão original.\n" +
				"• Detecção de Conflitos: Alerta imediato se o novo padrão colidir com arquivos já existentes na mesma pasta.\n" +
				"• Preservação de Extensão: Altere o nome sem perder a associação de formato do arquivo.",
				new List<HelpShortcut>
				{
					new("F2", "Renomear Item Selecionado"),
					new("Ctrl + R / Menu Contexto", "Renomear em Lote (Múltiplos Itens)"),
				},
				new[] { "renomear", "lote", "bulk rename", "arquivos", "nomes", "extensão" }
			),

			new HelpTopic(
				HelpContextId.ClassicMenuBar,
				"Barra de Menus Clássica (Alt+F, Alt+V, etc.)",
				"Interface e Produtividade",
				HelpBadgeType.ExplorerAdvantage,
				"Diferencial vs Explorer Moderno",
				"A familiaridade da barra de menus clássica de volta, com aceleração instantânea por teclado.",
				"O Windows Explorer moderno removeu a barra de menus tradicional, obrigando o uso de menus suspensos escondidos.\n\n" +
				"No Files Custom:\n" +
				"• Menus Completos: Arquivo, Editar, Exibir, Ferramentas e Ajuda acessíveis com uma única tecla Alt.\n" +
				"• Acesso por Teclas de Acesso (Access Keys): Pressione Alt e em seguida a letra sublinhada (ex: Alt+F, N para nova pasta).\n" +
				"• Integração de Ferramentas de Desenvolvedor: Git, Terminal com privilégios de Administrador, VS Code e Bloco de Notas acessíveis diretamente no menu.",
				new List<HelpShortcut>
				{
					new("Alt + F", "Menu Arquivo"),
					new("Alt + E", "Menu Editar"),
					new("Alt + V", "Menu Exibir"),
					new("Alt + T", "Menu Ferramentas"),
					new("Alt + H", "Menu Ajuda"),
				},
				new[] { "menu", "barra de menus", "menubar", "alt", "clássico", "atalhos" }
			),

			new HelpTopic(
				HelpContextId.SearchEngine,
				"Índice de Busca Instantânea e Filtros Avançados",
				"Busca e Localização",
				HelpBadgeType.ExplorerAdvantage,
				"Superior ao Windows Explorer",
				"Índice próprio em memória (< 100 ms), sem duplicatas por subst/junções e com operadores avançados.",
				"Diferente da busca do Windows Explorer que trava ou demora minutos:\n\n" +
				"• Índice Próprio Instantâneo (F015): Funciona no Modo Básico sem administrador (snapshots em disco + ReadDirectoryChangesExW) e no Modo Completo (leitura direta da MFT + USN Journal).\n" +
				"• Busca Sem Acentos e Sem Duplicatas: Digitar 'acao' encontra 'Ação' e 'AÇÃO' (NFC/NFD), e unidades subst, junções, links simbólicos e hardlinks nunca geram resultados repetidos.\n" +
				"• Operadores Avançados: Use ext:cs;ts, size:>10mb, dm:today, dc:thisweek, type:folder, type:file, path:src\\, re:<regex> e case:Abc.\n" +
				"• Tecla F3 para Busca de Conteúdo Externa: Acione Agent Ransack ou Everything diretamente na pasta atual.",
				new List<HelpShortcut>
				{
					new("Ctrl + F", "Focar na Caixa de Busca Instantânea"),
					new("F3", "Disparar Busca Externa na Pasta Atual"),
					new("Esc", "Cancelar Busca em Andamento"),
				},
				new[] { "busca", "pesquisar", "índice", "mft", "usn", "f3", "agent ransack", "everything", "encontrar" }
			),

			new HelpTopic(
				HelpContextId.ExplorerReplacement,
				"Substituição Transparente do Windows Explorer",
				"Sistema e Integração",
				HelpBadgeType.ExclusiveFeature,
				"Inovação Exclusiva",
				"O Files Custom assume Win+E e duplo clique em pastas, com segurança de fallback automático.",
				"Você pode usar o Files Custom como o gerenciador de arquivos padrão do Windows sem nenhum risco de quebrar o sistema operacional.\n\n" +
				"Como funciona a proteção:\n" +
				"• Interceptação Limpa: O atalho Win+E e a abertura de pastas a partir de outros programas (Chrome, VS Code, etc.) abrem diretamente no Files Custom.\n" +
				"• Fallback Automático: Caso o Files Custom seja encerrado ou indisponível, qualquer comando de pasta é imediatamente repassado ao Explorer nativo da Microsoft, sem telas de erro ou arquivos não encontrados.\n" +
				"• Ativação em 1 Clique: Pode ser ativado ou desativado a qualquer momento na página de Configurações do app.",
				new List<HelpShortcut>
				{
					new("Win + E", "Abrir Files Custom (quando ativado como padrão)"),
				},
				new[] { "substituir explorer", "padrao", "win+e", "sistema", "fallback", "resiliencia" }
			),

			new HelpTopic(
				HelpContextId.SettingsAndCustomization,
				"Personalização, Fontes e Densidade Compacta",
				"Configurações",
				HelpBadgeType.ExplorerAdvantage,
				"Personalização Completa",
				"Ajuste fontes independentes, espaçamento compacto de itens e paletas de cores modernas.",
				"Adapte o Files Custom ao seu estilo visual e resolução de monitor:\n\n" +
				"• Densidade Compacta: Ideal para exibir dezenas de arquivos sem rolagem excessiva em monitores menores.\n" +
				"• Fontes Personalizadas: Escolha fontes dedicadas para a interface e para a lista de arquivos.\n" +
				"• Temas do Windows: Suporte completo a Modo Claro, Escuro e Contraste Alto nativos do WinUI 3.\n" +
				"• Barra Lateral Pessoal: Fixe suas unidades, pastas de projetos e favoritos.",
				new List<HelpShortcut>
				{
					new("Ctrl + ,", "Abrir Configurações"),
				},
				new[] { "configurações", "aparência", "fontes", "densidade", "compacto", "tema", "personalização" }
			),

			new HelpTopic(
				HelpContextId.KeyboardShortcuts,
				"Tabela Completa de Atalhos de Teclado",
				"Produtividade",
				HelpBadgeType.OneCommanderInspired,
				"Guia de Produtividade",
				"Controle todas as ações do gerenciador sem tirar as mãos do teclado.",
				"Lista consolidada dos principais atalhos para aceleração de trabalho diário:\n\n" +
				"• F1: Ajuda Contextual da tela atual\n" +
				"• Ctrl+T: Nova Aba\n" +
				"• Ctrl+W: Fechar Aba\n" +
				"• Ctrl+K: Duplicar Aba\n" +
				"• Ctrl+Shift+E: Alternar Painel Duplo\n" +
				"• Ctrl+1 / 2 / 3 / 4 / 5: Alternar modos de exibição (Detalhes, Blocos, Grade, Ícones, Colunas)\n" +
				"• F2: Renomear arquivo\n" +
				"• F3: Busca Externa Avançada na Pasta\n" +
				"• F5 / Ctrl+R: Atualizar pasta\n" +
				"• Shift+Del: Excluir permanentemente sem enviar para a Lixeira\n" +
				"• Alt+Enter: Propriedades do item\n" +
				"• Alt+F / E / V / T / H: Barra de Menus Clássica",
				new List<HelpShortcut>
				{
					new("F1", "Ajuda Contextual"),
					new("Ctrl + 1..5", "Modos de Visualização"),
					new("Ctrl + T / W", "Gerenciar Abas"),
					new("Ctrl + Shift + E", "Painel Duplo"),
					new("F2 / F3 / F5", "Renomear / Buscar / Atualizar"),
				},
				new[] { "atalhos", "teclado", "produtividade", "hotkeys", "rapido" }
			)
		];

		public static IReadOnlyList<HelpTopic> AllTopics => _topics.AsReadOnly();

		public static HelpTopic GetTopic(HelpContextId contextId)
		{
			var found = _topics.FirstOrDefault(t => t.ContextId == contextId);
			return found ?? _topics[0];
		}
	}
}

