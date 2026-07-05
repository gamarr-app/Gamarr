using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GamarrLibrary
{
    /// <summary>
    /// Minimal code-only WPF settings view (no XAML so the project compiles
    /// with plain reference assemblies on any OS). Playnite assigns the
    /// GamarrLibrarySettingsViewModel as DataContext.
    /// </summary>
    public class GamarrLibrarySettingsView : UserControl
    {
        public GamarrLibrarySettingsView()
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(12)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Gamarr URL (e.g. http://localhost:6767)",
                Margin = new Thickness(0, 0, 0, 4)
            });
            panel.Children.Add(CreateBoundTextBox("Settings.BaseUrl"));

            panel.Children.Add(new TextBlock
            {
                Text = "API key (Gamarr: Settings -> General -> Security)",
                Margin = new Thickness(0, 12, 0, 4)
            });
            panel.Children.Add(CreateBoundTextBox("Settings.ApiKey"));

            var importCheckBox = new CheckBox
            {
                Content = "Also import monitored games that are not downloaded yet",
                Margin = new Thickness(0, 16, 0, 0)
            };
            importCheckBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding("Settings.ImportNotDownloaded")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
            panel.Children.Add(importCheckBox);

            Content = panel;
        }

        private static TextBox CreateBoundTextBox(string path)
        {
            var textBox = new TextBox
            {
                MinWidth = 360,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            textBox.SetBinding(TextBox.TextProperty, new Binding(path)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            return textBox;
        }
    }
}
