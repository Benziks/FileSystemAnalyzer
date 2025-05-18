using System.Windows;

namespace FileSystemAnalyzer.Services
{
    //Реалізація сервісу для зміни теми програми
    public class ThemeService : IThemeService
    {
        private bool _isDark = true;
        public void ToggleTheme()
        {
            var app = System.Windows.Application.Current;
            // Забираємо стару тему
            var old = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/Theme.",StringComparison.OrdinalIgnoreCase));
            if (old != null)
                app.Resources.MergedDictionaries.Remove(old);
            // Вирішуємо, який файл підключати. Якщо до перемикання була темна, то ставимо світлу, інакше ставимо темну.
            string themeFile = _isDark ? "Themes/Theme.Light.xaml" : "Themes/Theme.Dark.xaml";
            // Створюємо новий словник ресурсів та додаємо його до колекції об'єднаних словників ресурсів програми.
            var dict = new ResourceDictionary
            {
                Source = new Uri(themeFile, UriKind.Relative)
            };
            app.Resources.MergedDictionaries.Add(dict);
            // Змінюємо флаг і зберігаємо стан теми в налаштуваннях програми.
            _isDark = !_isDark;
            Properties.Settings.Default.IsDarkTheme = _isDark;
            Properties.Settings.Default.Save();

        }
    }
}
