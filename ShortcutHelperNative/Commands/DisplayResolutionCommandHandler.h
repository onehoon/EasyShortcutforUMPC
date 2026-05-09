#pragma once

#include <string>

namespace commands {
bool IsDisplayResolutionCommand(const std::wstring& action);
void ExecuteDisplayResolutionCommand(const std::wstring& action);
}
