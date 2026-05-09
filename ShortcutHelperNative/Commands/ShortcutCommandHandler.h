#pragma once

#include <string>

namespace commands {
bool IsShortcutCommand(const std::wstring& action);
void ExecuteShortcutCommand(const std::wstring& action);
}
