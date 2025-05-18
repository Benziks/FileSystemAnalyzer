using System.Windows;


namespace FileSystemAnalyzer.Services
{
    
    public class LocalizationService : ILocalizationService
    {
        //Список підтримуваних культур
        private readonly IReadOnlyList<string> _cultures = new[] { "uk-UA", "en-US" };
        public string CurrentCulture { get; private set; } = "uk-UA";
        public IReadOnlyList<string> GetSupportedCultures() => _cultures;
        // Метод для отримання поточної культури
        public void SetCulture(string culture)
        {
            if (culture == CurrentCulture || !_cultures.Contains(culture))
                return;
            CurrentCulture = culture;
            // Формуємо шлях до потрібного словника за патерном Resources/Resources.{culture}.xaml
            var dictUri = new Uri($"Resources/Resources.{culture}.xaml",UriKind.Relative);
            //Завантажуємо новий словник
            var newDict = new ResourceDictionary { Source = dictUri };
            
            var app = System.Windows.Application.Current;
            // Видаляємо старий словник, якщо він існує, а якщо ні, то просто додаємо новий
            var oldDict = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Resources/Resources.", StringComparison.OrdinalIgnoreCase));
            if (oldDict != null)
                app.Resources.MergedDictionaries.Remove(oldDict);

            app.Resources.MergedDictionaries.Add(newDict);
            // Зберігаємо нову культуру в налаштуваннях програми
            Properties.Settings.Default.Culture = culture;
            Properties.Settings.Default.Save();

        }
    }
}
