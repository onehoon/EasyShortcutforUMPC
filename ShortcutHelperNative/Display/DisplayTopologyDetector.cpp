#include "DisplayTopologyDetector.h"

#include <windows.h>

#include <vector>

namespace {
bool IsInternalTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY tech) {
    return tech == DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL ||
           tech == DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED ||
           tech == DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED;
}

std::wstring GetPrimaryGdiName() {
    DISPLAY_DEVICEW dd{};
    dd.cb = sizeof(dd);
    for (DWORD i = 0; EnumDisplayDevicesW(nullptr, i, &dd, 0); ++i) {
        if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0 &&
            (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0) {
            return dd.DeviceName;
        }
    }

    return L"";
}

std::wstring TryGetSourceGdiName(const DISPLAYCONFIG_PATH_INFO& path) {
    DISPLAYCONFIG_SOURCE_DEVICE_NAME sourceName{};
    sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
    sourceName.header.size = sizeof(sourceName);
    sourceName.header.adapterId = path.sourceInfo.adapterId;
    sourceName.header.id = path.sourceInfo.id;
    if (DisplayConfigGetDeviceInfo(&sourceName.header) != ERROR_SUCCESS) {
        return L"";
    }

    return sourceName.viewGdiDeviceName;
}
}

namespace display {
PrimaryDisplayInfo DetectPrimaryDisplayInfo() {
    PrimaryDisplayInfo info{};

    UINT32 pathCount = 0;
    UINT32 modeCount = 0;
    LONG sizeResult = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, &pathCount, &modeCount);
    if (sizeResult != ERROR_SUCCESS || pathCount == 0) {
        return info;
    }

    std::vector<DISPLAYCONFIG_PATH_INFO> paths(pathCount);
    std::vector<DISPLAYCONFIG_MODE_INFO> modes(modeCount);
    LONG queryResult = QueryDisplayConfig(
        QDC_ONLY_ACTIVE_PATHS,
        &pathCount,
        paths.data(),
        &modeCount,
        modes.data(),
        nullptr);
    if (queryResult != ERROR_SUCCESS) {
        return info;
    }

    const auto primaryGdiName = GetPrimaryGdiName();
    if (primaryGdiName.empty()) {
        return info;
    }

    bool anyExternal = false;
    bool matchedPrimary = false;
    bool primaryIsInternal = false;

    for (const auto& path : paths) {
        const bool pathIsInternal = IsInternalTechnology(path.targetInfo.outputTechnology);
        if (!pathIsInternal) {
            anyExternal = true;
        }

        const auto gdiName = TryGetSourceGdiName(path);
        if (!gdiName.empty() && _wcsicmp(gdiName.c_str(), primaryGdiName.c_str()) == 0) {
            matchedPrimary = true;
            primaryIsInternal = pathIsInternal;
        }
    }

    if (!matchedPrimary) {
        return info;
    }

    info.valid = true;
    info.hasActiveExternalPath = anyExternal;
    info.primaryIsInternal = primaryIsInternal;
    info.primaryGdiDeviceName = primaryGdiName;
    return info;
}
}
