using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using OxyPlot;
using FileSystemAnalyzer.Models;
using FileSystemAnalyzer.Services;
using System.IO;
using System.Windows.Data;    
using System.Windows.Media;  
using System.Collections.Specialized;
using static FileSystemAnalyzer.Models.FileItem;


namespace FileSystemAnalyzer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IFileSystemScanner _scanner;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IThemeService _themeService;
        private readonly IExportService _exportService;
        private readonly IChartService _chartService;
        private readonly IDuplicateService _duplicateService;

        private Dictionary<string, OxyColor> _typeColors = new Dictionary<string, OxyColor>();
        private bool _isLanguageOverlayVisible;
        private LanguageOption _selectedLanguageOption;
        private Dictionary<string, SolidColorBrush> _hashBrushes = new Dictionary<string, SolidColorBrush>();
        private bool _cancelled;
        private PlotModel _chartModel;
        private double _progressValue;
        private bool _isAnalyzing;
        private int _currentChartIndex;
        private string _themeButtonText;
        private bool _isDarkTheme = true;

        public bool IsLanguageOverlayVisible
        {
            get => _isLanguageOverlayVisible;
            set { _isLanguageOverlayVisible = value; OnPropertyChanged(nameof(IsLanguageOverlayVisible)); }
        }
        public IReadOnlyList<LanguageOption> AvailableLanguages { get; }
        public LanguageOption SelectedLanguageOption
        {
            get => _selectedLanguageOption;
            set
            {
                if (_selectedLanguageOption != value)
                {
                    _selectedLanguageOption = value;
                    OnPropertyChanged(nameof(SelectedLanguageOption));
                    _localizationService.SetCulture(value.Code);

                    UpdateThemeButtonText();
                }
            }
        }

        public ObservableCollection<FileItem> FileItems { get; } = new ObservableCollection<FileItem>();
        public bool Cancelled => _cancelled;
        public PlotModel ChartModel
        {
            get => _chartModel;
            private set { _chartModel = value; OnPropertyChanged(nameof(ChartModel)); }
        }
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            private set { _isAnalyzing = value; OnPropertyChanged(nameof(IsAnalyzing)); }
        }
        public string ThemeButtonText
        {
            get => _themeButtonText;
            private set
            {
                if (_themeButtonText != value)
                {
                    _themeButtonText = value;
                    OnPropertyChanged(nameof(ThemeButtonText));
                }
            }
        }
        public ICommand FullAnalysisCommand { get; }
        public ICommand QuickAnalysisCommand { get; }
        public ICommand SelectiveAnalysisCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ExportChartCommand { get; }
        public ICommand GroupDuplicatesCommand { get; }
        public ICommand NextChartCommand { get; }
        public ICommand PrevChartCommand { get; }
        public ICommand ChangeThemeCommand { get; }
        public ICommand ToggleLanguageOverlayCommand { get; }
        public ICommand CloseLanguageOverlayCommand { get; }


        public MainWindowViewModel(
            IFileSystemScanner scanner,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IThemeService themeService,
            IExportService exportService,
            IChartService chartService,
            IDuplicateService duplicateService)
        {
            _scanner = scanner;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _themeService = themeService;
            _exportService = exportService;
            _chartService = chartService;
            _duplicateService = duplicateService;
            var savedCulture = Properties.Settings.Default.Culture;
            _localizationService.SetCulture(savedCulture);
            _isDarkTheme = Properties.Settings.Default.IsDarkTheme;

            if (!_isDarkTheme)
                _themeService.ToggleTheme();

            ChangeThemeCommand = new RelayCommand(_ =>
            {
                _themeService.ToggleTheme();
                _isDarkTheme = !_isDarkTheme;
                UpdateThemeButtonText();
            });

            UpdateThemeButtonText();

            AvailableLanguages = _localizationService
     .GetSupportedCultures()
     .Select(code => new LanguageOption(
         code,
         code == "uk-UA" ? "Українська" :
         code == "en-US" ? "English" :
         code)) 
     .ToList();
            SelectedLanguageOption = AvailableLanguages.First(l => l.Code == _localizationService.CurrentCulture);

            FullAnalysisCommand = new RelayCommand(async _ => await ExecuteFullAnalysis(), _ => !IsAnalyzing);
            QuickAnalysisCommand = new RelayCommand(async _ => await ExecuteQuickAnalysis(), _ => !IsAnalyzing);
            SelectiveAnalysisCommand = new RelayCommand(async _ => await ExecuteSelectiveAnalysis(), _ => !IsAnalyzing);
            ExportDataCommand = new RelayCommand(_ => ExecuteExportData(), _ => FileItems.Count > 0);
            ExportChartCommand = new RelayCommand(_ => ExecuteExportChart(), _ => ChartModel != null);
            GroupDuplicatesCommand = new RelayCommand(_ => ExecuteGroupDuplicates(), _ => FileItems.Count > 0);
            ToggleLanguageOverlayCommand = new RelayCommand(_ => IsLanguageOverlayVisible = !IsLanguageOverlayVisible);
            CloseLanguageOverlayCommand = new RelayCommand(_ => IsLanguageOverlayVisible = false);

            PrevChartCommand = new RelayCommand(_ =>
            {
                if (_currentChartIndex > 0)
                {
                    _currentChartIndex--;
                    UpdateChart();   
                }

            },
            _ => _currentChartIndex > 0);

            NextChartCommand = new RelayCommand(_ =>
            {
                if (_currentChartIndex < 1)
                {
                    _currentChartIndex++;
                    UpdateChart();
                }
            },
            _ => _currentChartIndex < 1);

            ChartModel = null;

            FileItems.CollectionChanged += OnFileItemsChanged;
        }

        private void UpdateThemeButtonText()
        {

            var prefix = (string)System.Windows.Application.Current.TryFindResource("ThemeLabel") ?? "Theme:";
            var suffixKey = _isDarkTheme
                     ? "ThemeSuffix_Dark"
                     : "ThemeSuffix_Light";
            var suffix = (string)System.Windows.Application.Current.TryFindResource(suffixKey) ?? (suffixKey == "ThemeSuffix_Dark" ? "Dark" : "Light");

            ThemeButtonText = $"{prefix} {suffix}";
        }

        private void OnFileItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateChart();
            UpdateCommands();
        }

        private void UpdateFileItems(IEnumerable<FileItem> items)
        {
            FileItems.CollectionChanged -= OnFileItemsChanged;
            FileItems.Clear();
            foreach (var f in items) FileItems.Add(f);
            FileItems.CollectionChanged += OnFileItemsChanged;
            UpdateChart();
            _duplicateService.MarkDuplicates(FileItems);
        }

        private async Task ExecuteFullAnalysis()
        {
            _cancelled = false;

            _currentChartIndex = 0;
            _typeColors.Clear();
            _hashBrushes.Clear();
            ChartModel = null;

            var roots = await _dialogService.ShowDriveSelectionAsync();
            if (roots == null || roots.Length == 0)
            {
                _cancelled = true;
                IsAnalyzing = false;
                return;
            }
            IsAnalyzing = true;
            ProgressValue = 0;
            var progress = new Progress<double>(p => ProgressValue = p);

            List<FileItem> items;
            try
            {
                items = await _scanner.FullScanAsync(roots, progress);
            }
            catch (OperationCanceledException)
            {
                IsAnalyzing = false;
                return;
            }

            UpdateFileItems(items);
            IsAnalyzing = false;
        }

        private async Task ExecuteQuickAnalysis()
        {
            _cancelled = false;
            IsAnalyzing = true;
            ProgressValue = 0;

            var progress = new Progress<double>(p => ProgressValue = p);
            var items = await _scanner.QuickScanAsync(progress);

            UpdateFileItems(items);
            IsAnalyzing = false;
        }

        private async Task ExecuteSelectiveAnalysis()
        {
            _cancelled = false;

            var original = await _dialogService.ShowFolderSelectionAsync(new string[0]);
            if (original == null || original.Length == 0)
            {
                _cancelled = true;
                IsAnalyzing = false;
                return;
            }

            var distinct = original
                .Select(f => Path.GetFullPath(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f.Length)
                .ToList();

            var filtered = new List<string>();
            var removed = new List<string>();
            foreach (var f in distinct)
            {
                if (!filtered.Any(p => f.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                    filtered.Add(f);
                else
                    removed.Add(f);
            }

            if (removed.Count > 0)
            {
                var msg = (string)System.Windows.Application.Current.FindResource("FoldersExcluded_Message")
                          + "\n\n" + string.Join("\n", removed);
                _dialogService.ShowMessage((string)System.Windows.Application.Current.FindResource("FoldersExcluded_Title"), msg);
            }

            if (filtered.Count == 0)
            {
                _cancelled = true;
                IsAnalyzing = false;
                return;
            }

            var formats = await _dialogService.ShowFormatSelectionAsync(filtered.ToArray());
            if (formats == null || formats.Length == 0)
            {
                _cancelled = true;
                IsAnalyzing = false;
                return;
            }
            IsAnalyzing = true;
            ProgressValue = 0;
            var progress = new Progress<double>(p => ProgressValue = p);

            List<FileItem> items;
            try
            {
                items = await _scanner.SelectiveScanAsync(filtered, formats, progress);
            }
            catch (OperationCanceledException)
            {
                IsAnalyzing = false;
                return;
            }

            UpdateFileItems(items);
            IsAnalyzing = false;
        }

        private async void ExecuteExportData()
        {
            // Відкриваємо діалог збереження для текстового файлу (.txt)
            var path = await _dialogService.ShowSaveFileDialogAsync(
                title: (string)System.Windows.Application.Current.FindResource("ExportData_Title"),
                filter: (string)System.Windows.Application.Current.FindResource("ExportData_Filter"),
                defaultExt: "txt",
                defaultName: "FileSystemReport.txt");

            // Якщо користувач скасував діалог або не вказав шлях - нічого не робимо
            if (string.IsNullOrEmpty(path)) return;

            // Отримуємо поточне подання колекції FileItems (враховуючи сортування і фільтрацію в DataGrid)
            var view = CollectionViewSource.GetDefaultView(FileItems);
            var sortedItems = view.Cast<FileItem>().ToList();

            // Готуємо заголовки колонок, довантажені з ресурсів для локалізації
            var headers = new[] {
        (string)System.Windows.Application.Current.FindResource("ColumnName"),
        (string)System.Windows.Application.Current.FindResource("ColumnType"),
        (string)System.Windows.Application.Current.FindResource("ColumnSize"),
        (string)System.Windows.Application.Current.FindResource("ColumnCreation"),
        (string)System.Windows.Application.Current.FindResource("ColumnAccess"),
        (string)System.Windows.Application.Current.FindResource("ColumnPath") };

            // Створюємо структуру для зберігання всіх рядків, перший рядок це загаловки колонок
            var rows = new List<string[]> { headers };

            // Додаємо до rows кожен FileItem у вигляді масиву рядків
            foreach (var item in sortedItems)
            {
                rows.Add(new[]
                {
            item.Name,
            item.Extension,
            item.FormattedSize,
            item.CreationTime.ToString("dd.MM.yyyy"),
            item.LastAccessTime.ToString("dd.MM.yyyy"),
            item.FullPath
        });
            }

            // Обчислюємо максимальну ширину кожного стовпця, щоб вирівняти дані
            int cols = headers.Length;
            var maxWidth = new int[cols];
            for (int c = 0; c < cols; c++)
                maxWidth[c] = rows.Max(r => r[c]?.Length ?? 0);

            // Викликаємо сервіс ExportData, передаючи список вирівняних рядків і шлях до файлу
            _exportService.ExportData(rows.Select(r => string.Join("", r.Select((cell, c) => cell.PadRight(maxWidth[c] + 2)))), path);
        }

        private async void ExecuteExportChart()
        {
            if (ChartModel == null)
                return;

            // Відкриваємо діалог "Зберегти як" і отримуємо вибраний шлях
            var path = await _dialogService.ShowSaveFileDialogAsync(
                title: (string)System.Windows.Application.Current.FindResource("ExportChart_Title"),
                filter: "PNG Image|*.png",
                defaultExt: "png",
                defaultName: _currentChartIndex == 0 ? "bar_chart.png" : "line_chart.png");
            if (string.IsNullOrEmpty(path))return;
            _exportService.ExportChart(ChartModel, path, 800, 600);
        }

        private async void ExecuteGroupDuplicates()
        {
            if (IsAnalyzing) return;

            var view = CollectionViewSource.GetDefaultView(FileItems);
            view.SortDescriptions.Clear();

            // Очищаємо попереднє групування і формуємо новий порядок
            var snapshot = FileItems.ToList(); // Копіюємо поточний список файлів
            var reordered = await Task.Run(() =>
                _duplicateService.GroupDuplicates(snapshot).ToList()
            );

            // Відписуємося від події, щоб тимчасово уникнути дублюючого оновлення
            FileItems.CollectionChanged -= OnFileItemsChanged;

            // Очищаємо поточну колекцію та додаємо елементи в новому порядку
            FileItems.Clear();
            foreach (var f in reordered)
                FileItems.Add(f);

            //Після перезавантаження колекції знову підписуємося на подію зміни
            FileItems.CollectionChanged += OnFileItemsChanged;

            // Позначаємо кожен дубльований рядок однаковим кольором
            _duplicateService.MarkDuplicates(FileItems);
        }

        private void UpdateChart()
        {

            if (_currentChartIndex == 0)
                ChartModel = _chartService.CreateBarChart(FileItems);
            else
                ChartModel = _chartService.CreateLineChart(FileItems);

            OnPropertyChanged(nameof(ChartModel));
        }

        private void UpdateCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

    }
}
