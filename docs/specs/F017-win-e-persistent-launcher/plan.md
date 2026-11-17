# F017 — Interceptador Global Ultraleve, Persistente do Win+E e Instalador Modular — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Arquitetura Proposta

### 1.1. Interceptador e Persistência Sub-Segundo (Win+E)

1. **`KyraFiles.HotkeyService.exe` (Worker de Atalho)**:
   - Executável C++ nativo puro (~100 KB).
   - Sem UI (`WinMain`, sem janelas visíveis).
   - Instala um hook `WH_KEYBOARD_LL` que engole Win+E (D-021: `RegisterHotKey` falha com erro 1409 porque o Explorer é dono do atalho).
   - Ao receber o evento:
     - Dispara `ShellExecuteW(NULL, L"open", L"kyrafiles-dev:", NULL, NULL, SW_SHOWNORMAL)` (URI exato do ramo Win+E do `MainWindow`; com query o app tenta ler JSON de aba e quebra).
     - Se o KyraFiles estiver aberto, o protocolo cai na instância ativa em < 15 ms.
     - Se estiver fechado, o KyraFiles é acionado imediatamente sem acionar o Windows Explorer.

2. **`KyraFiles.Watchdog.exe` (Supervisor de Persistência)**:
   - Executável C++ nativo companheiro (~80 KB).
   - Segura o handle de processo do `KyraFiles.HotkeyService.exe` via `WaitForSingleObject(hProc, INFINITE)`.
   - Ao detectar encerramento involuntário (morte forçada), reabre o Worker em **~200 ms** (bem abaixo do teto de 1 segundo).
   - O Worker também monitora o Watchdog, garantindo resiliência recíproca.
   - Encerramento cooperativo controlado pelo evento nomeado Win32 `Global\FilesCustom_ShutdownHotkeyService`.

---

## 2. Instalador Modular, Atualizador Inteligente e Isolamento de Dependências

1. **Pasta de Instalação e Isolamento**:
   - Destino: `%LocalAppData%\Programs\KyraFiles\` (sem poluir `Windows` ou `System32` e sem depender de Admin para o app principal).
   - Subpasta de dependências: `%LocalAppData%\Programs\KyraFiles\Dependencies\` (bibliotecas de runtime isoladas do Windows).
   - Zero acoplamento no registro do Windows além do `Run` do usuário e associações de shell de usuário.

2. **Detecção de Dependências e Pacote Reduzido (Delta Update)**:
   - Manifesto de versão `package_manifest.json` com hashes SHA256 e tamanhos de cada arquivo.
   - **Fluxo do Atualizador (Verificar Atualizações / Notificação)**:
     1. Consulta endpoint/manifesto da nova versão.
     2. Compara o hash das dependências pesadas (`Dependencies\`) locais com o da nova versão.
     3. Se as dependências forem idênticas: baixa apenas o **Instalador Reduzido / Delta** (< 15 MB), contendo somente os binários do KyraFiles e módulos que mudaram.
     4. Se a arquitetura ou as dependências mudaram: baixa o instalador completo.
     5. O atualizador encerra com segurança o KyraFiles e os serviços auxiliares (`KyraFiles.HotkeyService`, `KyraFiles.Watchdog`), copia/substitui os arquivos atualizados e reinicia o app.

3. **Desinstalação 100% Limpa**:
   - Script de desinstalação (gerado pelo Inno Setup):
     1. Sinaliza o evento de encerramento do Watchdog e do HotkeyService.
     2. Remove as chaves de inicialização `HKCU\...\Run` e associações `HKCU\Software\Classes\Folder\...`.
     3. Remove recursivamente a pasta `%LocalAppData%\Programs\KyraFiles\`.
     4. Imediatamente após a remoção, o atalho **Win+E** e as aberturas de pastas voltam a invocar o `explorer.exe` nativo do Windows.

---

## 3. Plano de Auditoria e Otimização de Desempenho

- Após cada entrega do instalador e dos serviços, rodar a suíte oficial de performance em `tools/perf/`:
  - `tools/perf/f007_measure_fps.ps1`: Medição de renderização e rolagem.
  - Testes de tempo de cold start e tempo de resposta de diretórios.
  - Análise dos pontos de regressão observados na última execução para garantir que a navegação continue instantânea e fluida.
