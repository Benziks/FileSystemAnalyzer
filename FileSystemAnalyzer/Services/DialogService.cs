using System.Windows;

namespace FileSystemAnalyzer.Services
{
   public class DialogService : IDialogService
    {
        public async Task<string[]> ShowDriveSelectionAsync()
        {
            var dlg = new DriveSelectionWindow();
            dlg.Owner = Application.Current.MainWindow;
            bool? res = dlg.ShowDialog();
            return res == true ? dlg.SelectedRoots : null;
        }
        public async Task<string[]> ShowFolderSelectionAsync(string[] initialFolders)
        {
            var dlg = new FolderSelectionWindow();
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            bool? res = dlg.ShowDialog();
            return res == true ? dlg.SelectedFolders.ToArray() : null;
        }
        public async Task<string[]> ShowFormatSelectionAsync(string[] folders)
        {
            var dlg = new FormatSelectionWindow(folders);
            dlg.Owner = Application.Current.MainWindow;
            bool? res = dlg.ShowDialog();
            return res == true ? dlg.SelectedFormats : null;
        }

        public Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultExt, string defaultName)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultName
            };
            bool? res = dlg.ShowDialog();
            return Task.FromResult(res == true ? dlg.FileName : null);
        }

        public void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
