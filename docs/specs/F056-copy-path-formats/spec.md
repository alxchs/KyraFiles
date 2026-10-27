# F056 — Copiar caminho em vários formatos — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Hoje há "Copiar caminho" e "Copiar caminho com aspas". Quem desenvolve precisa do mesmo caminho para o WSL, para JSON, para C#, como URL de arquivo ou relativo ao repositório; escapar à mão gera erro.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero um submenu "Copiar caminho como…" com os formatos mais usados.
- **US2 (P2)** Como desenvolvedor, quero escolher quais formatos aparecem e o atalho do mais usado.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | arquivo `C:\desenv\x y\a.txt` | "Windows" | copia `C:\desenv\x y\a.txt` |
| AC-2 | mesmo arquivo | "WSL" | copia `/mnt/c/desenv/x y/a.txt` |
| AC-3 | mesmo arquivo | "JSON" | copia `"C:\\desenv\\x y\\a.txt"` (escapado, entre aspas) |
| AC-4 | mesmo arquivo | "C# literal" | copia `@"C:\desenv\x y\a.txt"` |
| AC-5 | mesmo arquivo | "URL de arquivo" | copia `file:///C:/desenv/x%20y/a.txt` |
| AC-6 | arquivo dentro de um repositório git | "Relativo ao repositório" | copia `x y/a.txt` com barras `/` |
| AC-7 | vários arquivos selecionados | qualquer formato | copia um por linha, na ordem exibida |
| AC-8 | caminho de rede `\\srv\pasta\a.txt` | "WSL" | gera o caminho UNC equivalente ou avisa que não há; nunca copia um caminho errado calado |

## 4. Requisitos
- **FR-001** Formatos: Windows, WSL, JSON, literal C#, URL de arquivo, relativo ao repositório, relativo à pasta atual.
- **FR-002** Reaproveitar as ações "Copiar caminho" existentes; não duplicar.
- **NFR-001** Sem atraso perceptível ao abrir o menu (medido).

## 5. Casos de borda
- Acentos e caracteres especiais; caminho longo; unidade não mapeada no WSL; pasta fora de repositório (opção fica desabilitada).

## 6. Fora de escopo
- Formatos definidos por script do usuário (vão em F045).

## 7. Critérios de sucesso
- Colar o caminho no destino certo na primeira tentativa, sem editar à mão.

## 8. Pontos em aberto
- Nenhum. Pronta para aprovação.

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
