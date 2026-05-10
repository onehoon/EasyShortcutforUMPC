#include "DisplayModeEnumerator.h"

#include <windows.h>

#include <set>

namespace display {
std::vector<DisplayModeInfo> EnumerateDetailedModes(const std::wstring& gdiDeviceName) {
    std::vector<DisplayModeInfo> modes;

    DEVMODEW dm{};
    dm.dmSize = sizeof(dm);
    for (DWORD modeIndex = 0; EnumDisplaySettingsExW(gdiDeviceName.c_str(), modeIndex, &dm, 0); ++modeIndex) {
        DisplayModeInfo mode;
        mode.width = static_cast<int>(dm.dmPelsWidth);
        mode.height = static_cast<int>(dm.dmPelsHeight);
        mode.frequency = static_cast<int>(dm.dmDisplayFrequency);
        mode.bitsPerPel = static_cast<int>(dm.dmBitsPerPel);
        modes.push_back(mode);
    }

    return modes;
}

std::vector<std::pair<int, int>> EnumerateModes(const std::wstring& gdiDeviceName) {
    std::set<std::pair<int, int>> uniqueModes;

    const auto detailed = EnumerateDetailedModes(gdiDeviceName);
    for (const auto& mode : detailed) {
        uniqueModes.insert({ mode.width, mode.height });
    }

    return std::vector<std::pair<int, int>>(uniqueModes.begin(), uniqueModes.end());
}

bool ContainsMode(const std::vector<std::pair<int, int>>& modes, int width, int height) {
    for (const auto& mode : modes) {
        if (mode.first == width && mode.second == height) {
            return true;
        }
    }
    return false;
}

bool ContainsModeAtCurrentTiming(const std::wstring& gdiDeviceName, int width, int height) {
    DEVMODEW current{};
    current.dmSize = sizeof(current);
    if (!EnumDisplaySettingsExW(gdiDeviceName.c_str(), ENUM_CURRENT_SETTINGS, &current, 0)) {
        return false;
    }

    const bool currentFrequencyIsDefault =
        current.dmDisplayFrequency == 0 || current.dmDisplayFrequency == 1;

    const auto detailed = EnumerateDetailedModes(gdiDeviceName);
    for (const auto& mode : detailed) {
        if (mode.width == width &&
            mode.height == height &&
            mode.bitsPerPel == static_cast<int>(current.dmBitsPerPel) &&
            (currentFrequencyIsDefault || mode.frequency == static_cast<int>(current.dmDisplayFrequency))) {
            return true;
        }
    }

    return false;
}

bool TryGetCurrentMode(const std::wstring& gdiDeviceName, DisplayModeInfo& modeOut) {
    DEVMODEW current{};
    current.dmSize = sizeof(current);
    if (!EnumDisplaySettingsExW(gdiDeviceName.c_str(), ENUM_CURRENT_SETTINGS, &current, 0)) {
        return false;
    }

    modeOut.width = static_cast<int>(current.dmPelsWidth);
    modeOut.height = static_cast<int>(current.dmPelsHeight);
    modeOut.frequency = static_cast<int>(current.dmDisplayFrequency);
    modeOut.bitsPerPel = static_cast<int>(current.dmBitsPerPel);
    return modeOut.width > 0 && modeOut.height > 0;
}
}
