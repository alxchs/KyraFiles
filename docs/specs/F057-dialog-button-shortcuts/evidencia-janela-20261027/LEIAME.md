# F057 - prova de tela de 27/10/2026 (janela 12:35-13:20)

Build testado: integra-f056-f057. Caixa usada: "Delete item" (tecla Delete sobre arquivos do laboratorio).

| AC | Estado | Evidencia |
|---|---|---|
| AC-1 letras sublinhadas | **OBSERVED** | caixa_excluir.png: D de Delete, C de Cancel e P de Permanently delete aparecem sublinhados |
| AC-5 sem letra repetida | **OBSERVED** | as tres letras visiveis na mesma caixa (D, C, P) sao diferentes |
| AC-2, AC-3, AC-8 (teclas) | **NOT TESTED** | os resultados FAIL dos CSVs sao do SCRIPT, nao do app: a deteccao da caixa pela presenca de um botao Cancel se confundiu com elementos antigos da arvore de UI, e a sessao perdeu o primeiro plano (mstsc e uma janela OpenWith assumiram) antes de as teclas serem provadas. Nao ha como dizer que o app falhou nem que passou. |
| AC-4, AC-6, AC-7, AC-9 | NOT TESTED | nao rodados |

Nota: uma tecla Enter enviada com a caixa suposta aberta acionou uma janela "Abrir com" do Windows (OpenWith), sinal de que a caixa nao estava aberta naquele momento.
