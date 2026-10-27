# F046 — Busca profunda na rede — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Em pastas compartilhadas e NAS, a busca é lenta ou incompleta: o Everything só percebe mudanças feitas deste PC, o servidor de índice compartilhado dele foi removido e a busca do Windows em NAS é muito lenta. Quem trabalha com servidor de arquivos precisa achar arquivos na rede de forma confiável.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero buscar por nome numa pasta de rede e ver resultados enquanto a busca ainda anda.
- **US2 (P1)** Como usuário, quero cancelar a qualquer momento e que um servidor lento ou desligado não trave a busca nos outros.
- **US3 (P2)** Como usuário, quero buscar em "todas as pastas compartilhadas do servidor X" e descobrir servidores da rede local.
- **US4 (P2)** Como usuário, quero que a repetição da busca seja rápida (o app lembra o que não mudou).

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | pasta de rede com 50 mil arquivos | busco um nome | o primeiro resultado aparece antes de a busca terminar, com indicador de progresso |
| AC-2 | busca em andamento | "Cancelar" | para em até 1 s e mantém o que achou |
| AC-3 | dois servidores no escopo e um desligado | busco | o desligado tem tempo limite próprio (configurável), é listado como "sem resposta" e o outro retorna normalmente |
| AC-4 | escopo "todas as pastas compartilhadas de X" | busco | o app varre as pastas a que tenho acesso e cada resultado mostra de qual compartilhamento veio |
| AC-5 | pasta sem permissão | busco | é ignorada e contada como "inacessível"; nunca mostra nome de arquivo que não posso abrir |
| AC-6 | repito a mesma busca na mesma pasta | busco de novo | termina mais rápido que a primeira vez, e detecta arquivo criado remotamente depois |
| AC-7 | filtros (extensão, tamanho, data, profundidade) | aplico | resultados respeitam todos |

## 4. Requisitos
- **FR-001** Resultados incrementais (streaming), nunca só ao final.
- **FR-002** Tempo limite por servidor e limite de concorrência para não saturar a rede.
- **FR-003** Respeitar as permissões do usuário atual.
- **FR-004** Índice por compartilhamento opcional, com validade e botão "Atualizar agora".
- **NFR-001** CPU e memória estáveis em varredura de 1 milhão de arquivos (medido); a baseline vem do brief `R-rede-baseline`.

## 5. Casos de borda
- Caminho UNC com mais de 260 caracteres; unidade mapeada que mudou de servidor; VPN que cai no meio; nomes com acento.

## 6. Fora de escopo
- Busca por conteúdo na rede (decidir abaixo); servidor de índice próprio.

## 7. Critérios de sucesso
- Achar um arquivo por nome num NAS de 1 milhão de arquivos em tempo medido e menor que a busca do Windows (comparação com os mesmos scripts).

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: guardar índice de rede em disco (acelera, mas fica desatualizado)? Recomendação: sim, opcional e desligado por padrão.]
- [DECIDIDO 24/10/2026, vale a recomendação: busca por conteúdo na rede entra? Recomendação: não nesta versão.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude). Condicionada à medição `R-rede-baseline` (ainda não feita).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
