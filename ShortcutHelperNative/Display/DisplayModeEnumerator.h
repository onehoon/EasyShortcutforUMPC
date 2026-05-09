#pragma once

#include <string>
#include <utility>
#include <vector>

namespace display {
std::vector<std::pair<int, int>> EnumerateModes(const std::wstring& gdiDeviceName);
bool ContainsMode(const std::vector<std::pair<int, int>>& modes, int width, int height);
}
