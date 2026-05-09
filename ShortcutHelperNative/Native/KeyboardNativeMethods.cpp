#include "KeyboardNativeMethods.h"

#include <filesystem>

namespace keyboard {
void SendKey(WORD vk, bool up, bool extended) {
    INPUT input{};
    input.type = INPUT_KEYBOARD;
    input.ki.wVk = vk;
    input.ki.dwFlags = (up ? KEYEVENTF_KEYUP : 0) | (extended ? KEYEVENTF_EXTENDEDKEY : 0);
    SendInput(1, &input, sizeof(INPUT));
}

void SendCombo(const std::vector<std::pair<WORD, bool>>& keys) {
    for (const auto& k : keys) {
        SendKey(k.first, false, k.second);
    }
    for (auto it = keys.rbegin(); it != keys.rend(); ++it) {
        SendKey(it->first, true, it->second);
    }
}

std::wstring GetForegroundProcessName() {
    HWND hwnd = GetForegroundWindow();
    if (!hwnd) {
        return L"";
    }

    DWORD pid = 0;
    GetWindowThreadProcessId(hwnd, &pid);
    if (!pid) {
        return L"";
    }

    HANDLE h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);
    if (!h) {
        return L"";
    }

    wchar_t path[MAX_PATH]{};
    DWORD size = MAX_PATH;
    std::wstring name;
    if (QueryFullProcessImageNameW(h, 0, path, &size)) {
        std::filesystem::path p(path);
        name = p.stem().wstring();
    }
    CloseHandle(h);
    return name;
}

bool IsGameBarProcess(const std::wstring& name) {
    return _wcsicmp(name.c_str(), L"GameBar") == 0 ||
           _wcsicmp(name.c_str(), L"GameBarFTServer") == 0 ||
           _wcsicmp(name.c_str(), L"XboxPcAppFT") == 0;
}
}
