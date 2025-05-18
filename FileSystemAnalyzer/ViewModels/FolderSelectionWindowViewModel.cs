using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace FileSystemAnalyzer.ViewModels
{
    //Цей клас представляє опцію вибору папки
    public class FolderOption : INotifyPropertyChanged
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

        public FolderOption(string path) => Path = path;
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class FolderSelectionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FolderOption> Folders { get; }
        public FolderOption SelectedFolder { get; set; }

        public ICommand AddFolderCommand { get; }
        public ICommand RemoveFolderCommand { get; }
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

        public FolderSelectionViewModel()
        {
            Folders = new ObservableCollection<FolderOption>();

            AddFolderCommand = new RelayCommand(_ => AddFolder());
            RemoveFolderCommand = new RelayCommand(_ => RemoveFolder(), _ => SelectedFolder != null);
            ConfirmCommand = new RelayCommand(_ => Confirm(), _ => Folders.Any());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void AddFolder()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Title = (string)System.Windows.Application.Current.FindResource("FolderWindow_Title")
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var path = dlg.FileName;
                if (!Folders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    Folders.Add(new FolderOption(path));
            }
        }

        private void RemoveFolder()
        {
            if (SelectedFolder != null)
                Folders.Remove(SelectedFolder);
        }

        private void Confirm()
        {
            if (!Folders.Any())
            {
                DialogResult = false;
                return;
            }

            // Проверяем наличие файлов во выбранных папках
            var options = new System.IO.EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };
            bool anyFiles = Folders.Select(f => f.Path)
                .Where(p => Directory.Exists(p))
                .Any(p => Directory.EnumerateFiles(p, "*", options).Any());

            if (!anyFiles)
            {
                // Локализованное сообщение об отсутствии файлов

                System.Windows.MessageBox.Show((string)System.Windows.Application.Current.FindResource("Fld_NoFiles"), (string)System.Windows.Application.Current.FindResource("Warning_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel() => DialogResult = false;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

