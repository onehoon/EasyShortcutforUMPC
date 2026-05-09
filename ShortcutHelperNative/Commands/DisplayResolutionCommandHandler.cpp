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

void WriteState(
    bool available,
    const std::wstring& group,
    bool support1200,
    bool support1080,
    bool support1050,
    bool support1440x900,
    bool support900,
    bool support720) {
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
    out << L"support_1200p=" << (support1200 ? 1 : 0) << L"\n";
    out << L"support_1080p=" << (support1080 ? 1 : 0) << L"\n";
    out << L"support_1050p=" << (support1050 ? 1 : 0) << L"\n";
    out << L"support_1440x900=" << (support1440x900 ? 1 : 0) << L"\n";
    out << L"support_900p=" << (support900 ? 1 : 0) << L"\n";
    out << L"support_720p=" << (support720 ? 1 : 0) << L"\n";
}

bool IsTargetSupported(const std::wstring& gdiDeviceName, int width, int height) {
    // Only expose/apply presets that support the current refresh rate and color depth.
    // This feature intentionally does not switch refresh rate.
    return display::ContainsModeAtCurrentTiming(gdiDeviceName, width, height);
}

void RunDetection() {
    const auto info = display::DetectPrimaryDisplayInfo();
    if (!info.valid || info.hasActiveExternalPath || !info.primaryIsInternal || info.primaryGdiDeviceName.empty()) {
        WriteState(false, L"none", false, false, false, false, false, false);
        return;
    }

    const auto modes = display::EnumerateModes(info.primaryGdiDeviceName);
    if (modes.empty()) {
        WriteState(false, L"none", false, false, false, false, false, false);
        return;
    }

    const bool has1200Base = display::ContainsMode(modes, 1920, 1200);
    const bool has1080Base = display::ContainsMode(modes, 1920, 1080);

    if (has1200Base) {
        const bool support1200 = IsTargetSupported(info.primaryGdiDeviceName, 1920, 1200);
        const bool support1080 = IsTargetSupported(info.primaryGdiDeviceName, 1920, 1080);
        const bool support1050 = IsTargetSupported(info.primaryGdiDeviceName, 1680, 1050);
        const bool support1440x900 = IsTargetSupported(info.primaryGdiDeviceName, 1440, 900);
        const bool any = support1200 || support1080 || support1050 || support1440x900;
        WriteState(any, L"1200", support1200, support1080, support1050, support1440x900, false, false);
        return;
    }

    if (has1080Base) {
        const bool support1080 = IsTargetSupported(info.primaryGdiDeviceName, 1920, 1080);
        const bool support900 = IsTargetSupported(info.primaryGdiDeviceName, 1600, 900);
        const bool support720 = IsTargetSupported(info.primaryGdiDeviceName, 1280, 720);
        const bool any = support1080 || support900 || support720;
        WriteState(any, L"1080", false, support1080, false, false, support900, support720);
        return;
    }

    WriteState(false, L"none", false, false, false, false, false, false);
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
           action == L"set-resolution-1440-900" ||
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
    } else if (action == L"set-resolution-1440-900") {
        RunSetResolution(1440, 900);
    } else if (action == L"set-resolution-1280-720") {
        RunSetResolution(1280, 720);
    }
}
}
