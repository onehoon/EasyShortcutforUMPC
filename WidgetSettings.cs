using System;
using System.Collections.Generic;
using System.IO;
using Windows.Data.Json;

namespace Easy_Shortcut_for_UMPC
{
    internal static class WidgetSettingsDefaults
    {
        internal static readonly IReadOnlyList<string> DefaultLosslessKeys = new[] { "Ctrl", "Alt", "S" };
        internal static readonly IReadOnlyList<string> DefaultSectionOrder = new[] { "overlay", "resolution", "custom" };

        internal static WidgetSettings Create()
        {
            return new WidgetSettings
            {
                Version = 1,
                BuiltInLosslessKeys = new List<string>(DefaultLosslessKeys),
                SectionOrder = new List<string>(DefaultSectionOrder),
                CustomShortcuts = new Dictionary<string, CustomShortcutSlot>(StringComparer.OrdinalIgnoreCase)
                {
                    ["custom1"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 1", Keys = new List<string>() },
                    ["custom2"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 2", Keys = new List<string>() },
                    ["custom3"] = new CustomShortcutSlot { Enabled = false, Label = "Custom 3", Keys = new List<string>() }
                }
            };
        }
    }

    internal sealed class WidgetSettings
    {
        internal int Version { get; set; }
        internal List<string> BuiltInLosslessKeys { get; set; } = new List<string>();
        internal Dictionary<string, CustomShortcutSlot> CustomShortcuts { get; set; } = new Dictionary<string, CustomShortcutSlot>(StringComparer.OrdinalIgnoreCase);
        internal List<string> SectionOrder { get; set; } = new List<string>();
    }

    internal sealed class CustomShortcutSlot
    {
        internal bool Enabled { get; set; }
        internal string Label { get; set; }
        internal List<string> Keys { get; set; } = new List<string>();
    }

    internal static class WidgetSettingsStore
    {
        private const string SettingsFileName = "widget_settings.json";

        internal static WidgetSettings Load()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                if (!File.Exists(filePath))
                {
                    var defaults = WidgetSettingsDefaults.Create();
                    Save(defaults);
                    return defaults;
                }

                string raw = File.ReadAllText(filePath);
                var parsed = Parse(raw);
                if (parsed == null)
                {
                    var defaults = WidgetSettingsDefaults.Create();
                    Save(defaults);
                    return defaults;
                }

                return parsed;
            }
            catch
            {
                return WidgetSettingsDefaults.Create();
            }
        }

        internal static void Save(WidgetSettings settings)
        {
            var normalized = Normalize(settings);
            string filePath = GetSettingsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, Serialize(normalized));
        }

        internal static WidgetSettings Normalize(WidgetSettings input)
        {
            var defaults = WidgetSettingsDefaults.Create();
            if (input == null)
            {
                return defaults;
            }

            var result = WidgetSettingsDefaults.Create();
            result.Version = input.Version > 0 ? input.Version : defaults.Version;

            if (IsValidKeys(input.BuiltInLosslessKeys))
            {
                result.BuiltInLosslessKeys = new List<string>(input.BuiltInLosslessKeys);
            }

            foreach (string slotId in new[] { "custom1", "custom2", "custom3" })
            {
                if (input.CustomShortcuts != null && input.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) && slot != null)
                {
                    result.CustomShortcuts[slotId] = new CustomShortcutSlot
                    {
                        Enabled = slot.Enabled && IsValidKeys(slot.Keys),
                        Label = string.IsNullOrWhiteSpace(slot.Label) ? result.CustomShortcuts[slotId].Label : slot.Label.Trim(),
                        Keys = IsValidKeys(slot.Keys) ? new List<string>(slot.Keys) : new List<string>()
                    };
                }
            }

            if (input.SectionOrder != null)
            {
                var cleaned = new List<string>();
                foreach (string section in input.SectionOrder)
                {
                    if (string.Equals(section, "overlay", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(section, "resolution", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(section, "custom", StringComparison.OrdinalIgnoreCase))
                    {
                        string normalized = section.ToLowerInvariant();
                        if (!cleaned.Contains(normalized))
                        {
                            cleaned.Add(normalized);
                        }
                    }
                }

                if (cleaned.Count == 3)
                {
                    result.SectionOrder = cleaned;
                }
            }

            return result;
        }

        internal static bool IsConfigured(CustomShortcutSlot slot)
        {
            return slot != null && slot.Enabled && IsValidKeys(slot.Keys);
        }

        internal static bool IsValidKeys(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(keys[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetSettingsFilePath()
        {
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyShortcutForUMPC");
            return Path.Combine(baseDir, SettingsFileName);
        }

        private static string Serialize(WidgetSettings settings)
        {
            var root = new JsonObject();
            root["version"] = JsonValue.CreateNumberValue(settings.Version);

            var builtIn = new JsonObject();
            var ls = new JsonObject();
            var lsKeys = new JsonArray();
            foreach (string key in settings.BuiltInLosslessKeys)
            {
                lsKeys.Add(JsonValue.CreateStringValue(key));
            }

            ls["keys"] = lsKeys;
            builtIn["losslessScaling"] = ls;
            root["builtInShortcuts"] = builtIn;

            var customRoot = new JsonObject();
            foreach (var slotId in new[] { "custom1", "custom2", "custom3" })
            {
                var slot = settings.CustomShortcuts[slotId];
                var slotObj = new JsonObject
                {
                    ["enabled"] = JsonValue.CreateBooleanValue(slot.Enabled),
                    ["label"] = JsonValue.CreateStringValue(slot.Label ?? string.Empty)
                };

                var keys = new JsonArray();
                foreach (string key in slot.Keys)
                {
                    keys.Add(JsonValue.CreateStringValue(key));
                }

                slotObj["keys"] = keys;
                customRoot[slotId] = slotObj;
            }

            root["customShortcuts"] = customRoot;

            var order = new JsonArray();
            foreach (string section in settings.SectionOrder)
            {
                order.Add(JsonValue.CreateStringValue(section));
            }

            root["sectionOrder"] = order;
            return root.Stringify();
        }

        private static WidgetSettings Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (!JsonObject.TryParse(raw, out JsonObject root))
            {
                return null;
            }

            var parsed = WidgetSettingsDefaults.Create();

            if (root.TryGetValue("version", out IJsonValue versionValue) && versionValue.ValueType == JsonValueType.Number)
            {
                parsed.Version = (int)versionValue.GetNumber();
            }

            if (root.TryGetValue("builtInShortcuts", out IJsonValue builtInValue) && builtInValue.ValueType == JsonValueType.Object)
            {
                var builtInObj = builtInValue.GetObject();
                if (builtInObj.TryGetValue("losslessScaling", out IJsonValue losslessValue) && losslessValue.ValueType == JsonValueType.Object)
                {
                    var losslessObj = losslessValue.GetObject();
                    parsed.BuiltInLosslessKeys = ReadStringArray(losslessObj, "keys");
                }
            }

            if (root.TryGetValue("customShortcuts", out IJsonValue customValue) && customValue.ValueType == JsonValueType.Object)
            {
                var customObj = customValue.GetObject();
                foreach (string slotId in new[] { "custom1", "custom2", "custom3" })
                {
                    if (customObj.TryGetValue(slotId, out IJsonValue slotValue) && slotValue.ValueType == JsonValueType.Object)
                    {
                        var slotObj = slotValue.GetObject();
                        var slot = parsed.CustomShortcuts[slotId];

                        if (slotObj.TryGetValue("enabled", out IJsonValue enabledValue) && enabledValue.ValueType == JsonValueType.Boolean)
                        {
                            slot.Enabled = enabledValue.GetBoolean();
                        }

                        if (slotObj.TryGetValue("label", out IJsonValue labelValue) && labelValue.ValueType == JsonValueType.String)
                        {
                            slot.Label = labelValue.GetString();
                        }

                        slot.Keys = ReadStringArray(slotObj, "keys");
                    }
                }
            }

            if (root.TryGetValue("sectionOrder", out IJsonValue orderValue) && orderValue.ValueType == JsonValueType.Array)
            {
                parsed.SectionOrder = new List<string>();
                foreach (IJsonValue value in orderValue.GetArray())
                {
                    if (value.ValueType == JsonValueType.String)
                    {
                        parsed.SectionOrder.Add(value.GetString());
                    }
                }
            }

            return Normalize(parsed);
        }

        private static List<string> ReadStringArray(JsonObject source, string key)
        {
            var result = new List<string>();
            if (!source.TryGetValue(key, out IJsonValue value) || value.ValueType != JsonValueType.Array)
            {
                return result;
            }

            foreach (IJsonValue item in value.GetArray())
            {
                if (item.ValueType == JsonValueType.String)
                {
                    string text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add(text.Trim());
                    }
                }
            }

            return result;
        }
    }
}
