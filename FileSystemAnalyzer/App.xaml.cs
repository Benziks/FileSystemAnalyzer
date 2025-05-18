using System.Windows;
using FileSystemAnalyzer.Services;
using System.Globalization;
using FileSystemAnalyzer.Properties;

namespace FileSystemAnalyzer
{
    public partial class App : System.Windows.Application
    {
        //OnStartup - метод, який викликається при запуску програми для ініціалізації культури та теми
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var savedCulture = Settings.Default.Culture;
            var locService = new LocalizationService();
            locService.SetCulture(savedCulture);

            var isDark = Settings.Default.IsDarkTheme;
            var themeService = new ThemeService();

            if (!isDark)
                themeService.ToggleTheme();
        }
    }
}


