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
            return await Task.Run(() => ScanAndComputeUnified(roots, null, useParallelEnumeration: true, progress));
        }
        
        //Швидкий пошук файлів у папках "Завантаження" та "Робочий стіл". 
        public async Task<List<FileItem>> QuickScanAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                var scanned = QuickScan();
                int total = scanned.Count;
                int done = 0;
                var bag = new ConcurrentBag<FileItem>();
                Parallel.ForEach(scanned, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) },
                    fi =>
                    {
                        try { fi.Hash = ComputeFileHash(fi.FullPath); }
                        catch { fi.Hash = string.Empty; }
                        bag.Add(fi);
                        int d = Interlocked.Increment(ref done);
                        progress?.Report(d * 100.0 / total);
                    });
                return bag.ToList();
            });
        }

        //Пошук файлів у вказаних папках з можливістю фільтрації за форматом.
        public async Task<List<FileItem>> SelectiveScanAsync(IEnumerable<string> folders, IEnumerable<string> formats, IProgress<double> progress)
        {
            return await Task.Run(() => ScanAndComputeUnified(folders.ToList(), formats.ToList(), useParallelEnumeration: false, progress));
        }

        //Метод який виконує швидкий пошук файлів у папках "Завантаження" та "Робочий стіл".
        private List<FileItem> QuickScan()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var folders = new[] { desktop, downloads };
            var result = new List<FileItem>();
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(filePath);
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
                    catch { }
                }
            }
            return result;
        }
        //Метод який виконує аналіз, об'єднує вибірковий пошук та повний пошук. 
        private List<FileItem> ScanAndComputeUnified(IList<string> folders, IList<string>? formats, bool useParallelEnumeration, IProgress<double> progress)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };
            var allPathsBag = new ConcurrentBag<string>();
            if (useParallelEnumeration)
            {
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
            var allPaths = allPathsBag.ToArray();
            int total = allPaths.Length;
            int processed = 0;
            var result = new ConcurrentBag<FileItem>();
            Parallel.ForEach(allPaths, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) },
                path =>
                {
                    try
                    {
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
                    catch { }
                    int done = Interlocked.Increment(ref processed);
                    progress?.Report((double)done / total * 100);
                });
            return result.ToList();
        }
        //Метод для обчислення хешу файлу. Використовує SHA256 для обчислення хешу файлу.
        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            }
        }
    }
}
