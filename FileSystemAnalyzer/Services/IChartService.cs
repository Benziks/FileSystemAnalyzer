using FileSystemAnalyzer.Models;
using OxyPlot;

namespace FileSystemAnalyzer.Services
{
    //Цей інтерфейс визначає методи для створення графіків на основі даних про файли.
    public interface IChartService {
        PlotModel CreateBarChart(IEnumerable<FileItem> items);
        PlotModel CreateLineChart(IEnumerable<FileItem> items);
    
    }

}
