using FileSystemAnalyzer.Models;

namespace FileSystemAnalyzer.Services
{
    //Інтерфейс для роботи з дублікатами
    public interface IDuplicateService
    {
        //Метод для маркування дублікатів
        void MarkDuplicates(IEnumerable<FileItem> items);
        //Метод для групування дублікатів
        IEnumerable<FileItem> GroupDuplicates(IEnumerable<FileItem> items);
    }
}
