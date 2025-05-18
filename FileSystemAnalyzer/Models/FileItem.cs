

namespace FileSystemAnalyzer.Models
{
    //Запис про файл у програмі. (Ім'я, тип, розмір, дата створення, дата доступу, шлях, хеш). Колір дублікатів, округлення в стовпці розмір, а також мова.
    public class FileItem
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public string FullPath { get; set; }
        public string Hash { get; set; }
        public System.Windows.Media.Brush RowColor { get; set; } = System.Windows.Media.Brushes.White;
        public string FormattedSize {
            get => FormatSize(Size);
        }
        private static string FormatSize(long sizeInBytes)
        {
            var uByte = (string)System.Windows.Application.Current.FindResource("Unit_Byte");
            var uKB = (string)System.Windows.Application.Current.FindResource("Unit_KB");
            var uMB = (string)System.Windows.Application.Current.FindResource("Unit_MB");
            var uGB = (string)System.Windows.Application.Current.FindResource("Unit_GB");
            var uTB = (string)System.Windows.Application.Current.FindResource("Unit_TB");

            if (sizeInBytes < 1024) return$"{sizeInBytes} {uByte}";

            double size = sizeInBytes / 1024.0;
            if (size < 1024) return $"{Math.Round(size, 2)} {uKB}";
            size /= 1024.0;
            if (size < 1024) return $"{Math.Round(size, 2)} {uMB}";
            size /= 1024.0;
            if (size < 1024) return $"{Math.Round(size, 2)} {uGB}";
            size /= 1024.0;
            return $"{Math.Round(size, 2)} {uTB}";
        }
        public class LanguageOption
        {
            public string Code { get; }
            public string DisplayName { get; }
            public LanguageOption(string code, string displayName)
            {
                Code = code;
                DisplayName = displayName;
            }
        }

    }
}
