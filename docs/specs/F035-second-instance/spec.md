# F035 — Segunda instância e abertura direta — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui: isso é do `plan.md`.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Abrir o Files duas vezes seguidas (atalho, "Abrir com", clique em pasta) às vezes mata a segunda instância com erro e o pedido se perde (a pasta pedida não abre). Registrado 5 vezes em 18/10/2026 no log de eventos do Windows (`SemaphoreFullException`). Também: abrir o `Files.exe` direto pelo arquivo, fora do pacote, morre sem aviso (`REGDB_E_CLASSNOTREG`, 17 vezes de 16 a 23/10).

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero que abrir uma pasta pelo Files duas ou mais vezes seguidas sempre leve a pasta pedida para a janela aberta, para nunca perder um clique.
- **US2 (P2)** Como usuário, quero que abrir o programa pelo arquivo errado se corrija sozinho (abrindo pelo caminho certo) ou explique o que fazer, em vez de sumir calado.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | Files aberto | disparo 5 aberturas de 5 pastas diferentes com menos de 1 s entre elas | as 5 pastas aparecem (abas) e nenhum processo termina com exceção |
| AC-2 | Files fechado | disparo 2 aberturas seguidas de pastas diferentes | uma janela abre e mostra as 2 pastas |
| AC-3 | uma janela de diálogo de arquivo ativa | abro uma pasta | o diálogo continua como diálogo e a pasta abre na janela principal |
| AC-4 | Files aberto | executo o `Files.exe` direto do arquivo instalado | o app abre normalmente pelo pacote, ou mostra uma mensagem clara; nunca termina sem aviso |
| AC-5 | janela principal "estacionada" (em segundo plano) | abro uma pasta | a janela volta ao primeiro plano mostrando a pasta |

## 4. Requisitos
- **FR-001** Toda ativação redirecionada DEVE chegar à instância principal, qualquer que seja a quantidade ou a velocidade dos disparos.
- **FR-002** Nenhum processo DEVE terminar por exceção não tratada nesse caminho.
- **FR-003** O comportamento do diálogo de arquivo (F012) DEVE ser preservado.
- **FR-004** Abertura direta do executável DEVE se resolver sozinha ou explicar a causa.
- **NFR-001** A abertura da pasta na janela principal DEVE aparecer em até 2 s com a janela já aberta (medido).

## 5. Casos de borda
- Chave de instância velha (instância principal que travou antes).
- Disparos simultâneos de dois terminais com diretórios de trabalho diferentes.
- Instância principal ocupada (carregando pasta grande).

## 6. Fora de escopo
- Mudar o modelo de uma janela por processo; reescrever a ativação do upstream.

## 7. Critérios de sucesso
- 20 rodadas do AC-1 sem uma única morte de processo (contagem no log de eventos).

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: no AC-4, prefere reabrir pelo pacote automaticamente (recomendado) ou só mostrar mensagem?]

## 9. Histórico
- 2026-10-24 — rascunho (Claude), a partir do log de eventos de 16 a 23/10.
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
