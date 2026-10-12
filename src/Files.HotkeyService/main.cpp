// Copyright (c) Files Community. Licensed under the MIT License.
// Files.HotkeyService: Interceptador global de Win+E nativo, ultraleve e sem UI.

#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <string>

#pragma comment(lib, "user32.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "kernel32.lib")

constexpr int HOTKEY_ID = 1001;
constexpr const wchar_t* SHUTDOWN_EVENT_NAME = L"Global\\FilesCustom_ShutdownHotkeyService";
constexpr const wchar_t* WATCHDOG_PROCESS_NAME = L"Files.Watchdog.exe";

static HWND g_hWnd = NULL;
static HANDLE g_hShutdownEvent = NULL;

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_HOTKEY:
        if (wParam == HOTKEY_ID)
        {
            // Aciona o Files Custom diretamente pelo protocolo registrado
            // Se o Files já estiver aberto, o protocolo atinge a instância existente
            // Se estiver fechado, o Windows inicia o Files.
            ShellExecuteW(NULL, L"open", L"files-dev:?tab=Home", NULL, NULL, SW_SHOWNORMAL);
            return 0;
        }
        break;

    case WM_DESTROY:
        UnregisterHotKey(hWnd, HOTKEY_ID);
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
    // Mutex para instância única do serviço
    HANDLE hSingleInstanceMutex = CreateMutexW(NULL, TRUE, L"Global\\FilesCustom_HotkeyService_InstanceMutex");
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return 0;
    }

    // Cria/Abre evento global de shutdown cooperativo
    g_hShutdownEvent = CreateEventW(NULL, TRUE, FALSE, SHUTDOWN_EVENT_NAME);

    // Registra classe de janela invisível para receber mensagens WM_HOTKEY
    WNDCLASSEXW wcex = {};
    wcex.cbSize = sizeof(WNDCLASSEXW);
    wcex.lpfnWndProc = WndProc;
    wcex.hInstance = hInstance;
    wcex.lpszClassName = L"FilesCustom_HotkeyService_WindowClass";

    RegisterClassExW(&wcex);

    g_hWnd = CreateWindowExW(
        0,
        wcex.lpszClassName,
        L"FilesCustom_HotkeyService_Window",
        0, 0, 0, 0, 0,
        HWND_MESSAGE, // Janela somente para mensagens (sem tarefa, sem tela, sem foco)
        NULL,
        hInstance,
        NULL
    );

    if (!g_hWnd)
    {
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return 1;
    }

    // Registra atalho global WIN + E (MOD_WIN | MOD_NOREPEAT, 'E' = 0x45)
    // MOD_NOREPEAT evita disparos contínuos repetitivos ao segurar a tecla
    BOOL registered = RegisterHotKey(g_hWnd, HOTKEY_ID, MOD_WIN | MOD_NOREPEAT, 'E');
    if (!registered)
    {
        // Se falhou com MOD_NOREPEAT (ex: build antigo do Win), tenta fallback simples
        RegisterHotKey(g_hWnd, HOTKEY_ID, MOD_WIN, 'E');
    }

    // Inicia monitor do evento de desligamento cooperativo
    HANDLE hThread = CreateThread(NULL, 0, ShutdownMonitorThread, NULL, 0, NULL);
    if (hThread) CloseHandle(hThread);

    // Message Loop padrão Win32 (0% de CPU em repouso)
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

