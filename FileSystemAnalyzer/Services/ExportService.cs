using OxyPlot.Wpf;
using OxyPlot;
using System.IO;
using System.Text;
using FileSystemAnalyzer.Models;

namespace FileSystemAnalyzer.Services
{
   public class ExportService : IExportService
    {
        public void ExportData(IEnumerable<string> lines, string path)
        {
            File.WriteAllText(path, string.Join(Environment.NewLine, lines), Encoding.UTF8);
        }
        public void ExportChart(PlotModel model, string path, int width, int height)
        {
            model.Background = OxyColors.White;
            PngExporter.Export(model, path, width, height);
        }

    }
}
