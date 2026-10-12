// Copyright (c) Files Community. Licensed under the MIT License.
// Files.Watchdog: Supervisor mútuo ultraleve para garantia de retorno instantâneo (< 1s)

#include <windows.h>
#include <string>

#pragma comment(lib, "kernel32.lib")

constexpr const wchar_t* SHUTDOWN_EVENT_NAME = L"Global\\FilesCustom_ShutdownHotkeyService";
constexpr const wchar_t* HOTKEY_EXE_NAME = L"Files.HotkeyService.exe";

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // Mutex para instância única do Watchdog
    HANDLE hSingleInstanceMutex = CreateMutexW(NULL, TRUE, L"Global\\FilesCustom_Watchdog_InstanceMutex");
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        if (hSingleInstanceMutex) CloseHandle(hSingleInstanceMutex);
        return 0;
    }

    HANDLE hShutdownEvent = CreateEventW(NULL, TRUE, FALSE, SHUTDOWN_EVENT_NAME);

    // Obtém o caminho do diretório do executável
    wchar_t currentDir[MAX_PATH];
    GetModuleFileNameW(NULL, currentDir, MAX_PATH);
    wchar_t* lastSlash = wcsrchr(currentDir, L'\\');
    if (lastSlash) *(lastSlash + 1) = L'\0';

    std::wstring hotkeyExePath = std::wstring(currentDir) + HOTKEY_EXE_NAME;

    // Loop de supervisão resiliente
    while (true)
    {
        // Se foi sinalizado encerramento limpo, encerra o loop
        if (hShutdownEvent && WaitForSingleObject(hShutdownEvent, 0) == WAIT_OBJECT_0)
        {
            break;
        }

        STARTUPINFOW si = {};
        si.cb = sizeof(STARTUPINFOW);
        si.dwFlags = STARTF_USESHOWWINDOW;
        si.wShowWindow = SW_HIDE;

        PROCESS_INFORMATION pi = {};

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

            // Codigo 2: o Worker nao conseguiu instalar o hook (ver hotkey.log). Reiniciar a cada 200ms so
            // gastaria CPU, entao espera mais; qualquer outra morte volta em ~200ms.
            Sleep(exitCode == 2 ? 5000 : 200);
        }
        else
        {
            // Se falhou ao criar (ex: arquivo temporariamente ocupado), aguarda brevemente
            Sleep(500);
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

