using System;


namespace FileSystemAnalyzer.Services
{
    //Реалізація інтерфейсу ILocalizationService
    public interface ILocalizationService
    {
        string CurrentCulture { get; }
        void SetCulture(string culture);
        IReadOnlyList<string> GetSupportedCultures() => new[] { "uk-UA", "en-US" };
    }
}
