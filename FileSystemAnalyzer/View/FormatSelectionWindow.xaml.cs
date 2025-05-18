using System.Windows;
using System.Windows.Forms;
using FileSystemAnalyzer.ViewModels;


namespace FileSystemAnalyzer
{
    public partial class FormatSelectionWindow : Window
    {
        public FormatSelectionViewModel ViewModel => (FormatSelectionViewModel)DataContext;
        public string[] SelectedFormats => ViewModel.SelectedFormats;
        public FormatSelectionWindow(string[] folders)
        {
            InitializeComponent();
            DataContext = new FormatSelectionViewModel(folders);
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FormatSelectionViewModel.DialogResult)) this.DialogResult = ViewModel.DialogResult;
            };
        }
    }
}

