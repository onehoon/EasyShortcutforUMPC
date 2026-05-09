#include "ResolutionChanger.h"

#include <windows.h>

namespace display {
bool ChangePrimaryResolution(const std::wstring& gdiDeviceName, int width, int height) {
    DEVMODEW current{};
    current.dmSize = sizeof(current);
    if (!EnumDisplaySettingsExW(gdiDeviceName.c_str(), ENUM_CURRENT_SETTINGS, &current, 0)) {
        return false;
    }

    DEVMODEW candidate{};
    candidate.dmSize = sizeof(candidate);
    candidate.dmPelsWidth = static_cast<DWORD>(width);
    candidate.dmPelsHeight = static_cast<DWORD>(height);
    candidate.dmDisplayFrequency = current.dmDisplayFrequency;
    candidate.dmBitsPerPel = current.dmBitsPerPel;
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
