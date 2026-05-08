#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <string>
#include <fstream>
#include <filesystem>
#include <vector>

namespace {
    constexpr DWORD kDuplicateGuardMs = 700;
    constexpr DWORD kInitialSettleMs = 120;
    constexpr DWORD kPostFocusSettleMs = 420;

    std::wstring GetLocalDataPath(const wchar_t* fileName) {
        std::wstring base;
        PWSTR knownFolderPath = nullptr;
        if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &knownFolderPath)) && knownFolderPath != nullptr) {
            base = knownFolderPath;
            CoTaskMemFree(knownFolderPath);
        } else {
            wchar_t* localAppData = nullptr;
            size_t len = 0;
            _wdupenv_s(&localAppData, &len, L"LOCALAPPDATA");
            base = localAppData ? localAppData : L"";
            if (localAppData) {
                free(localAppData);
            }
        }

        std::filesystem::path p = std::filesystem::path(base) / L"EasyShortcutForUMPC";
        std::filesystem::create_directories(p);
        return (p / fileName).wstring();
    }

    std::wstring ToLower(std::wstring v) {
        for (auto& c : v) c = towlower(c);
        return v;
    }

    std::wstring ResolveAction(int argc, wchar_t** argv) {
        for (int i = 1; i < argc; ++i) {
            std::wstring a = ToLower(argv[i]);
            if (a == L"insert" || a == L"altinsert" || a == L"home" || a == L"end" || a == L"losslessscaling" || a == L"quit") {
                return a;
            }
        }
        return L"";
    }

    bool ShouldSkipDuplicate(const std::wstring& action) {
        const auto guardPath = GetLocalDataPath(L"helper.guard");
        const ULONGLONG now = GetTickCount64();
        try {
            std::wifstream in(guardPath);
            if (in) {
                std::wstring line;
                std::getline(in, line);
                const auto sep = line.find(L'|');
                if (sep != std::wstring::npos) {
                    const auto ticks = std::stoull(line.substr(0, sep));
                    const auto prevAction = line.substr(sep + 1);
                    if (prevAction == action && now >= ticks && (now - ticks) < kDuplicateGuardMs) {
                        return true;
                    }
                }
            }
        } catch (...) {}

        try {
            std::wofstream out(guardPath, std::ios::trunc);
            out << now << L"|" << action;
        } catch (...) {}

        return false;
    }

    void SendKey(WORD vk, bool up, bool extended) {
        INPUT input{};
        input.type = INPUT_KEYBOARD;
        input.ki.wVk = vk;
        input.ki.dwFlags = (up ? KEYEVENTF_KEYUP : 0) | (extended ? KEYEVENTF_EXTENDEDKEY : 0);
        SendInput(1, &input, sizeof(INPUT));
    }

    void SendCombo(const std::vector<std::pair<WORD, bool>>& keys) {
        for (const auto& k : keys) SendKey(k.first, false, k.second);
        for (auto it = keys.rbegin(); it != keys.rend(); ++it) SendKey(it->first, true, it->second);
    }

    std::wstring GetForegroundProcessName() {
        HWND hwnd = GetForegroundWindow();
        if (!hwnd) return L"";

        DWORD pid = 0;
        GetWindowThreadProcessId(hwnd, &pid);
        if (!pid) return L"";

        HANDLE h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);
        if (!h) return L"";

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

    bool IsGameBarProcess(const std::wstring& n) {
        return _wcsicmp(n.c_str(), L"GameBar") == 0 ||
               _wcsicmp(n.c_str(), L"GameBarFTServer") == 0 ||
               _wcsicmp(n.c_str(), L"XboxPcAppFT") == 0;
    }

    void CloseGameBarAndWaitFocus() {
        SendCombo({ {VK_LWIN,false}, {0x47,false} });

        int nonGameBarStreak = 0;
        for (int i = 1; i <= 8; ++i) {
            Sleep(i == 1 ? 260 : 160);
            const auto proc = GetForegroundProcessName();
            if (!IsGameBarProcess(proc)) {
                nonGameBarStreak++;
                if (nonGameBarStreak >= 2) {
                    Sleep(kPostFocusSettleMs);
                    return;
                }
            } else {
                nonGameBarStreak = 0;
            }
        }
        Sleep(kPostFocusSettleMs);
    }
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    int argc = 0;
    wchar_t** argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv == nullptr || argc <= 1) {
        if (argv != nullptr) {
            LocalFree(argv);
        }
        return 0;
    }

    const auto action = ResolveAction(argc, argv);
    LocalFree(argv);
    if (action.empty()) return 0;

    Sleep(kInitialSettleMs);
    if (ShouldSkipDuplicate(action)) return 0;

    CloseGameBarAndWaitFocus();

    if (action == L"insert") {
        SendCombo({ {VK_INSERT, true} });
    } else if (action == L"altinsert") {
        SendCombo({ {VK_MENU, false}, {VK_INSERT, true} });
    } else if (action == L"home") {
        SendCombo({ {VK_HOME, true} });
    } else if (action == L"end") {
        SendCombo({ {VK_END, true} });
    } else if (action == L"losslessscaling") {
        SendCombo({ {VK_CONTROL, false}, {VK_MENU, false}, {0x53, false} });
    } else if (action == L"quit") {
        SendCombo({ {VK_MENU, false}, {VK_F4, false} });
    }

    return 0;
}
