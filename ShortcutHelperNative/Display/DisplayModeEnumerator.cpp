#include "DisplayModeEnumerator.h"

#include <windows.h>

#include <set>

namespace display {
std::vector<std::pair<int, int>> EnumerateModes(const std::wstring& gdiDeviceName) {
    std::set<std::pair<int, int>> uniqueModes;

    DEVMODEW dm{};
    dm.dmSize = sizeof(dm);
    for (DWORD modeIndex = 0; EnumDisplaySettingsExW(gdiDeviceName.c_str(), modeIndex, &dm, 0); ++modeIndex) {
        uniqueModes.insert({ static_cast<int>(dm.dmPelsWidth), static_cast<int>(dm.dmPelsHeight) });
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
}
