#pragma once

#include <string>

namespace display {
struct PrimaryDisplayInfo {
    bool valid = false;
    bool hasActiveExternalPath = true;
    bool primaryIsInternal = false;
    std::wstring primaryGdiDeviceName;
};

PrimaryDisplayInfo DetectPrimaryDisplayInfo();
}
