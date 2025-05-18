using FileSystemAnalyzer.Models;
using System.Windows.Media;

namespace FileSystemAnalyzer.Services
{
    public class DuplicateService : IDuplicateService
    {
        private readonly Dictionary<string, SolidColorBrush> _brushCache = new();
        private readonly Random _rnd = new();

        public void MarkDuplicates(IEnumerable<FileItem> items)
        {
            foreach (var f in items)
                f.RowColor = System.Windows.Media.Brushes.White;

            var valid = items.Where(f => !string.IsNullOrEmpty(f.Hash) && f.Size > 0);
            var sizeGroups = valid.GroupBy(f => f.Size).Where(g => g.Count() > 1);
            
            foreach (var sg in sizeGroups)
            {
                var hashGroups = sg.GroupBy(f => f.Hash).Where(g => g.Count() > 1);
                foreach (var hg in hashGroups)
                {
                    //Генерація кольору для дублікатів
                    if (!_brushCache.TryGetValue(hg.Key, out var brush))
                    {
                        brush = new SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(180,
                            (byte)_rnd.Next(100, 256),
                            (byte)_rnd.Next(100, 256),
                            (byte)_rnd.Next(100, 256)));
                        brush.Freeze();
                        _brushCache[hg.Key] = brush;
                    }
                    foreach (var f in hg)
                        f.RowColor = brush;
                }
            }
        }
        //Групування дублікатів 
        public IEnumerable<FileItem> GroupDuplicates(IEnumerable<FileItem> items)
        {
            //Фільтрація дублікатів 
            var dup = items
                .Where(f => !string.IsNullOrEmpty(f.Hash) && f.Size > 0)
                .GroupBy(f => f.Size)
                .SelectMany(g => g
                           .GroupBy(f => f.Hash)
                           .Where(hg => hg.Count() > 1)
                           .SelectMany(hg => hg.OrderBy(f => f.Name)));
            var dupList = dup.ToList();
            var others = items.Except(dupList);
            return dupList.Concat(others);

        }
    }
}
