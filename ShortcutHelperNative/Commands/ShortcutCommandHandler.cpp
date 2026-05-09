#include "ShortcutCommandHandler.h"

#include <windows.h>

#include <filesystem>
#include <fstream>
#include <map>
#include <string>
#include <vector>

#include "../Native/KeyboardNativeMethods.h"

namespace {
constexpr DWORD kPostFocusSettleMs = 420;

std::wstring ToLower(const std::wstring& value) {
    std::wstring lowered = value;
    for (auto& ch : lowered) {
        ch = towlower(ch);
    }

    return lowered;
}

std::wstring Trim(const std::wstring& value) {
    size_t start = 0;
    while (start < value.size() && iswspace(value[start])) {
        ++start;
    }

    size_t end = value.size();
    while (end > start && iswspace(value[end - 1])) {
        --end;
    }

    return value.substr(start, end - start);
}

std::filesystem::path GetSettingsPath() {
    wchar_t* localAppData = nullptr;
    size_t len = 0;
    _wdupenv_s(&localAppData, &len, L"LOCALAPPDATA");
    std::filesystem::path base = localAppData != nullptr ? localAppData : L"";
    if (localAppData != nullptr) {
        free(localAppData);
    }

    return base / L"EasyShortcutForUMPC" / L"widget_settings.json";
}

std::wstring ReadTextFile(const std::filesystem::path& path) {
    std::wifstream in(path);
    if (!in) {
        return L"";
    }

    std::wstring content;
    std::wstring line;
    while (std::getline(in, line)) {
        content += line;
    }

    return content;
}

std::wstring ExtractObjectByName(const std::wstring& source, const std::wstring& name, size_t searchStart = 0) {
    std::wstring quoted = L"\"" + name + L"\"";
    size_t keyPos = source.find(quoted, searchStart);
    if (keyPos == std::wstring::npos) {
        return L"";
    }

    size_t braceStart = source.find(L"{", keyPos);
    if (braceStart == std::wstring::npos) {
        return L"";
    }

    int depth = 0;
    for (size_t i = braceStart; i < source.size(); ++i) {
        if (source[i] == L'{') {
            ++depth;
        } else if (source[i] == L'}') {
            --depth;
            if (depth == 0) {
                return source.substr(braceStart, i - braceStart + 1);
            }
        }
    }

    return L"";
}

std::vector<std::wstring> ExtractStringArray(const std::wstring& source, const std::wstring& key) {
    std::vector<std::wstring> values;
    std::wstring quotedKey = L"\"" + key + L"\"";
    size_t keyPos = source.find(quotedKey);
    if (keyPos == std::wstring::npos) {
        return values;
    }

    size_t start = source.find(L"[", keyPos);
    size_t end = source.find(L"]", start == std::wstring::npos ? keyPos : start);
    if (start == std::wstring::npos || end == std::wstring::npos || end <= start) {
        return values;
    }

    std::wstring body = source.substr(start + 1, end - start - 1);
    size_t pos = 0;
    while (pos < body.size()) {
        size_t open = body.find(L'\"', pos);
        if (open == std::wstring::npos) {
            break;
        }

        size_t close = body.find(L'\"', open + 1);
        if (close == std::wstring::npos) {
            break;
        }

        std::wstring token = Trim(body.substr(open + 1, close - open - 1));
        if (!token.empty()) {
            values.push_back(token);
        }

        pos = close + 1;
    }

    return values;
}

bool ExtractBool(const std::wstring& source, const std::wstring& key, bool fallback) {
    std::wstring quotedKey = L"\"" + key + L"\"";
    size_t keyPos = source.find(quotedKey);
    if (keyPos == std::wstring::npos) {
        return fallback;
    }

    size_t colon = source.find(L":", keyPos);
    if (colon == std::wstring::npos) {
        return fallback;
    }

    size_t valueStart = colon + 1;
    while (valueStart < source.size() && iswspace(source[valueStart])) {
        ++valueStart;
    }

    if (source.compare(valueStart, 4, L"true") == 0) {
        return true;
    }

    if (source.compare(valueStart, 5, L"false") == 0) {
        return false;
    }

    return fallback;
}

WORD MapKeyNameToVirtualKey(const std::wstring& keyName) {
    static const std::map<std::wstring, WORD> named = {
        {L"insert", VK_INSERT}, {L"delete", VK_DELETE}, {L"home", VK_HOME}, {L"end", VK_END},
        {L"page up", VK_PRIOR}, {L"page down", VK_NEXT}, {L"space", VK_SPACE}, {L"tab", VK_TAB},
        {L"escape", VK_ESCAPE}, {L"arrow up", VK_UP}, {L"arrow down", VK_DOWN}, {L"arrow left", VK_LEFT}, {L"arrow right", VK_RIGHT}
    };

    std::wstring k = ToLower(Trim(keyName));
    auto it = named.find(k);
    if (it != named.end()) {
        return it->second;
    }

    if (k.size() == 1) {
        wchar_t c = k[0];
        if (c >= L'a' && c <= L'z') {
            return static_cast<WORD>(towupper(c));
        }

        if (c >= L'0' && c <= L'9') {
            return static_cast<WORD>(c);
        }
    }

    if (k.size() >= 2 && k[0] == L'f') {
        int n = _wtoi(k.substr(1).c_str());
        if (n >= 1 && n <= 12) {
            return static_cast<WORD>(VK_F1 + (n - 1));
        }
    }

    return 0;
}

std::vector<std::pair<WORD, bool>> BuildComboFromKeys(const std::vector<std::wstring>& keys) {
    std::vector<std::pair<WORD, bool>> combo;
    WORD primaryKey = 0;
    bool ctrl = false;
    bool alt = false;
    bool shift = false;

    for (const auto& raw : keys) {
        std::wstring token = ToLower(Trim(raw));
        if (token == L"ctrl" || token == L"control") {
            ctrl = true;
            continue;
        }

        if (token == L"alt") {
            alt = true;
            continue;
        }

        if (token == L"shift") {
            shift = true;
            continue;
        }

        if (primaryKey == 0) {
            primaryKey = MapKeyNameToVirtualKey(token);
        }
    }

    if (primaryKey == 0) {
        return {};
    }

    if (ctrl) {
        combo.push_back({VK_CONTROL, false});
    }

    if (alt) {
        combo.push_back({VK_MENU, false});
    }

    if (shift) {
        combo.push_back({VK_SHIFT, false});
    }

    combo.push_back({primaryKey, false});
    return combo;
}

std::vector<std::pair<WORD, bool>> GetLosslessComboFromSettings() {
    std::wstring json = ReadTextFile(GetSettingsPath());
    if (json.empty()) {
        return {{VK_CONTROL, false}, {VK_MENU, false}, {0x53, false}};
    }

    std::wstring builtIn = ExtractObjectByName(json, L"builtInShortcuts");
    std::wstring lossless = builtIn.empty() ? L"" : ExtractObjectByName(builtIn, L"losslessScaling");
    std::vector<std::wstring> keys = lossless.empty() ? std::vector<std::wstring>() : ExtractStringArray(lossless, L"keys");
    auto combo = BuildComboFromKeys(keys);
    if (combo.empty()) {
        return {{VK_CONTROL, false}, {VK_MENU, false}, {0x53, false}};
    }

    return combo;
}

std::vector<std::pair<WORD, bool>> GetCustomComboFromSettings(const std::wstring& slotName) {
    std::wstring json = ReadTextFile(GetSettingsPath());
    if (json.empty()) {
        return {};
    }

    std::wstring customRoot = ExtractObjectByName(json, L"customShortcuts");
    if (customRoot.empty()) {
        return {};
    }

    std::wstring slot = ExtractObjectByName(customRoot, slotName);
    if (slot.empty() || !ExtractBool(slot, L"enabled", false)) {
        return {};
    }

    std::vector<std::wstring> keys = ExtractStringArray(slot, L"keys");
    return BuildComboFromKeys(keys);
}

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
           action == L"custom1" ||
           action == L"custom2" ||
           action == L"custom3" ||
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
    } else if (action == L"custom1") {
        auto combo = GetCustomComboFromSettings(L"custom1");
        if (!combo.empty()) {
            keyboard::SendCombo(combo);
        }
    } else if (action == L"custom2") {
        auto combo = GetCustomComboFromSettings(L"custom2");
        if (!combo.empty()) {
            keyboard::SendCombo(combo);
        }
    } else if (action == L"custom3") {
        auto combo = GetCustomComboFromSettings(L"custom3");
        if (!combo.empty()) {
            keyboard::SendCombo(combo);
        }
    } else if (action == L"home") {
        keyboard::SendCombo({ {VK_HOME, true} });
    } else if (action == L"end") {
        keyboard::SendCombo({ {VK_END, true} });
    } else if (action == L"losslessscaling") {
        keyboard::SendCombo(GetLosslessComboFromSettings());
    } else if (action == L"quit") {
        keyboard::SendCombo({ {VK_MENU, false}, {VK_F4, false} });
    }
}
}
