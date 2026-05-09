using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Easy_Shortcut_for_UMPC
{
    public sealed partial class WidgetSettingsPage : Page
    {
        private static readonly IReadOnlyList<string> ModifierOptions = new[]
        {
            "None", "Ctrl", "Alt", "Shift", "Ctrl + Alt", "Ctrl + Shift", "Alt + Shift", "Ctrl + Alt + Shift"
        };

        private static readonly IReadOnlyList<string> KeyOptions = new[]
        {
            "Not Set",
            "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
            "0","1","2","3","4","5","6","7","8","9",
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "Insert","Delete","Home","End","Page Up","Page Down","Space","Tab","Escape",
            "Arrow Up","Arrow Down","Arrow Left","Arrow Right"
        };

        private WidgetSettings _draft;
        private XboxGameBarWidget _gameBarWidget;

        public WidgetSettingsPage()
        {
            InitializeComponent();
            InitializeCombos();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _gameBarWidget = e.Parameter as XboxGameBarWidget;
            LoadDraft();
        }

        private void InitializeCombos()
        {
            SetComboItems(LosslessModifierCombo, ModifierOptions);
            SetComboItems(LosslessKeyCombo, KeyOptions);

            SetComboItems(Custom1ModifierCombo, ModifierOptions);
            SetComboItems(Custom1KeyCombo, KeyOptions);
            SetComboItems(Custom2ModifierCombo, ModifierOptions);
            SetComboItems(Custom2KeyCombo, KeyOptions);
            SetComboItems(Custom3ModifierCombo, ModifierOptions);
            SetComboItems(Custom3KeyCombo, KeyOptions);
        }

        private void LoadDraft()
        {
            _draft = WidgetSettingsStore.Normalize(WidgetSettingsStore.Load());

            SplitShortcut(_draft.BuiltInLosslessKeys, out string losslessModifier, out string losslessKey);
            LosslessModifierCombo.SelectedItem = ModifierOptions.Contains(losslessModifier) ? losslessModifier : "Ctrl + Alt";
            LosslessKeyCombo.SelectedItem = KeyOptions.Contains(losslessKey) ? losslessKey : "S";

            BindCustomSlot("custom1", Custom1ModifierCombo, Custom1KeyCombo);
            BindCustomSlot("custom2", Custom2ModifierCombo, Custom2KeyCombo);
            BindCustomSlot("custom3", Custom3ModifierCombo, Custom3KeyCombo);

            RenderSectionOrder();
            ValidationTextBlock.Visibility = Visibility.Collapsed;
            ValidationTextBlock.Text = string.Empty;
        }

        private void BindCustomSlot(string slotId, ComboBox modifier, ComboBox key)
        {
            if (!_draft.CustomShortcuts.TryGetValue(slotId, out CustomShortcutSlot slot) || slot == null)
            {
                slot = new CustomShortcutSlot { Keys = new List<string>() };
                _draft.CustomShortcuts[slotId] = slot;
            }

            SplitShortcut(slot.Keys, out string currentModifier, out string currentKey);
            modifier.SelectedItem = ModifierOptions.Contains(currentModifier) ? currentModifier : "None";
            key.SelectedItem = KeyOptions.Contains(currentKey) ? currentKey : "Not Set";
        }

        private void RenderSectionOrder()
        {
            SectionOrderPanel.Children.Clear();
            for (int i = 0; i < _draft.SectionOrder.Count; i++)
            {
                string section = _draft.SectionOrder[i];
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var name = new TextBlock
                {
                    Text = GetSectionDisplayName(section),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Windows.UI.Colors.White)
                };

                var up = new Button { Content = "▲", MinWidth = 40, IsEnabled = i > 0 };
                var down = new Button { Content = "▼", MinWidth = 40, IsEnabled = i < _draft.SectionOrder.Count - 1 };

                int index = i;
                up.Click += (_, __) =>
                {
                    if (index <= 0)
                    {
                        return;
                    }

                    string temp = _draft.SectionOrder[index - 1];
                    _draft.SectionOrder[index - 1] = _draft.SectionOrder[index];
                    _draft.SectionOrder[index] = temp;
                    RenderSectionOrder();
                };

                down.Click += (_, __) =>
                {
                    if (index >= _draft.SectionOrder.Count - 1)
                    {
                        return;
                    }

                    string temp = _draft.SectionOrder[index + 1];
                    _draft.SectionOrder[index + 1] = _draft.SectionOrder[index];
                    _draft.SectionOrder[index] = temp;
                    RenderSectionOrder();
                };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(up, 1);
                Grid.SetColumn(down, 2);
                row.Children.Add(name);
                row.Children.Add(up);
                row.Children.Add(down);
                SectionOrderPanel.Children.Add(row);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
            ValidationTextBlock.Text = string.Empty;

            if (string.Equals(LosslessKeyCombo.SelectedItem as string, "Not Set", StringComparison.OrdinalIgnoreCase))
            {
                SetValidation("Lossless Scaling shortcut must include a key.");
                return;
            }

            List<string> losslessKeys = ComposeShortcut(LosslessModifierCombo.SelectedItem as string ?? "None", LosslessKeyCombo.SelectedItem as string ?? "Not Set");
            if (losslessKeys.Count == 0)
            {
                SetValidation("Lossless Scaling shortcut must include a key.");
                return;
            }

            _draft.BuiltInLosslessKeys = losslessKeys;
            UpdateCustom("custom1", Custom1ModifierCombo, Custom1KeyCombo);
            UpdateCustom("custom2", Custom2ModifierCombo, Custom2KeyCombo);
            UpdateCustom("custom3", Custom3ModifierCombo, Custom3KeyCombo);

            try
            {
                _draft = WidgetSettingsStore.Normalize(_draft);
                DiagnosticsLog.Write("Saving SectionOrder=" + string.Join(",", _draft.SectionOrder));
                WidgetSettingsStore.Save(_draft);
                await CloseSettingsAndReturnToMainAsync();
            }
            catch (Exception ex)
            {
                SetValidation($"Failed to save settings: {ex.Message}");
            }
        }

        private void UpdateCustom(string slotId, ComboBox modifier, ComboBox key)
        {
            List<string> keys = ComposeShortcut(modifier.SelectedItem as string ?? "None", key.SelectedItem as string ?? "Not Set");

            _draft.CustomShortcuts[slotId] = new CustomShortcutSlot
            {
                Keys = keys
            };
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            await CloseSettingsAndReturnToMainAsync();
        }

        private void LosslessReset_Click(object sender, RoutedEventArgs e)
        {
            LosslessModifierCombo.SelectedItem = "Ctrl + Alt";
            LosslessKeyCombo.SelectedItem = "S";
        }

        private void Custom1Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetSlot(Custom1ModifierCombo, Custom1KeyCombo);
        }

        private void Custom2Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetSlot(Custom2ModifierCombo, Custom2KeyCombo);
        }

        private void Custom3Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetSlot(Custom3ModifierCombo, Custom3KeyCombo);
        }

        private void ResetSlot(ComboBox modifier, ComboBox key)
        {
            modifier.SelectedItem = "None";
            key.SelectedItem = "Not Set";
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            _draft.SectionOrder = new List<string>(WidgetSettingsDefaults.DefaultSectionOrder);
            RenderSectionOrder();
        }

        private static void SetComboItems(ComboBox combo, IReadOnlyList<string> items)
        {
            combo.Items.Clear();
            foreach (string item in items)
            {
                combo.Items.Add(item);
            }
        }

        private void SetValidation(string text)
        {
            ValidationTextBlock.Text = text;
            ValidationTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 128, 128));
            ValidationTextBlock.Visibility = Visibility.Visible;
        }

        private async Task CloseSettingsAndReturnToMainAsync()
        {
            if (_gameBarWidget == null)
            {
                SetValidation("Settings widget context is not available.");
                return;
            }

            try
            {
                var control = new XboxGameBarWidgetControl(_gameBarWidget);
                await control.ActivateAsync("Widget2");
                await control.CloseAsync("Widget2Settings");
            }
            catch (Exception ex)
            {
                SetValidation($"Failed to return to main widget: {ex.Message}");
            }
        }

        private static List<string> ComposeShortcut(string modifier, string key)
        {
            if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "Not Set", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string>();
            }

            var output = new List<string>();
            if (!string.IsNullOrWhiteSpace(modifier) && !string.Equals(modifier, "None", StringComparison.OrdinalIgnoreCase))
            {
                output.AddRange(modifier.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()));
            }

            output.Add(key.Trim());

            return output;
        }

        private static void SplitShortcut(IReadOnlyList<string> keys, out string modifier, out string key)
        {
            modifier = "None";
            key = "Not Set";
            if (!WidgetSettingsStore.IsValidKeys(keys))
            {
                return;
            }

            if (keys.Count == 1)
            {
                key = keys[0];
                return;
            }

            key = keys[keys.Count - 1];
            modifier = string.Join(" + ", keys.Take(keys.Count - 1));
        }

        private static string GetSectionDisplayName(string sectionId)
        {
            return sectionId switch
            {
                "overlay" => "OptiScaler Overlay",
                "resolution" => "Display Resolution",
                "custom" => "Custom",
                _ => sectionId
            };
        }
    }
}
