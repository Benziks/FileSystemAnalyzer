using OxyPlot;
using FileSystemAnalyzer.Models;

namespace FileSystemAnalyzer.Services
{
   public interface IExportService
    {
        //Експорт даних у текстовий файл, графік, експорт у CSV
        void ExportData(IEnumerable<string> lines, string path);
        void ExportChart(PlotModel model, string path, int width, int height);
    }
}
