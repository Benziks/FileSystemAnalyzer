using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace FileSystemAnalyzer.ViewModels
{
    //Цей клас представляє опцію вибору диска
    public class DriveOption : INotifyPropertyChanged
    {
        public string Path { get; }
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
        public DriveOption(string path) => Path = path;
        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class DriveSelectionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DriveOption> Drives { get; }

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
        public DriveSelectionViewModel()
        {
            Drives = new ObservableCollection<DriveOption>(
                DriveInfo.GetDrives()
                         .Where(d => d.IsReady)
                         .Select(d => new DriveOption(d.RootDirectory.FullName))
                         );
            ConfirmCommand = new RelayCommand(
                _ => DialogResult = true,
                _ => Drives.Any(d => d.IsSelected)
                );
            CancelCommand = new RelayCommand(
                _ => DialogResult = false
                );
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
