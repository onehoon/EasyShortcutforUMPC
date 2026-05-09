#include <windows.h>
#include <appmodel.h>
#include <shellapi.h>
#include <shlobj.h>

#include <filesystem>
#include <fstream>
#include <string>

#include "Commands/DisplayResolutionCommandHandler.h"
#include "Commands/ShortcutCommandHandler.h"

namespace {
constexpr DWORD kDuplicateGuardMs = 700;
constexpr DWORD kInitialSettleMs = 120;

std::wstring GetLocalDataPath(const wchar_t* fileName) {
    UINT32 packageFamilyNameLength = 0;
    LONG packageResult = GetCurrentPackageFamilyName(&packageFamilyNameLength, nullptr);
    if (packageResult == ERROR_INSUFFICIENT_BUFFER && packageFamilyNameLength > 0) {
        std::vector<wchar_t> packageFamilyName(packageFamilyNameLength);
        if (GetCurrentPackageFamilyName(&packageFamilyNameLength, packageFamilyName.data()) == ERROR_SUCCESS) {
            wchar_t* localAppData = nullptr;
            size_t len = 0;
            _wdupenv_s(&localAppData, &len, L"LOCALAPPDATA");
            std::wstring base = localAppData ? localAppData : L"";
            if (localAppData) {
                free(localAppData);
            }

            if (!base.empty()) {
                std::filesystem::path packageLocalStatePath = std::filesystem::path(base) / L"Packages" / packageFamilyName.data() / L"LocalState";
                std::filesystem::create_directories(packageLocalStatePath);
                return (packageLocalStatePath / fileName).wstring();
            }
        }
    }

    // Fallback for unexpected non-packaged execution contexts.
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
    for (auto& c : v) {
        c = towlower(c);
    }
    return v;
}

std::wstring ResolveAction(int argc, wchar_t** argv) {
    for (int i = 1; i < argc; ++i) {
        auto a = ToLower(argv[i]);
        if (commands::IsShortcutCommand(a) || commands::IsDisplayResolutionCommand(a)) {
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
    } catch (...) {
    }

    try {
        std::wofstream out(guardPath, std::ios::trunc);
        out << now << L"|" << action;
    } catch (...) {
    }

    return false;
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
    if (action.empty()) {
        return 0;
    }

    Sleep(kInitialSettleMs);
    if (action != L"detect-resolution-presets" && ShouldSkipDuplicate(action)) {
        return 0;
    }

    if (commands::IsShortcutCommand(action)) {
        commands::ExecuteShortcutCommand(action);
        return 0;
    }

    if (commands::IsDisplayResolutionCommand(action)) {
        commands::ExecuteDisplayResolutionCommand(action);
    }

    return 0;
}
