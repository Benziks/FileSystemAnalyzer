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

        public async Task ExportToTextAsync(IEnumerable<FileItem> items,string path)
        {
            //Перевірка на null або пустий список
            if (items == null || !items.Any()) return;
            var headers = new[] { "Name", "Extension", "Size", "Cration Date", "FullPath" };
            //Визначення ширини стовпців
            int nameW = Math.Max(headers[0].Length, items.Max(i => i.Name?.Length ?? 0));
            int extW = Math.Max(headers[1].Length, items.Max(i => i.Extension?.Length ?? 0));
            int sizeW = Math.Max(headers[2].Length, items.Max(i => i.Size.ToString().Length));
            int dateW = headers[3].Length;
            int pathW = Math.Max(headers[4].Length, items.Max(i => i.FullPath.Length));
            
            var lines = new List<string>();
            //Додавання заголовків
            lines.Add(
                $"{headers[0].PadRight(nameW)}" +
                $"{headers[1].PadRight(extW)}" +
                $"{headers[2].PadRight(sizeW)}" +
                $"{headers[3].PadRight(dateW)}" +
                $"{headers[4].PadRight(pathW)}" 
                );
            //Додавання даних
            foreach (var f in items){
                lines.Add(
                $"{f.Name.PadRight(nameW)}  " +
                $"{f.Extension.PadRight(extW)}  " +
                $"{f.Size.ToString().PadRight(sizeW)}  " +
                $"{f.CreationTime:dd.MM.yyyy}.PadRight(dateW)  " +
                $"{f.FullPath.PadRight(pathW)}  " 
                );
            }
            await File.WriteAllLinesAsync(path, lines, Encoding.UTF8);
        }

        public async Task ExportToCsvAsync(IEnumerable<FileItem> items, string path)
        {
            if (items == null || !items.Any())return;

            var lines = new List<string> {"Name,Extension,Size,CreationTime,FullPath"};
            foreach (var f in items)
            {
                string Escape(string s) => (s.Contains(",") || s.Contains("\"")) ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
                //Експорт даних у CSV
                lines.Add(string.Join(",",
                    Escape(f.Name),
                    Escape(f.Extension),
                    f.Size.ToString(),
                    f.CreationTime.ToString("yyyy-MM-dd"),
                    Escape(f.FullPath)
                    ));
            }
            await File.WriteAllLinesAsync(path, lines, Encoding.UTF8);
        }
    }
}
