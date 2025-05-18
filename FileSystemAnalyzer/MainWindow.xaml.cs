using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileSystemAnalyzer.Services;
using System.Collections.ObjectModel;
using FileSystemAnalyzer.Models;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel;  
using FileSystemAnalyzer.ViewModels;

namespace FileSystemAnalyzer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        
        private ObservableCollection<FileItem> _fileItems;

        public MainWindow()
        {
            InitializeComponent();

            ResultsDataGrid.ItemsSource = _fileItems;

            var vm = new MainWindowViewModel(
                new FileSystemScanner(),
                new DialogService(),
                new LocalizationService(),
                new ThemeService(),
                new ExportService(),
                new ChartService(),
                new DuplicateService());
            DataContext = vm;

            _fileItems = vm.FileItems;
            ResultsDataGrid.ItemsSource = _fileItems;

            vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = (MainWindowViewModel)DataContext;

            if (e.PropertyName == nameof(vm.IsAnalyzing))
            {
                if (vm.IsAnalyzing)
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    MainMenuPanel.Visibility = Visibility.Collapsed;
                    AnalysisModePanel.Visibility = Visibility.Collapsed;
                    ResultsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;

                    if (vm.Cancelled)
                    {
                        AnalysisModePanel.Visibility = Visibility.Collapsed;
                        MainMenuPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AnalysisModePanel.Visibility = Visibility.Collapsed;
                        ResultsPanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        private void StartAnalysis_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            AnalysisModePanel.Visibility = Visibility.Visible;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ResultsBack_Click(object sender, RoutedEventArgs e)
        {

            ResultsPanel.Visibility = Visibility.Collapsed;
            AnalysisModePanel.Visibility = Visibility.Visible;
        }

        private void AnalysisBack_Click(object sender, RoutedEventArgs e)
        {
            AnalysisModePanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;
        }

        private void SettingsBack_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;
        }

        private void FaqButton_Click(object sender, RoutedEventArgs e)
        {
            FaqOverlay.Visibility = Visibility.Visible;
        }

        private void CloseFaq_Click(object sender, RoutedEventArgs e)
        {
            FaqOverlay.Visibility = Visibility.Collapsed;
        }

        private void ResultsDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);
            if (!(dep is DataGridRow row)) return;

            row.IsSelected = true;
            if (!(row.Item is FileItem item)) return;

            var cm = new ContextMenu();
            var miOpen = new MenuItem { Header = (string)FindResource("Context_OpenFolder") };
            miOpen.Click += (_, __) => OpenFolder(item);
            var miDel = new MenuItem { Header = (string)FindResource("Context_DeleteFile") };
            miDel.Click += (_, __) => DeleteFile(item);
            cm.Items.Add(miOpen);
            cm.Items.Add(miDel);

            row.ContextMenu = cm;
            cm.IsOpen = true;
            e.Handled = true;
        }

        private void OpenFolder(FileItem item)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{item.FullPath}\"",
                UseShellExecute = true
            });
        }

        private void DeleteFile(FileItem item)
        {
            var text = (string)FindResource("DeleteFile_Confirm") + "\n" + item.FullPath;
            var caption = (string)FindResource("Confirm_Title");
            if (System.Windows.MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            FileSystem.DeleteFile(item.FullPath,
                                  UIOption.OnlyErrorDialogs,
                                  RecycleOption.SendToRecycleBin);

            _fileItems.Remove(item);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
