using System.Windows;

namespace FileSystemAnalyzer.Services
{
    //Інтерфейс для сервісу діалогових вікон
    public interface IDialogService
    {
        // Показує діалог вибору дисків, повертає масив вибраного коріння або null
        Task<string[]> ShowDriveSelectionAsync();
        // Показує вікно вибору папок, повертає список або null
        Task<string[]> ShowFolderSelectionAsync(string[] initialFolders);
        // Показує вікно вибору форматів, повертає список форматних рядків або null
        Task<string[]> ShowFormatSelectionAsync(string[] folders);
        // Зберігає файл через SaveFileDialog, повертає шлях або null
        Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultExt, string defaultName);
        // Показує повідомлення з заголовком і текстом
        void ShowMessage(string title, string message);
    }
}
