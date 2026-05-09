#include "DisplayResolutionCommandHandler.h"

#include <windows.h>
#include <appmodel.h>

#include <filesystem>
#include <fstream>
#include <vector>

#include "../Display/DisplayModeEnumerator.h"
#include "../Display/DisplayTopologyDetector.h"
#include "../Display/ResolutionChanger.h"

namespace {
std::wstring GetLocalStatePath() {
    UINT32 length = 0;
    LONG result = GetCurrentPackageFamilyName(&length, nullptr);
    if (result != ERROR_INSUFFICIENT_BUFFER || length == 0) {
        return L"";
    }

    std::vector<wchar_t> packageFamilyName(length);
    if (GetCurrentPackageFamilyName(&length, packageFamilyName.data()) != ERROR_SUCCESS) {
        return L"";
    }

    wchar_t* localAppData = nullptr;
    size_t envLen = 0;
    _wdupenv_s(&localAppData, &envLen, L"LOCALAPPDATA");
    std::wstring base = localAppData ? localAppData : L"";
    if (localAppData) {
        free(localAppData);
    }

    if (base.empty()) {
        return L"";
    }

    std::filesystem::path path = std::filesystem::path(base) / L"Packages" / packageFamilyName.data() / L"LocalState";
    std::filesystem::create_directories(path);
    return path.wstring();
}

void WriteState(bool available, const std::wstring& group) {
    const auto dir = GetLocalStatePath();
    if (dir.empty()) {
        return;
    }

    std::wofstream out(std::filesystem::path(dir) / L"resolution_state.txt", std::ios::trunc);
    if (!out) {
        return;
    }

    out << L"available=" << (available ? 1 : 0) << L"\n";
    out << L"group=" << group << L"\n";
}

bool IsTargetSupported(const std::wstring& gdiDeviceName, int width, int height) {
    const auto modes = display::EnumerateModes(gdiDeviceName);
    return display::ContainsMode(modes, width, height);
}

void RunDetection() {
    const auto info = display::DetectPrimaryDisplayInfo();
    if (!info.valid || info.hasActiveExternalPath || !info.primaryIsInternal || info.primaryGdiDeviceName.empty()) {
        WriteState(false, L"none");
        return;
    }

    const auto modes = display::EnumerateModes(info.primaryGdiDeviceName);
    if (modes.empty()) {
        WriteState(false, L"none");
        return;
    }

    if (display::ContainsMode(modes, 1920, 1200)) {
        WriteState(true, L"1200");
        return;
    }

    if (display::ContainsMode(modes, 1920, 1080)) {
        WriteState(true, L"1080");
        return;
    }

    WriteState(false, L"none");
}

void RunSetResolution(int width, int height) {
    const auto info = display::DetectPrimaryDisplayInfo();
    if (!info.valid || info.hasActiveExternalPath || !info.primaryIsInternal || info.primaryGdiDeviceName.empty()) {
        return;
    }

    if (!IsTargetSupported(info.primaryGdiDeviceName, width, height)) {
        return;
    }

    (void)display::ChangePrimaryResolution(info.primaryGdiDeviceName, width, height);
}
}

namespace commands {
bool IsDisplayResolutionCommand(const std::wstring& action) {
    return action == L"detect-resolution-presets" ||
           action == L"set-resolution-1920-1200" ||
           action == L"set-resolution-1920-1080" ||
           action == L"set-resolution-1680-1050" ||
           action == L"set-resolution-1600-900" ||
           action == L"set-resolution-1280-720";
}

void ExecuteDisplayResolutionCommand(const std::wstring& action) {
    if (action == L"detect-resolution-presets") {
        RunDetection();
    } else if (action == L"set-resolution-1920-1200") {
        RunSetResolution(1920, 1200);
    } else if (action == L"set-resolution-1920-1080") {
        RunSetResolution(1920, 1080);
    } else if (action == L"set-resolution-1680-1050") {
        RunSetResolution(1680, 1050);
    } else if (action == L"set-resolution-1600-900") {
        RunSetResolution(1600, 900);
    } else if (action == L"set-resolution-1280-720") {
        RunSetResolution(1280, 720);
    }
}
}
