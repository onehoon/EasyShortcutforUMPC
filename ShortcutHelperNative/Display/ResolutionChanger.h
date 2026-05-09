#pragma once

#include <string>

namespace display {
bool ChangePrimaryResolution(const std::wstring& gdiDeviceName, int width, int height);
bool RollbackPrimaryResolution(const std::wstring& gdiDeviceName);
}
