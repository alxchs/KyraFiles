// Copyright (c) KyraFiles contributors. Licensed under the MIT License.
// KyraFiles.Watchdog: Supervisor mútuo ultraleve para garantia de retorno instantâneo (< 1s)

#include <windows.h>
#include <string>

#pragma comment(lib, "kernel32.lib")

constexpr const wchar_t* SHUTDOWN_EVENT_NAME = L"Global\\KyraFiles_ShutdownHotkeyService";
constexpr const wchar_t* WATCHDOG_MUTEX_NAME = L"Global\\KyraFiles_Watchdog_InstanceMutex";
constexpr const wchar_t* HOTKEY_SERVICE_MUTEX_NAME = L"Global\\KyraFiles_HotkeyService_InstanceMutex";
constexpr const wchar_t* HOTKEY_EXE_NAME = L"KyraFiles.HotkeyService.exe";

constexpr DWORD INITIAL_DELAY_MS = 200;
constexpr DWORD MAX_DELAY_MS = 30000;
constexpr DWORD MIN_CODE2_DELAY_MS = 5000;
constexpr DWORD HEALTHY_LIFETIME_MS = 60000;
constexpr DWORD MUTEX_CHECK_INTERVAL_MS = 2000;
constexpr DWORD EXIT_CODE_ALREADY_RUNNING = 3;
constexpr DWORD EXIT_CODE_HOOK_FAILED = 2;

static std::wstring GetTestSuffixedName(const wchar_t* baseName)
{
    wchar_t suffix[64] = {};
    DWORD len = GetEnvironmentVariableW(L"FILES_WINE_TEST_SUFFIX", suffix, ARRAYSIZE(suffix));
    if (len > 0 && len < ARRAYSIZE(suffix))
    {
        return std::wstring(baseName) + suffix;
    }
    return std::wstring(baseName);
}

// Grava uma linha de reinício no hotkey.log ao lado do executável em UTF-16
static void LogRestart(DWORD exitCode, DWORD lifetimeMs, DWORD delayMs)
{
    wchar_t path[MAX_PATH];
    DWORD n = GetModuleFileNameW(NULL, path, MAX_PATH);
    if (n == 0 || n >= MAX_PATH) return;
    wchar_t* slash = wcsrchr(path, L'\\');
    if (!slash) return;
    wcscpy_s(slash + 1, MAX_PATH - (slash + 1 - path), L"hotkey.log");

    HANDLE h = CreateFileW(path, FILE_APPEND_DATA, FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (h == INVALID_HANDLE_VALUE) return;

    if (GetLastError() != ERROR_ALREADY_EXISTS)
    {
        wchar_t bom = 0xFEFF;
        DWORD writtenBom = 0;
        WriteFile(h, &bom, sizeof(wchar_t), &writtenBom, NULL);
    }

    SYSTEMTIME st;
    GetLocalTime(&st);
    wchar_t line[256];
    int len = wsprintfW(line, L"%04d-%02d-%02d %02d:%02d:%02d [Watchdog] Reinicio: codigo %lu, vida %lu ms, espera %lu ms\r\n",
        st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, exitCode, lifetimeMs, delayMs);
    DWORD written = 0;
    WriteFile(h, line, len * sizeof(wchar_t), &written, NULL);
    CloseHandle(h);
}
// Aguarda o mutex do servico liberar por bloqueio direto no mutex (retomada instantanea < 1s)
static bool WaitForServiceMutexToClear(HANDLE hShutdownEvent, const std::wstring& hotkeyMutexName, DWORD& totalWaitMs)
{
    totalWaitMs = 0;
    HANDLE hMutex = OpenMutexW(SYNCHRONIZE, FALSE, hotkeyMutexName.c_str());
    if (!hMutex)
    {
        return true; // mutex nao existe, o servico nao esta rodando
    }

    ULONGLONG startWait = GetTickCount64();

    if (hShutdownEvent)
    {
        HANDLE waitHandles[2] = { hShutdownEvent, hMutex };
        DWORD waitRes = WaitForMultipleObjects(2, waitHandles, FALSE, INFINITE);
        ULONGLONG endWait = GetTickCount64();
        totalWaitMs = (endWait >= startWait) ? (DWORD)(endWait - startWait) : 0;

        if (waitRes == WAIT_OBJECT_0)
        {
            CloseHandle(hMutex);
            return false; // encerramento solicitado
        }
        else if (waitRes == WAIT_OBJECT_0 + 1 || waitRes == WAIT_ABANDONED_0 + 1)
        {
            ReleaseMutex(hMutex);
            CloseHandle(hMutex);
            return true; // mutex adquirido ou abandonado: liberado
        }
        else
        {
            CloseHandle(hMutex);
            return false;
        }
    }
    else
    {
        while (true)
        {
            DWORD waitRes = WaitForSingleObject(hMutex, MUTEX_CHECK_INTERVAL_MS);
            if (waitRes == WAIT_OBJECT_0 || waitRes == WAIT_ABANDONED)
            {
                ULONGLONG endWait = GetTickCount64();
                totalWaitMs = (endWait >= startWait) ? (DWORD)(endWait - startWait) : 0;
                ReleaseMutex(hMutex);
                CloseHandle(hMutex);
                return true;
            }
            else if (waitRes == WAIT_TIMEOUT)
            {
                continue; // aguarda proximo intervalo sem laco rapido
            }
            else
            {
                ULONGLONG endWait = GetTickCount64();
                totalWaitMs = (endWait >= startWait) ? (DWORD)(endWait - startWait) : 0;
                CloseHandle(hMutex);
                return false;
            }
        }
    }
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // Mutex para instância única do Watchdog
    std::wstring watchdogMutexName = GetTestSuffixedName(WATCHDOG_MUTEX_NAME);
    HANDLE hSingleInstanceMutex = CreateMutexW(NULL, TRUE, watchdogMutexName.c_str());
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return 0;
    }

    std::wstring shutdownEventName = GetTestSuffixedName(SHUTDOWN_EVENT_NAME);
    HANDLE hShutdownEvent = CreateEventW(NULL, TRUE, FALSE, shutdownEventName.c_str());
    if (hShutdownEvent) ResetEvent(hShutdownEvent);

    std::wstring hotkeyMutexName = GetTestSuffixedName(HOTKEY_SERVICE_MUTEX_NAME);

    // Obtém o caminho do diretório do executável
    wchar_t currentDir[MAX_PATH];
    GetModuleFileNameW(NULL, currentDir, MAX_PATH);
    wchar_t* lastSlash = wcsrchr(currentDir, L'\\');
    if (lastSlash) *(lastSlash + 1) = L'\0';

    std::wstring hotkeyExePath = std::wstring(currentDir) + HOTKEY_EXE_NAME;

    DWORD currentDelayMs = INITIAL_DELAY_MS;

    // Loop de supervisão resiliente
    while (true)
    {
        // Se foi sinalizado encerramento limpo, encerra o loop
        if (hShutdownEvent && WaitForSingleObject(hShutdownEvent, 0) == WAIT_OBJECT_0)
        {
            break;
        }

        // Se já existe um serviço ativo rodando por fora, não cria outro: aguarda ele encerrar
        HANDLE hPreCheck = OpenMutexW(SYNCHRONIZE, FALSE, hotkeyMutexName.c_str());
        if (hPreCheck)
        {
            CloseHandle(hPreCheck);
            DWORD waitMs = 0;
            if (!WaitForServiceMutexToClear(hShutdownEvent, hotkeyMutexName, waitMs))
            {
                break;
            }
            currentDelayMs = INITIAL_DELAY_MS;
            LogRestart(EXIT_CODE_ALREADY_RUNNING, 0, waitMs);
        }

        STARTUPINFOW si = {};
        si.cb = sizeof(STARTUPINFOW);
        si.dwFlags = STARTF_USESHOWWINDOW;
        si.wShowWindow = SW_HIDE;

        PROCESS_INFORMATION pi = {};

        ULONGLONG startTime = GetTickCount64();

        // Inicia o Worker em modo oculto
        BOOL success = CreateProcessW(
            hotkeyExePath.c_str(),
            NULL,
            NULL,
            NULL,
            FALSE,
            CREATE_NO_WINDOW,
            NULL,
            currentDir,
            &si,
            &pi
        );

        if (success)
        {
            // Aguarda ou a morte do processo Worker OU a sinalização do evento de shutdown
            HANDLE waitHandles[2] = { pi.hProcess, hShutdownEvent };
            DWORD waitResult = WaitForMultipleObjects(2, waitHandles, FALSE, INFINITE);

            DWORD exitCode = 0;
            if (waitResult == WAIT_OBJECT_0)
            {
                GetExitCodeProcess(pi.hProcess, &exitCode);
            }

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);

            // Se o evento de shutdown foi sinalizado, encerra
            if (waitResult == WAIT_OBJECT_0 + 1)
            {
                break;
            }

            ULONGLONG endTime = GetTickCount64();
            DWORD lifetimeMs = (endTime >= startTime) ? (DWORD)(endTime - startTime) : 0;

            // (2) Código 3: NÃO recriar; aguardar 2 s e conferir com OpenMutexW se o mutex do serviço ainda existe;
            // só criar outro filho quando o mutex sumir.
            if (exitCode == EXIT_CODE_ALREADY_RUNNING)
            {
                DWORD waitMs = 0;
                if (!WaitForServiceMutexToClear(hShutdownEvent, hotkeyMutexName, waitMs))
                {
                    break;
                }
                currentDelayMs = INITIAL_DELAY_MS;
                LogRestart(exitCode, lifetimeMs, waitMs);
                continue;
            }

            // (1) Espera crescente:
            // Volta a 200 ms se o filho viveu >= 60 s.
            // Se viveu < 60 s: dobra a cada morte até 30000 ms.
            if (lifetimeMs >= HEALTHY_LIFETIME_MS)
            {
                currentDelayMs = INITIAL_DELAY_MS;
            }

            DWORD delayToApply = currentDelayMs;

            // (3) Código 2: continua com mínimo de 5 s e sobe pela mesma regra
            if (exitCode == EXIT_CODE_HOOK_FAILED)
            {
                if (delayToApply < MIN_CODE2_DELAY_MS)
                {
                    delayToApply = MIN_CODE2_DELAY_MS;
                }
            }

            // (4) Cada reinício grava uma linha no hotkey.log
            LogRestart(exitCode, lifetimeMs, delayToApply);

            // Prepara a espera crescente para a próxima morte
            currentDelayMs = delayToApply * 2;
            if (currentDelayMs > MAX_DELAY_MS)
            {
                currentDelayMs = MAX_DELAY_MS;
            }

            // Aguarda a espera aplicada respeitando o evento de shutdown
            if (WaitForSingleObject(hShutdownEvent, delayToApply) == WAIT_OBJECT_0)
            {
                break;
            }
        }
        else
        {
            // Falha de CreateProcess usa a mesma espera crescente
            DWORD delayToApply = currentDelayMs;
            DWORD err = GetLastError();

            LogRestart(err, 0, delayToApply);

            currentDelayMs = delayToApply * 2;
            if (currentDelayMs > MAX_DELAY_MS)
            {
                currentDelayMs = MAX_DELAY_MS;
            }

            if (WaitForSingleObject(hShutdownEvent, delayToApply) == WAIT_OBJECT_0)
            {
                break;
            }
        }
    }

    if (hSingleInstanceMutex)
    {
        ReleaseMutex(hSingleInstanceMutex);
        CloseHandle(hSingleInstanceMutex);
    }

    if (hShutdownEvent)
    {
        CloseHandle(hShutdownEvent);
    }

    return 0;
}