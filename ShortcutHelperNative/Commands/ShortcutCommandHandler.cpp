#include "ShortcutCommandHandler.h"

#include <windows.h>

#include "../Native/KeyboardNativeMethods.h"

namespace {
constexpr DWORD kPostFocusSettleMs = 420;

void CloseGameBarAndWaitFocus() {
    keyboard::SendCombo({ {VK_LWIN,false}, {0x47,false} });

    int nonGameBarStreak = 0;
    for (int i = 1; i <= 8; ++i) {
        Sleep(i == 1 ? 260 : 160);
        const auto proc = keyboard::GetForegroundProcessName();
        if (!keyboard::IsGameBarProcess(proc)) {
            nonGameBarStreak++;
            if (nonGameBarStreak >= 2) {
                Sleep(kPostFocusSettleMs);
                return;
            }
        } else {
            nonGameBarStreak = 0;
        }
    }

    Sleep(kPostFocusSettleMs);
}
}

namespace commands {
bool IsShortcutCommand(const std::wstring& action) {
    return action == L"insert" ||
           action == L"altinsert" ||
           action == L"home" ||
           action == L"end" ||
           action == L"losslessscaling" ||
           action == L"quit";
}

void ExecuteShortcutCommand(const std::wstring& action) {
    CloseGameBarAndWaitFocus();

    if (action == L"insert") {
        keyboard::SendCombo({ {VK_INSERT, true} });
    } else if (action == L"altinsert") {
        keyboard::SendCombo({ {VK_MENU, false}, {VK_INSERT, true} });
    } else if (action == L"home") {
        keyboard::SendCombo({ {VK_HOME, true} });
    } else if (action == L"end") {
        keyboard::SendCombo({ {VK_END, true} });
    } else if (action == L"losslessscaling") {
        keyboard::SendCombo({ {VK_CONTROL, false}, {VK_MENU, false}, {0x53, false} });
    } else if (action == L"quit") {
        keyboard::SendCombo({ {VK_MENU, false}, {VK_F4, false} });
    }
}
}
