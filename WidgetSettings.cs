using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Data.Json;
using Windows.Storage;

namespace Easy_Shortcut_for_UMPC
{
    internal static class WidgetSettingsDefaults
    {
        internal const string SectionTopShortcuts = "topShortcuts";
        internal const string SectionLosslessScaling = "LosslessScaling";
        internal const string SectionOverlay = "overlay";
        internal const string SectionResolution = "resolution";
        internal const string SectionCustom = "custom";
        internal const string TopShortcutOrderLosslessFirst = "losslessFirst";
        internal const string TopShortcutOrderOverlayFirst = "overlayFirst";
        internal const string DefaultOverlayDisplayName = "OptiScaler Overlay";
        internal const int OverlayDisplayNameMaxLength = 24;

        internal static readonly IReadOnlyList<string> DefaultLosslessKeys = new[] { "Ctrl", "Alt", "S" };
        internal static readonly IReadOnlyList<string> DefaultOverlayKeys = new[] { "Insert" };
        internal static readonly IReadOnlyList<string> DefaultSectionOrder = new[]
        {
            SectionTopShortcuts,
            SectionResolution,
            SectionCustom
        };

        internal static WidgetSettings Create()
        {
            return new WidgetSettings
            {
                Version = 2,
                TopShortcutOrder = TopShortcutOrderLosslessFirst,
                BuiltInLosslessKeys = new List<string>(DefaultLosslessKeys),
                BuiltInOverlayKeys = new List<string>(DefaultOverlayKeys),
                OverlayDisplayName = DefaultOverlayDisplayName,
                SectionOrder = new List<string>(DefaultSectionOrder),
                CustomShortcuts = new Dictionary<string, CustomShortcutSlot>(StringComparer.OrdinalIgnoreCase)
                {
                    ["custom1"] = new CustomShortcutSlot { Keys = new List<string>() },
                    ["custom2"] = new CustomShortcutSlot { Keys = new List<string>() },
                    ["custom3"] = new CustomShortcutSlot { Keys = new List<string>() }
                }
            };
        }
    }

    internal sealed class WidgetSettings
    {
        internal int Version { get; set; }
        internal List<string> BuiltInLosslessKeys { get; set; } = new List<string>();
        internal List<string> BuiltInOverlayKeys { get; set; } = new List<string>();
        internal string OverlayDisplayName { get; set; } = WidgetSettingsDefaults.DefaultOverlayDisplayName;
        internal Dictionary<string, CustomShortcutSlot> CustomShortcuts { get; set; } = new Dictionary<string, CustomShortcutSlot>(StringComparer.OrdinalIgnoreCase);
        internal List<string> SectionOrder { get; set; } = new List<string>();
        internal string TopShortcutOrder { get; set; } = WidgetSettingsDefaults.TopShortcutOrderLosslessFirst;
    }

    internal sealed class CustomShortcutSlot
    {
        internal List<string> Keys { get; set; } = new List<string>();
    }

    internal static class WidgetSettingsStore
    {
        private const string SettingsFileName = "widget_settings.json";
        internal static event EventHandler SettingsSaved;

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
            SettingsSaved?.Invoke(null, EventArgs.Empty);
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
            result.Version = Math.Max(result.Version, 2);
            result.TopShortcutOrder = NormalizeTopShortcutOrder(input.TopShortcutOrder);
            result.OverlayDisplayName = NormalizeOverlayDisplayName(input.OverlayDisplayName);

            if (IsValidKeys(input.BuiltInLosslessKeys))
            {
                result.BuiltInLosslessKeys = input.BuiltInLosslessKeys.Select(k => k.Trim()).ToList();
            }

            if (IsValidKeys(input.BuiltInOverlayKeys))
            {
                result.BuiltInOverlayKeys = input.BuiltInOverlayKeys.Select(k => k.Trim()).ToList();
            }

            foreach (string slotId in new[] { "custom1", "custom2", "custom3" })
            {
                if (input.CustomShortcuts != null && input.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) && slot != null)
                {
                    result.CustomShortcuts[slotId] = new CustomShortcutSlot
                    {
                        Keys = IsValidKeys(slot.Keys) ? slot.Keys.Select(k => k.Trim()).ToList() : new List<string>()
                    };
                }
            }

            if (input.SectionOrder != null)
            {
                var cleaned = new List<string>();
                foreach (string section in input.SectionOrder)
                {
                    string normalizedSection = NormalizeSectionId(section);
                    if (!string.IsNullOrEmpty(normalizedSection) && !cleaned.Contains(normalizedSection, StringComparer.OrdinalIgnoreCase))
                    {
                        cleaned.Add(normalizedSection);
                    }
                }

                if (!cleaned.Contains(WidgetSettingsDefaults.SectionTopShortcuts, StringComparer.OrdinalIgnoreCase))
                {
                    cleaned.Insert(0, WidgetSettingsDefaults.SectionTopShortcuts);
                }

                foreach (string defaultSection in WidgetSettingsDefaults.DefaultSectionOrder)
                {
                    if (!cleaned.Contains(defaultSection, StringComparer.OrdinalIgnoreCase))
                    {
                        cleaned.Add(defaultSection);
                    }
                }

                result.SectionOrder = cleaned;
            }

            return result;
        }

        internal static bool IsConfigured(CustomShortcutSlot slot)
        {
            return slot != null && IsValidKeys(slot.Keys);
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

            string primaryKey = keys[keys.Count - 1].Trim();
            if (string.Equals(primaryKey, "Not Set", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IsModifierToken(primaryKey))
            {
                return false;
            }

            for (int i = 0; i < keys.Count - 1; i++)
            {
                if (!IsModifierToken(keys[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsModifierToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string normalized = token.Trim();
            return string.Equals(normalized, "Ctrl", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Control", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Alt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Shift", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSectionId(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
            {
                return null;
            }

            string normalized = section.Trim();
            if (string.Equals(normalized, WidgetSettingsDefaults.SectionTopShortcuts, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, WidgetSettingsDefaults.SectionLosslessScaling, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "losslessscaling", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, WidgetSettingsDefaults.SectionOverlay, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "overlay", StringComparison.OrdinalIgnoreCase))
            {
                return WidgetSettingsDefaults.SectionTopShortcuts;
            }

            if (string.Equals(normalized, WidgetSettingsDefaults.SectionResolution, StringComparison.OrdinalIgnoreCase))
            {
                return WidgetSettingsDefaults.SectionResolution;
            }

            if (string.Equals(normalized, WidgetSettingsDefaults.SectionCustom, StringComparison.OrdinalIgnoreCase))
            {
                return WidgetSettingsDefaults.SectionCustom;
            }

            return null;
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, SettingsFileName);
        }

        private static string Serialize(WidgetSettings settings)
        {
            var root = new JsonObject();
            root["version"] = JsonValue.CreateNumberValue(settings.Version);
            root["topShortcutOrder"] = JsonValue.CreateStringValue(NormalizeTopShortcutOrder(settings.TopShortcutOrder));
            root["overlayDisplayName"] = JsonValue.CreateStringValue(NormalizeOverlayDisplayName(settings.OverlayDisplayName));

            var builtIn = new JsonObject();
            var ls = new JsonObject();
            var lsKeys = new JsonArray();
            foreach (string key in settings.BuiltInLosslessKeys)
            {
                lsKeys.Add(JsonValue.CreateStringValue(key));
            }

            ls["keys"] = lsKeys;
            builtIn["losslessScaling"] = ls;

            var overlay = new JsonObject();
            var overlayKeys = new JsonArray();
            foreach (string key in settings.BuiltInOverlayKeys)
            {
                overlayKeys.Add(JsonValue.CreateStringValue(key));
            }

            overlay["keys"] = overlayKeys;
            builtIn["overlay"] = overlay;
            root["builtInShortcuts"] = builtIn;

            var customRoot = new JsonObject();
            foreach (var slotId in new[] { "custom1", "custom2", "custom3" })
            {
                var slot = settings.CustomShortcuts[slotId];
                var slotObj = new JsonObject();

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

            if (root.TryGetValue("topShortcutOrder", out IJsonValue topOrderValue) &&
                topOrderValue.ValueType == JsonValueType.String)
            {
                parsed.TopShortcutOrder = topOrderValue.GetString();
            }

            if (root.TryGetValue("overlayDisplayName", out IJsonValue overlayNameValue) &&
                overlayNameValue.ValueType == JsonValueType.String)
            {
                parsed.OverlayDisplayName = overlayNameValue.GetString();
            }

            if (root.TryGetValue("builtInShortcuts", out IJsonValue builtInValue) && builtInValue.ValueType == JsonValueType.Object)
            {
                var builtInObj = builtInValue.GetObject();
                if (builtInObj.TryGetValue("losslessScaling", out IJsonValue losslessValue) && losslessValue.ValueType == JsonValueType.Object)
                {
                    var losslessObj = losslessValue.GetObject();
                    parsed.BuiltInLosslessKeys = ReadStringArray(losslessObj, "keys");
                }

                if (builtInObj.TryGetValue("overlay", out IJsonValue overlayValue) && overlayValue.ValueType == JsonValueType.Object)
                {
                    var overlayObj = overlayValue.GetObject();
                    parsed.BuiltInOverlayKeys = ReadStringArray(overlayObj, "keys");
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

        private static string NormalizeTopShortcutOrder(string value)
        {
            if (string.Equals(value, WidgetSettingsDefaults.TopShortcutOrderOverlayFirst, StringComparison.OrdinalIgnoreCase))
            {
                return WidgetSettingsDefaults.TopShortcutOrderOverlayFirst;
            }

            return WidgetSettingsDefaults.TopShortcutOrderLosslessFirst;
        }

        internal static string NormalizeOverlayDisplayName(string value)
        {
            string normalized = (value ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return WidgetSettingsDefaults.DefaultOverlayDisplayName;
            }

            if (normalized.Length > WidgetSettingsDefaults.OverlayDisplayNameMaxLength)
            {
                normalized = normalized.Substring(0, WidgetSettingsDefaults.OverlayDisplayNameMaxLength);
            }

            return normalized;
        }
    }
}
