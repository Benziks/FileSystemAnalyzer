using FileSystemAnalyzer.Models;
using OxyPlot;//Бібліотека для роботи з графіками
using OxyPlot.Axes;//Бібліотека для роботи з осями графіків
using OxyPlot.Series; //Бліотека для роботи з серіями даних
using OxyPlot.Legends;//Бібліотека для роботи з легендами
using System.Windows.Input;
using OxyPlot.Wpf;

namespace FileSystemAnalyzer.Services
{
    public class ChartService : IChartService
    {
        private readonly Random _rnd = new Random();
        // Словник для зберігання кольорів типів файлів
        private Dictionary<string, OxyColor> _typeColors = new Dictionary<string, OxyColor>();
        const int MaxTopFileTypes = 49;
        const int MonthGroupingThresholdDays = 90;
        //Создає стовпчикову діаграму з даними про файли
        public PlotModel CreateBarChart(IEnumerable<FileItem> items)
        {
            // Групуємо за розширенням, замінюючи порожні на "[Відсутні]"
            var data = items.GroupBy(f => string.IsNullOrEmpty(f.Extension) ? (string)System.Windows.Application.Current.FindResource("Label_Missing") : f.Extension).ToDictionary(g => g.Key, g => g.Sum(f => f.Size) / 1024.0 / 1024.0);

            //Вибираємо топ‑49 типів, інші в одну групу "[Інші]"
            var top = TopAndOthers(data, MaxTopFileTypes, (string)System.Windows.Application.Current.FindResource("Label_Others"));
            //Перетворюэмо в список ыз сортуванням за обсягом
            var groups = top.Select(kv => new { Ext = kv.Key, Size = Math.Round(kv.Value, 2) }).OrderByDescending(x => x.Size).ToList();
            //Налаштовуємо модель графіка
            var model = new PlotModel { Title = (string)System.Windows.Application.Current.FindResource("Chart_Bar_Title") };
            //Додаємо категорійну вісь зліва для типів файлів
            model.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                ItemsSource = groups.Select(x => x.Ext).ToArray(),
                GapWidth = 0.5
            });
            //Додаємо числову віс знизу для розміру файлів
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = (string)System.Windows.Application.Current.FindResource("Axis_Size"),
                Minimum = 0,
                IsPanEnabled = false,
                IsZoomEnabled = false
            });
            //Створюємо серію стовпців
            var series = new BarSeries { LabelPlacement = LabelPlacement.Inside };
            
            foreach (var g in groups)
            {
                // Якщо колір для типу файлу ще не визначено, генеруємо новий випадковий колір для типу файлу
                if (!_typeColors.ContainsKey(g.Ext))
                    _typeColors[g.Ext] = OxyColor.FromRgb(
                        (byte)_rnd.Next(50, 200),
                        (byte)_rnd.Next(50, 200),
                        (byte)_rnd.Next(50, 200));
                series.Items.Add(new BarItem { Value = g.Size, Color = _typeColors[g.Ext] });
            }

            model.Series.Add(series);

            return model;
        }

        //Створює лінійний графік з даними про файли
        public PlotModel CreateLineChart(IEnumerable<FileItem> items)
        {
            var list = items.ToList();
            var model = new PlotModel { Title = (string)System.Windows.Application.Current.FindResource("Chart_Line_Title") };
            if (!list.Any()) return model;
            // Групуємо файли за розширенням і підраховуємо загальну кількість
            var raw = list.GroupBy(f => string.IsNullOrEmpty(f.Extension) ? (string)System.Windows.Application.Current.FindResource("Label_Missing") : f.Extension).ToDictionary(g => g.Key, g => (double)g.Count());
            // Топ-49 та "[Інші]"
            var top = TopAndOthers(raw, MaxTopFileTypes, (string)System.Windows.Application.Current.FindResource("Label_Others"));

            var exts = top.Keys.ToList();

            // Визначаємо період сканування 
            var min = list.Min(f => f.CreationTime);
            var max = list.Max(f => f.CreationTime);
            
            bool byMonth = (max - min).TotalDays > MonthGroupingThresholdDays;
            // Функція групування дат (щодня або щомісяця)
            Func<FileItem, DateTime> grp = byMonth ? (f => new DateTime(f.CreationTime.Year, f.CreationTime.Month, 1)) : (f => f.CreationTime.Date);

            var dates = list.Select(grp).Distinct().OrderBy(d => d).ToList();
            // Створення серії для кожного типу файлів
            foreach (var ext in exts)
            {
                if (!_typeColors.ContainsKey(ext))
                    _typeColors[ext] = OxyColor.FromRgb(
                        (byte)_rnd.Next(50, 200),
                        (byte)_rnd.Next(50, 200),
                        (byte)_rnd.Next(50, 200));
              
                var s = new LineSeries
                {
                    Title = ext,
                    Color = _typeColors[ext],
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3,
                    TrackerFormatString = (string)System.Windows.Application.Current.FindResource("Tracker_Format"),
                    CanTrackerInterpolatePoints = false
                };
                //Підраховуємо кількість файлів на кожну дату
                var counts = list.Where(f => (string.IsNullOrEmpty(f.Extension) ? (string)System.Windows.Application.Current.FindResource("Label_Missing") : f.Extension) == ext).GroupBy(grp).ToDictionary(g => g.Key, g => g.Count());
                // Додаємо точки на графік
                foreach (var d in dates)
                    s.Points.Add(new DataPoint(DateTimeAxis.ToDouble(d), counts.TryGetValue(d, out var c) ? c : 0));
                model.Series.Add(s);
            }
            // Додає осі до графіка. Вісь X - дати, вісь Y - кількість файлів
            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = byMonth ? "MM.yyyy" : "dd.MM.yyyy",
                MinorIntervalType = byMonth ? DateTimeIntervalType.Months : DateTimeIntervalType.Days,
                IntervalType = byMonth ? DateTimeIntervalType.Months : DateTimeIntervalType.Days,
                IsPanEnabled = false,
                IsZoomEnabled = false
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Title = (string)System.Windows.Application.Current.FindResource("Axis_Count"),
                IsPanEnabled = false,
                IsZoomEnabled = false
            });
            // Додає легенду до графіка
            model.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.TopLeft,      
                LegendOrientation = LegendOrientation.Vertical,   
                LegendMaxHeight = 10 * 20,                     
                LegendColumnSpacing = 70,                          
                LegendBorderThickness = 0,
                LegendBackground = OxyColors.Undefined,
                LegendFontSize = 13
            });
            return model;
        }

        // Залишає у словнику topN елементів із найбільшими значеннями
        private static Dictionary<TKey, double> TopAndOthers<TKey>(IDictionary<TKey, double> data, int topN, TKey othersLabel)
        {
            if (data.Count <= topN)
                return new Dictionary<TKey, double>(data);

            var sorted = data.OrderByDescending(kv => kv.Value).ToList();
            var result = sorted.Take(topN).ToDictionary(kv => kv.Key, kv => kv.Value);
            double sumOthers = sorted.Skip(topN).Sum(kv => kv.Value);

            if (sumOthers > 0)
                result[othersLabel] = sumOthers;
      
            return result;
        }
    }
}





