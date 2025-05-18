using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FileSystemAnalyzer.ViewModels
{
    //Формат вибору папки
    public class FormatOption : INotifyPropertyChanged
    {
        public string Extension { get; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        public FormatOption(string ext) => Extension = ext;
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class FormatSelectionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FormatOption> FormatOptions { get; }

        public ICommand SelectAllCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            private set
            {
                if (_dialogResult != value)
                {
                    _dialogResult = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogResult)));
                }
            }
        }

        public FormatSelectionViewModel(string[] folders)
        {
            // Собираем список расширений из папок
            FormatOptions = new ObservableCollection<FormatOption>();
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(folder, "*", options))
                    {
                        var ext = Path.GetExtension(file);
                        if (!string.IsNullOrEmpty(ext))
                            exts.Add(ext.ToLowerInvariant());
                    }
                }
                catch { }
            }
            foreach (var ext in exts.OrderBy(e => e))
                FormatOptions.Add(new FormatOption(ext));

            SelectAllCommand = new RelayCommand(_ => FormatOptions.ToList().ForEach(o => o.IsSelected = true), _ => FormatOptions.Any());
            ClearSelectionCommand = new RelayCommand(_ => FormatOptions.ToList().ForEach(o => o.IsSelected = false), _ => FormatOptions.Any(o => o.IsSelected));
            ConfirmCommand = new RelayCommand(_ => Confirm(), _ => FormatOptions.Any(o => o.IsSelected));
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Confirm()
        {
            if (!FormatOptions.Any(o => o.IsSelected))
            {
                System.Windows.MessageBox.Show((string)System.Windows.Application.Current.FindResource("Fmt_NoChoice"), (string)System.Windows.Application.Current.FindResource("Warning_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void Cancel() => DialogResult = false;

        public string[] SelectedFormats => FormatOptions.Where(o => o.IsSelected).Select(o => o.Extension).ToArray();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
