using FileSystemAnalyzer.ViewModels;
using System.Windows;


namespace FileSystemAnalyzer
{
    //
    public partial class DriveSelectionWindow : Window
    {
        
        public DriveSelectionViewModel ViewModel => (DriveSelectionViewModel)DataContext;
        public string[] SelectedRoots => ViewModel.Drives
            .Where(d => d.IsSelected)
            .Select(d => d.Path)
            .ToArray();
    
        public DriveSelectionWindow()
        {
            InitializeComponent();

            DataContext = new DriveSelectionViewModel();

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DriveSelectionViewModel.DialogResult))
                    this.DialogResult = ViewModel.DialogResult;
            };
        }
    }
}
