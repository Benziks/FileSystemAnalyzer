using FileSystemAnalyzer.Models;

namespace FileSystemAnalyzer.Services
{
   public interface IFileSystemScanner
    {
        //Пошук всіх файлів у вказаних папках. Параметри: масив шляхів до папок, прогрес бар.
        Task<List<FileItem>> FullScanAsync(string[] roots, IProgress<double> progress);
        //Швидкий пошук файлів у папках "Завантаження" та "Робочий стіл". Параметри: прогрес бар.
        Task<List<FileItem>> QuickScanAsync(IProgress<double> progress);
        //Пошук файлів у вказаних папках з можливістю фільтрації за форматом. Параметри: масив шляхів до папок, масив форматів, прогрес бар.
        Task<List<FileItem>> SelectiveScanAsync(IEnumerable<string> folders, IEnumerable<string> formats, IProgress<double> progress);
    }
}
