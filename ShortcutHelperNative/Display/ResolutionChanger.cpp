#include "ResolutionChanger.h"

#include <windows.h>

namespace {
bool CaptureCurrent(const std::wstring& gdiDeviceName, DEVMODEW* outCurrent) {
    if (outCurrent == nullptr) {
        return false;
    }

    DEVMODEW current{};
    current.dmSize = sizeof(current);
    if (!EnumDisplaySettingsExW(gdiDeviceName.c_str(), ENUM_CURRENT_SETTINGS, &current, 0)) {
        return false;
    }

    *outCurrent = current;
    return true;
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
    DEVMODEW current{};
    if (!CaptureCurrent(gdiDeviceName, &current)) {
        return false;
    }

    if (!ApplyMode(
            gdiDeviceName,
            static_cast<DWORD>(width),
            static_cast<DWORD>(height),
            current.dmDisplayFrequency,
            current.dmBitsPerPel)) {
        return false;
    }

    return true;
}
}
