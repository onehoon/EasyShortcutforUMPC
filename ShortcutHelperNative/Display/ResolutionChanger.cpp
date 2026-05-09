#include "ResolutionChanger.h"

#include <windows.h>
#include <shlobj.h>

#include <filesystem>
#include <fstream>
#include <string>

namespace {
struct ModeSnapshot {
    std::wstring deviceName;
    DWORD width = 0;
    DWORD height = 0;
    DWORD frequency = 0;
    DWORD bitsPerPel = 0;
    bool valid = false;
};

std::filesystem::path GetRollbackFilePath() {
    PWSTR knownFolderPath = nullptr;
    std::wstring base;
    if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &knownFolderPath)) && knownFolderPath != nullptr) {
        base = knownFolderPath;
        CoTaskMemFree(knownFolderPath);
    }

    if (base.empty()) {
        return {};
    }

    std::filesystem::path dir = std::filesystem::path(base) / L"EasyShortcutForUMPC";
    std::filesystem::create_directories(dir);
    return dir / L"resolution_rollback.txt";
}

ModeSnapshot CaptureCurrent(const std::wstring& gdiDeviceName) {
    ModeSnapshot snapshot;
    DEVMODEW current{};
    current.dmSize = sizeof(current);
    if (!EnumDisplaySettingsExW(gdiDeviceName.c_str(), ENUM_CURRENT_SETTINGS, &current, 0)) {
        return snapshot;
    }

    snapshot.deviceName = gdiDeviceName;
    snapshot.width = current.dmPelsWidth;
    snapshot.height = current.dmPelsHeight;
    snapshot.frequency = current.dmDisplayFrequency;
    snapshot.bitsPerPel = current.dmBitsPerPel;
    snapshot.valid = true;
    return snapshot;
}

void SaveSnapshot(const ModeSnapshot& snapshot) {
    if (!snapshot.valid) {
        return;
    }

    const auto path = GetRollbackFilePath();
    if (path.empty()) {
        return;
    }

    std::wofstream out(path, std::ios::trunc);
    if (!out) {
        return;
    }

    out << snapshot.deviceName << L"\n";
    out << snapshot.width << L" " << snapshot.height << L" " << snapshot.frequency << L" " << snapshot.bitsPerPel << L"\n";
}

ModeSnapshot LoadSnapshot() {
    ModeSnapshot snapshot;
    const auto path = GetRollbackFilePath();
    if (path.empty()) {
        return snapshot;
    }

    std::wifstream in(path);
    if (!in) {
        return snapshot;
    }

    std::wstring deviceName;
    if (!std::getline(in, deviceName) || deviceName.empty()) {
        return snapshot;
    }

    DWORD width = 0;
    DWORD height = 0;
    DWORD frequency = 0;
    DWORD bitsPerPel = 0;
    in >> width >> height >> frequency >> bitsPerPel;
    if (!in.good() && !in.eof()) {
        return snapshot;
    }

    snapshot.deviceName = deviceName;
    snapshot.width = width;
    snapshot.height = height;
    snapshot.frequency = frequency;
    snapshot.bitsPerPel = bitsPerPel;
    snapshot.valid = width > 0 && height > 0;
    return snapshot;
}

bool ApplyMode(const std::wstring& gdiDeviceName, DWORD width, DWORD height, DWORD frequency, DWORD bitsPerPel) {
    DEVMODEW candidate{};
    candidate.dmSize = sizeof(candidate);
    candidate.dmPelsWidth = width;
    candidate.dmPelsHeight = height;
    candidate.dmDisplayFrequency = frequency;
    candidate.dmBitsPerPel = bitsPerPel;
    candidate.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL;

    LONG testResult = ChangeDisplaySettingsExW(
        gdiDeviceName.c_str(),
        &candidate,
        nullptr,
        CDS_TEST,
        nullptr);
    if (testResult != DISP_CHANGE_SUCCESSFUL) {
        return false;
    }

    LONG applyResult = ChangeDisplaySettingsExW(
        gdiDeviceName.c_str(),
        &candidate,
        nullptr,
        0,
        nullptr);
    return applyResult == DISP_CHANGE_SUCCESSFUL;
}
}

namespace display {
bool ChangePrimaryResolution(const std::wstring& gdiDeviceName, int width, int height) {
    const ModeSnapshot previous = CaptureCurrent(gdiDeviceName);
    if (!previous.valid) {
        return false;
    }

    if (!ApplyMode(gdiDeviceName, static_cast<DWORD>(width), static_cast<DWORD>(height), previous.frequency, previous.bitsPerPel)) {
        return false;
    }

    SaveSnapshot(previous);
    return true;
}

bool RollbackPrimaryResolution(const std::wstring& gdiDeviceName) {
    const ModeSnapshot snapshot = LoadSnapshot();
    if (!snapshot.valid) {
        return false;
    }

    if (_wcsicmp(snapshot.deviceName.c_str(), gdiDeviceName.c_str()) != 0) {
        return false;
    }

    return ApplyMode(gdiDeviceName, snapshot.width, snapshot.height, snapshot.frequency, snapshot.bitsPerPel);
}
}
