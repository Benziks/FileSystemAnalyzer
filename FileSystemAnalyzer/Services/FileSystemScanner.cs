using System.Collections.Concurrent;
using System.IO;
using System.Security.Policy;
using FileSystemAnalyzer.Models;

namespace FileSystemAnalyzer.Services
{
    public class FileSystemScanner : IFileSystemScanner
    {
        public async Task<List<FileItem>> FullScanAsync(string[] roots, IProgress<double> progress)
        {
            // Виконуємо весь процес у фоновому потоці, передаючи null для форматів.
            return await Task.Run(() => ScanAndComputeUnified(roots, null, useParallelEnumeration: true, progress));
        }

        //Асинхроний швидкий аналіз "Завантаження" та "Робочий стіл". 
        public async Task<List<FileItem>> QuickScanAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                // Отримуємо початковий список файлів без хешів.
                var scanned = QuickScan();
                int total = scanned.Count;
                int done = 0;
                var bag = new ConcurrentBag<FileItem>();

                // Паралельно обчислюємо хеш та збираємо результати.
                Parallel.ForEach(scanned, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) },
                    fi =>
                    {
                        try
                        {
                            fi.Hash = ComputeFileHash(fi.FullPath);
                        }
                        catch
                        {
                            fi.Hash = string.Empty;
                        }
                        bag.Add(fi);
                        //Оновлюємо прогрес за допомогою IProgress<double>.
                        int d = Interlocked.Increment(ref done);
                        progress?.Report(d * 100.0 / total);
                    });
                // Повертаємо оброблений список.
                return bag.ToList();
            });
        }

        //Метод який виконує швидкий пошук файлів у папках "Завантаження" та "Робочий стіл".
        private List<FileItem> QuickScan()
        {
            // Отримуємо шляхи до Робочого стола та Завантаження
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var folders = new[] { desktop, downloads };
            var result = new List<FileItem>();

            // Рекурсивний обхід кожної папки
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(filePath);

                        //Формуємо об'єкт FileItem із базовими метаданими
                        result.Add(new FileItem
                        {
                            Name = info.Name,
                            Extension = info.Extension,
                            Size = info.Length,
                            CreationTime = info.CreationTime,
                            LastAccessTime = info.LastAccessTime,
                            FullPath = info.FullName
                        });
                    }
                    catch
                    {
                        //Ігноруємо файли, до яких немає доступу
                    }
                }
            }
            // Повертаємо список знайдених файлів без хешів
            return result;
        }

        //Пошук файлів у вказаних папках з можливістю фільтрації за форматом.
        public async Task<List<FileItem>> SelectiveScanAsync(IEnumerable<string> folders, IEnumerable<string> formats, IProgress<double> progress)
        {
            return await Task.Run(() => ScanAndComputeUnified(folders.ToList(), formats.ToList(), useParallelEnumeration: false, progress));
        }

        //Метод який виконує аналіз, об'єднує вибірковий пошук та повний пошук. 
        private List<FileItem> ScanAndComputeUnified(IList<string> folders, IList<string>? formats, bool useParallelEnumeration, IProgress<double> progress)
        {
            // Налаштування опцій для безпечного рекурсивного обходу.
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };
            //Збір всіх шляхів у ConcurrentBag для безпечного паралельного доступу.
            var allPathsBag = new ConcurrentBag<string>();
            if (useParallelEnumeration)
            {
                // Використовуємо паралельний перебір для збору всіх файлів у вказаних папках.
                Parallel.ForEach(folders, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, root =>
                {
                    if (!Directory.Exists(root)) return;
                    try
                    {
                        foreach (var path in Directory.EnumerateFiles(root, "*", options))
                            allPathsBag.Add(path);
                    }
                    catch (UnauthorizedAccessException) { }
                });
            }
            else
            {
                // Використовуємо звичайний послідовний перебір для збору всіх файлів у вказаних папках.
                foreach (var root in folders)
                {
                    if (!Directory.Exists(root)) continue;
                    try
                    {
                        foreach (var path in Directory.EnumerateFiles(root, "*", options))
                            allPathsBag.Add(path);
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            // Перетворюємо ConcurrentBag у масив для подальшої обробки.
            var allPaths = allPathsBag.ToArray();
            int total = allPaths.Length;
            int processed = 0;
            var result = new ConcurrentBag<FileItem>();
            // Паралельна обробка шляхів: фільтрація, збір метаданих, хеш
            Parallel.ForEach(allPaths, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) },
                path =>
                {
                    try
                    {
                        // Фільтрація за розширення (якщо вказані формати)
                        if (formats != null)
                        {
                            var ext = Path.GetExtension(path).ToLowerInvariant();
                            if (!formats.Contains(ext))
                            {
                                Interlocked.Increment(ref processed);
                                progress?.Report((double)processed / total * 100);
                                return;
                            }
                        }
                        // Збір метаданих файлу
                        var info = new FileInfo(path);
                        var fi = new FileItem()
                        {
                            Name = info.Name,
                            Extension = info.Extension,
                            Size = info.Length,
                            CreationTime = info.CreationTime,
                            LastAccessTime = info.LastAccessTime,
                            FullPath = info.FullName,
                            Hash = ComputeFileHash(info.FullName)
                        };
                        result.Add(fi);
                    }
                    catch
                    {
                        // Ігноруємо файли, до яких немає доступу або які не існують
                    }
                    // Оновлюємо прогрес за допомогою IProgress<double> після кожного обробленого файлу.
                    int done = Interlocked.Increment(ref processed);
                    progress?.Report((double)done / total * 100);
                });
            // Повертаємо результати як список.
            return result.ToList();
        }

        //Метод для обчислення хешу файлу. Використовує SHA256 для обчислення хешу файлу.
        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                // Обчислюємо хеш масиву байтів.
                var hash = sha256.ComputeHash(stream);
                // Перетворюємо хеш у шістнадцятковий рядок.
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            }
        }
    }
}
