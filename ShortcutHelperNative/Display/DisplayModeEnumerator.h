#pragma once

#include <string>
#include <utility>
#include <vector>

namespace display {
struct DisplayModeInfo {
	int width = 0;
	int height = 0;
	int frequency = 0;
	int bitsPerPel = 0;
};

std::vector<std::pair<int, int>> EnumerateModes(const std::wstring& gdiDeviceName);
std::vector<DisplayModeInfo> EnumerateDetailedModes(const std::wstring& gdiDeviceName);
bool ContainsMode(const std::vector<std::pair<int, int>>& modes, int width, int height);
bool ContainsModeAtCurrentTiming(const std::wstring& gdiDeviceName, int width, int height);
}
