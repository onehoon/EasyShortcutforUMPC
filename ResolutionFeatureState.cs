using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Easy_Shortcut_for_UMPC
{
    internal enum ResolutionPresetGroup
    {
        None,
        Group1200,
        Group1080
    }

    internal sealed class ResolutionFeatureState
    {
        internal bool Available { get; set; }
        internal ResolutionPresetGroup Group { get; set; }

        internal static readonly ResolutionFeatureState Unavailable = new ResolutionFeatureState
        {
            Available = false,
            Group = ResolutionPresetGroup.None
        };
    }

    internal static class ResolutionFeatureStateStore
    {
        private const string StateFileName = "resolution_state.txt";

        internal static async Task<ResolutionFeatureState> WaitForStateRefreshAsync(DateTimeOffset launchedAtUtc, int timeoutMs)
        {
            var start = DateTimeOffset.UtcNow;
            while ((DateTimeOffset.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                var state = await TryReadStateAsync(launchedAtUtc);
                if (state != null)
                {
                    return state;
                }

                await Task.Delay(120);
            }

            return ResolutionFeatureState.Unavailable;
        }

        private static async Task<ResolutionFeatureState> TryReadStateAsync(DateTimeOffset launchedAtUtc)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(StateFileName) as StorageFile;
                if (file == null)
                {
                    return null;
                }

                BasicProperties props = await file.GetBasicPropertiesAsync();
                if (props.DateModified < launchedAtUtc)
                {
                    return null;
                }

                string content = await FileIO.ReadTextAsync(file);
                return Parse(content);
            }
            catch
            {
                return ResolutionFeatureState.Unavailable;
            }
        }

        private static ResolutionFeatureState Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ResolutionFeatureState.Unavailable;
            }

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int sep = rawLine.IndexOf('=');
                if (sep <= 0 || sep >= rawLine.Length - 1)
                {
                    continue;
                }

                string key = rawLine.Substring(0, sep).Trim();
                string value = rawLine.Substring(sep + 1).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    values[key] = value;
                }
            }

            bool available = values.TryGetValue("available", out string availableValue) && availableValue == "1";
            if (!available)
            {
                return ResolutionFeatureState.Unavailable;
            }

            ResolutionPresetGroup group = ResolutionPresetGroup.None;
            if (values.TryGetValue("group", out string groupValue))
            {
                if (string.Equals(groupValue, "1200", StringComparison.OrdinalIgnoreCase))
                {
                    group = ResolutionPresetGroup.Group1200;
                }
                else if (string.Equals(groupValue, "1080", StringComparison.OrdinalIgnoreCase))
                {
                    group = ResolutionPresetGroup.Group1080;
                }
            }

            if (group == ResolutionPresetGroup.None)
            {
                return ResolutionFeatureState.Unavailable;
            }

            return new ResolutionFeatureState
            {
                Available = true,
                Group = group
            };
        }
    }
}
