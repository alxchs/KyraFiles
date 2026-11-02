# F072 — Documentação Local Offline Multilíngue — SPEC

Status: **APROVADA PARA EXECUÇÃO**  
Metodologia: Spec-Driven Development (`METODO.md` §6)  
Origem: Solicitação do usuário em 31/10/2026 (suporte aos 4 idiomas oficiais da documentação: Português-BR, Inglês, Espanhol e Alemão)

---

## 1. Por quê

O Files Custom atende a usuários em diferentes regiões e idiomas. Embora o aplicativo possua tradução para dezenas de idiomas em sua interface WinUI, a documentação local offline (F068) foi inicialmente disponibilizada apenas em Português do Brasil (pt-BR).

Para oferecer uma experiência completa, inclusiva e com excelência operacional:
- A documentação local offline precisa estar disponível nos **4 idiomas principais**:
  1. **Português do Brasil (`pt-BR`)**
  2. **Inglês (`en-US`)**
  3. **Espanhol (`es-ES`)**
  4. **Alemão (`de-DE`)**
- O aplicativo deve detectar o idioma atualmente configurado no app ou no Windows e abrir diretamente a documentação correspondente.
- Cada página da documentação HTML offline deve conter um seletor de idiomas interativo no cabeçalho ou menu lateral, permitindo alternar instantaneamente entre os 4 idiomas mantendo a mesma página/seção ativa (ex.: de `pt-BR/features/tabs.html` para `en-US/features/tabs.html`).
- Se um determinado arquivo não existir no idioma requisitado, o sistema recorre ao fallback gracioso ordenado: `idioma configurado` -> `pt-BR` -> `en-US` -> `web`.

---

## 2. Histórias de Usuário

- **US1 (Abertura no Idioma do App):** Como usuário com o Files configurado em Espanhol (ou Alemão, Inglês, Português), ao clicar em "Documentação" no menu ou tela Sobre, quero que o navegador abra diretamente o guia no meu idioma.
- **US2 (Alternância Fácil de Idiomas):** Como usuário navegando na documentação, quero poder clicar nas opções de idioma (PT | EN | ES | DE) na barra lateral para trocar de idioma instantaneamente sem perder a página em que estou lendo.
- **US3 (Resiliência e Fallback):** Como usuário, se alguma página específica estiver ausente no meu idioma, quero que o sistema abra a versão correspondente em Português ou Inglês sem erros de página em branco ("404").
- **US4 (Zero Dependência de Internet):** Toda a navegação e alternância entre os 4 idiomas deve funcionar 100% offline, sem carregar fontes, scripts ou estilos externos.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Cenário | Dado | Quando | Então |
| :--- | :--- | :--- | :--- | :--- |
| **AC-1** | **Abertura em Português** | O app está configurado em Português (`pt-BR`) | O usuário clica em Documentação | O arquivo local `Docs/pt-BR/getting-started/overview.html` é aberto. |
| **AC-2** | **Abertura em Inglês** | O app está configurado em Inglês (`en-US`) | O usuário clica em Documentação | O arquivo local `Docs/en-US/getting-started/overview.html` é aberto. |
| **AC-3** | **Abertura em Espanhol** | O app está configurado em Espanhol (`es-ES` / `es-419`) | O usuário clica em Documentação | O arquivo local `Docs/es-ES/getting-started/overview.html` é aberto. |
| **AC-4** | **Abertura em Alemão** | O app está configurado em Alemão (`de-DE`) | O usuário clica em Documentação | O arquivo local `Docs/de-DE/getting-started/overview.html` é aberto. |
| **AC-5** | **Seletor de idioma na página** | O usuário está visualizando a página de Abas em `pt-BR/features/tabs.html` | Clica no botão `EN` no topo da barra lateral | É redirecionado imediatamente para `en-US/features/tabs.html`. |
| **AC-6** | **Raiz da documentação (Docs/index.html)** | O usuário abre `Docs/index.html` diretamente | A página é carregada | O script local detecta o `navigator.language` do navegador e redireciona para a pasta do idioma correspondente (`pt-BR`, `en-US`, `es-ES` ou `de-DE`). |
| **AC-7** | **Testes de unidade** | O resolvedor `DocumentationHelper` recebe diferentes culturas (`pt`, `en`, `es`, `de`) | A resolução de arquivo é executada | Cada cultura resolve para sua respectiva pasta no disco. |

