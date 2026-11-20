# F072 — Documentação Local Offline Multilíngue — PLAN

Status: **APROVADO PARA EXECUÇÃO**  
Metodologia: Spec-Driven Development (`METODO.md` §6)

---

## 1. Arquitetura e Estrutura de Arquivos

### 1.1 Estrutura de Pastas em `src/KyraFiles.App/Docs/`
```text
src/KyraFiles.App/Docs/
├── index.html                   (Redirecionador raiz por navigator.language)
├── pt-BR/                       (Português do Brasil)
│   ├── index.html
│   ├── getting-started/
│   │   └── overview.html
│   └── features/
│       ├── tabs.html
│       ├── dual-pane.html
│       ├── layout.html
│       ├── command-palette.html
│       ├── git.html
│       ├── tags.html
│       └── files-custom.html
├── en-US/                       (Inglês)
│   ├── index.html
│   ├── getting-started/
│   │   └── overview.html
│   └── features/
│       └── ... (7 guias em inglês)
├── es-ES/                       (Espanhol)
│   ├── index.html
│   ├── getting-started/
│   │   └── overview.html
│   └── features/
│       └── ... (7 guias em espanhol)
└── de-DE/                       (Alemão)
    ├── index.html
    ├── getting-started/
    └── features/
        └── ... (7 guias em alemão)
```

### 1.2 Atualização do `DocumentationHelper.cs`
- Adicionar detecção de cultura via método `GetCurrentCultureCode()`:
  - Analisa `ApplicationLanguages.PrimaryLanguageOverride`, `CultureInfo.CurrentUICulture` e `CultureInfo.CurrentCulture`.
  - Normalização para os 4 códigos suportados:
    * `pt*` -> `"pt-BR"`
    * `es*` -> `"es-ES"`
    * `de*` -> `"de-DE"`
    * Qualquer outro -> `"en-US"`
- Ordem de resolução de caminhos candidatos:
  1. `<root>/Docs/<idioma-ativo>/<subPath>`
  2. `<root>/Docs/<idioma-ativo>/index.html`
  3. `<root>/Docs/pt-BR/<subPath>` (fallback primário local)
  4. `<root>/Docs/en-US/<subPath>` (fallback secundário local)
  5. `<root>/Docs/<subPath>` (legado/raiz)
  6. Fallback final para web: `https://github.com/alxchs/KyraFiles`

### 1.3 Seletor de Idiomas no Menu Lateral dos HTMLs
Cada arquivo HTML ganha um componente no topo da `<aside>`:
```html
<div class="lang-selector">
  <a href="../../pt-BR/getting-started/overview.html" class="lang-btn active">PT</a>
  <a href="../../en-US/getting-started/overview.html" class="lang-btn">EN</a>
  <a href="../../es-ES/getting-started/overview.html" class="lang-btn">ES</a>
  <a href="../../de-DE/getting-started/overview.html" class="lang-btn">DE</a>
</div>
```

---

## 2. Testes de Unidade
Atualizar `tests/KyraFiles.App.Search.Index.Tests/DocumentationHelperTests.cs` para testar:
- Resolução com cultura `pt-BR` -> abre pasta `pt-BR`
- Resolução com cultura `en-US` -> abre pasta `en-US`
- Resolução com cultura `es-ES` -> abre pasta `es-ES`
- Resolução com cultura `de-DE` -> abre pasta `de-DE`
- Fallback para outro idioma quando arquivo não existe no idioma requisitado

---

## 3. Desempenho e Requisitos F021
- Todos os arquivos HTML são estáticos, sem JS pesado, sem requisições de rede.
- Cópia no build com `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` em `KyraFiles.App.csproj`.
- Verificação F021 obrigatória com CPU em repouso < 5% e sem IndexService.

