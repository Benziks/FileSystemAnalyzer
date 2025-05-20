using System.Windows;
using FileSystemAnalyzer.ViewModels;

namespace FileSystemAnalyzer
{
    public partial class FolderSelectionWindow : Window
    {
        public FolderSelectionViewModel ViewModel => (FolderSelectionViewModel)DataContext;
        public string[] SelectedFolders => ViewModel.Folders.Select(f => f.Path).ToArray();

        public FolderSelectionWindow()
        {
            InitializeComponent();
            DataContext = new FolderSelectionViewModel();
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FolderSelectionViewModel.DialogResult))
                    this.DialogResult = ViewModel.DialogResult;
            };
        }
    }
}
