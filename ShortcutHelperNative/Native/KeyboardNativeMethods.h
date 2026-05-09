#pragma once

#include <windows.h>
#include <string>
#include <vector>

namespace keyboard {
std::wstring GetForegroundProcessName();
bool IsGameBarProcess(const std::wstring& name);
void SendCombo(const std::vector<std::pair<WORD, bool>>& keys);
}
