// Copyright (c) KyraFiles contributors. Licensed under the MIT License.
// KyraFiles.HotkeyService: Interceptador global de Win+E nativo, ultraleve e sem UI.
//
// RegisterHotKey(MOD_WIN, 'E') nao serve: o explorer.exe e dono desse atalho e a chamada falha com
// ERROR_HOTKEY_ALREADY_REGISTERED (1409), medido em 12/10/2026 (DECISIONS.md D-021). Por isso o servico usa
// um hook de teclado de baixo nivel que engole o Win+E e aciona o Files pelo protocolo registrado.

#include <windows.h>
#include <shellapi.h>
#include <string>
#include <cstdlib>

#pragma comment(lib, "user32.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "kernel32.lib")

constexpr UINT WM_OPEN_FILES = WM_APP + 1;
constexpr ULONG_PTR INJECTED_TAG = 0x46494C45; // "FILE": marca as teclas injetadas por este servico
constexpr WORD NEUTRAL_VK = 0xE8;              // VK sem atribuicao: cancela o menu Iniciar ao soltar o Win
constexpr DWORD EXIT_HOOK_FAILED = 2;          // o Watchdog espera mais antes de reiniciar neste caso
constexpr DWORD EXIT_ALREADY_RUNNING = 3;      // já existe outra instância em execução (FR-001)
constexpr const wchar_t* SHUTDOWN_EVENT_NAME = L"Global\\KyraFiles_ShutdownHotkeyService";
constexpr const wchar_t* INSTANCE_MUTEX_NAME = L"Global\\KyraFiles_HotkeyService_InstanceMutex";

static HWND g_hWnd = NULL;
static HANDLE g_hShutdownEvent = NULL;
static HHOOK g_hHook = NULL;
static bool g_swallowingE = false;

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

static bool IsTestNoHookEnabled()
{
    wchar_t suffix[64] = {};
    DWORD suffixLen = GetEnvironmentVariableW(L"FILES_WINE_TEST_SUFFIX", suffix, ARRAYSIZE(suffix));
    if (suffixLen == 0 || suffixLen >= ARRAYSIZE(suffix))
    {
        return false;
    }

    wchar_t val[16] = {};
    DWORD len = GetEnvironmentVariableW(L"FILES_WINE_TEST_NOHOOK", val, ARRAYSIZE(val));
    return (len > 0 && wcscmp(val, L"1") == 0);
}

static bool IsDown(int vk)
{
    return (GetAsyncKeyState(vk) & 0x8000) != 0;
}

// Grava uma linha ao lado do executavel quando algo impede o atalho de funcionar.
static void LogLine(const wchar_t* text, DWORD error)
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
    int len = wsprintfW(line, L"%04d-%02d-%02d %02d:%02d:%02d %s (erro %lu)\r\n",
        st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, text, error);
    DWORD written = 0;
    WriteFile(h, line, len * sizeof(wchar_t), &written, NULL);
    CloseHandle(h);
}

// Neutraliza o menu Iniciar: o sistema so abre o Iniciar quando Win sobe sem nenhuma outra tecla no meio.
// Como engolimos o E, injetamos uma tecla neutra com o Win ainda pressionado.
static void InjectNeutralKey()
{
    INPUT in[2] = {};
    in[0].type = INPUT_KEYBOARD;
    in[0].ki.wVk = NEUTRAL_VK;
    in[0].ki.dwExtraInfo = INJECTED_TAG;
    in[1] = in[0];
    in[1].ki.dwFlags = KEYEVENTF_KEYUP;
    SendInput(2, in, sizeof(INPUT));
}

// Callback minimo: roda no caminho de toda tecla do sistema e o Windows remove o hook que passar do timeout.
static LRESULT CALLBACK LowLevelKeyboardProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION)
    {
        const KBDLLHOOKSTRUCT* k = reinterpret_cast<const KBDLLHOOKSTRUCT*>(lParam);

        if (k->dwExtraInfo != INJECTED_TAG && k->vkCode == 'E')
        {
            const bool down = (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN);
            const bool up = (wParam == WM_KEYUP || wParam == WM_SYSKEYUP);

            if (down)
            {
                if (g_swallowingE)
                {
                    return 1; // repeticao automatica da tecla enquanto o E esta engolido
                }

                const bool win = IsDown(VK_LWIN) || IsDown(VK_RWIN);
                const bool others = IsDown(VK_CONTROL) || IsDown(VK_SHIFT) || IsDown(VK_MENU);

                if (win && !others)
                {
                    g_swallowingE = true;
                    InjectNeutralKey();
                    PostMessageW(g_hWnd, WM_OPEN_FILES, 0, 0);
                    return 1;
                }
            }
            else if (up && g_swallowingE)
            {
                g_swallowingE = false;
                return 1;
            }
        }
    }

    return CallNextHookEx(g_hHook, code, wParam, lParam);
}

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_OPEN_FILES:
        // Se o Files ja estiver aberto, o protocolo atinge a instancia existente; se nao, o Windows o inicia.
        // A URI tem de ser exatamente "kyrafiles:": e o ramo do MainWindow feito para o Win+E (abre a MainPage e traz
        // para o primeiro plano). Com query ("?tab=Home") o app tenta ler JSON de aba e quebra (debug.log, 12/10/2026).
        if (!IsTestNoHookEnabled())
        {
            ShellExecuteW(NULL, L"open", L"kyrafiles:", NULL, NULL, SW_SHOWNORMAL);
        }
        return 0;

    case WM_DESTROY:
        if (g_hHook)
        {
            UnhookWindowsHookEx(g_hHook);
            g_hHook = NULL;
        }
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProcW(hWnd, message, wParam, lParam);
}

// Thread para monitorar evento cooperativo de encerramento
DWORD WINAPI ShutdownMonitorThread(LPVOID lpParam)
{
    if (g_hShutdownEvent)
    {
        WaitForSingleObject(g_hShutdownEvent, INFINITE);
        if (g_hWnd)
        {
            PostMessageW(g_hWnd, WM_CLOSE, 0, 0);
        }
    }
    return 0;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // Suporte a teste automatizado: saida simulada quando sob ambiente de teste isolado
    wchar_t testSuffix[64] = {};
    if (GetEnvironmentVariableW(L"FILES_WINE_TEST_SUFFIX", testSuffix, ARRAYSIZE(testSuffix)) > 0)
    {
        wchar_t exitCodeStr[16] = {};
        if (GetEnvironmentVariableW(L"FILES_WINE_TEST_EXITCODE", exitCodeStr, ARRAYSIZE(exitCodeStr)) > 0)
        {
            return (int)_wtoi(exitCodeStr);
        }
    }

    // Mutex para instancia unica do servico
    std::wstring instanceMutexName = GetTestSuffixedName(INSTANCE_MUTEX_NAME);
    HANDLE hSingleInstanceMutex = CreateMutexW(NULL, TRUE, instanceMutexName.c_str());
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        LogLine(L"Outra instancia ja esta em execucao", ERROR_ALREADY_EXISTS);
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return EXIT_ALREADY_RUNNING;
    }

    // Cria/Abre evento global de shutdown cooperativo
    std::wstring shutdownEventName = GetTestSuffixedName(SHUTDOWN_EVENT_NAME);
    g_hShutdownEvent = CreateEventW(NULL, TRUE, FALSE, shutdownEventName.c_str());

    // Registra classe de janela invisivel para receber mensagens
    WNDCLASSEXW wcex = {};
    wcex.cbSize = sizeof(WNDCLASSEXW);
    wcex.lpfnWndProc = WndProc;
    wcex.hInstance = hInstance;
    wcex.lpszClassName = L"KyraFiles_HotkeyService_WindowClass";

    RegisterClassExW(&wcex);

    g_hWnd = CreateWindowExW(
        0,
        wcex.lpszClassName,
        L"KyraFiles_HotkeyService_Window",
        0, 0, 0, 0, 0,
        HWND_MESSAGE, // Janela somente para mensagens (sem tarefa, sem tela, sem foco)
        NULL,
        hInstance,
        NULL
    );

    if (!g_hWnd)
    {
        LogLine(L"CreateWindowExW falhou", GetLastError());
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return 1;
    }

    // O hook de baixo nivel exige uma thread com loop de mensagens: e a principal.
    if (!IsTestNoHookEnabled())
    {
        g_hHook = SetWindowsHookExW(WH_KEYBOARD_LL, LowLevelKeyboardProc, hInstance, 0);
        if (!g_hHook)
        {
            LogLine(L"SetWindowsHookExW(WH_KEYBOARD_LL) falhou; Win+E nao sera interceptado", GetLastError());
            DestroyWindow(g_hWnd);
            if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
            return EXIT_HOOK_FAILED;
        }
    }

    // Inicia monitor do evento de desligamento cooperativo
    HANDLE hThread = CreateThread(NULL, 0, ShutdownMonitorThread, NULL, 0, NULL);
    if (hThread) CloseHandle(hThread);

    // Message Loop padrao Win32 (0% de CPU em repouso)
    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    if (hSingleInstanceMutex)
    {
        ReleaseMutex(hSingleInstanceMutex);
        CloseHandle(hSingleInstanceMutex);
    }

    if (g_hShutdownEvent)
    {
        CloseHandle(g_hShutdownEvent);
    }

    return (int)msg.wParam;
}
