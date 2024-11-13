
using System.Collections.Concurrent;
using System.IO;


namespace WPFTasks.Models
{
    public class ScannerService
    {
        private readonly List<string> _bannedWords;
        private readonly List<string> _allowedExtensions;
        private readonly Action<ScanResult> _onFileFound;
        private readonly Action<int> _onProgressUpdated;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(4); // Количество потоков

        private ConcurrentDictionary<string, int> _wordStatistics = new ConcurrentDictionary<string, int>();

        public ScannerService(List<string> bannedWords, List<string> allowedExtensions, Action<ScanResult> onFileFound, Action<int> onProgressUpdated)
        {
            _bannedWords = bannedWords;
            _allowedExtensions = allowedExtensions;
            _onFileFound = onFileFound;
            _onProgressUpdated = onProgressUpdated;
        }

        public async Task ScanAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var drives = DriveInfo.GetDrives();
            int fileCount = 0;

            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    await ScanDirectoryAsync(drive.RootDirectory, fileCount);
                }
            }
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void GenerateReport(IEnumerable<ScanResult> results)
        {
            // Создание файла отчета с результатами
        }

        public List<BannedWordStatistics> GetTopBannedWords(int topCount)
        {
            return _wordStatistics.OrderByDescending(kv => kv.Value)
                                  .Take(topCount)
                                  .Select(kv => new BannedWordStatistics { Word = kv.Key, Count = kv.Value })
                                  .ToList();
        }

        private async Task ScanDirectoryAsync(DirectoryInfo directory, int totalFiles)
        {
            var files = directory.GetFiles();
            foreach (var file in files)
            {
                await _semaphore.WaitAsync();
                _ = Task.Run(() => ProcessFile(file), _cancellationTokenSource.Token)
                        .ContinueWith(t => _semaphore.Release());
                _onProgressUpdated((++totalFiles) * 100 / files.Length); // Прогресс обновляется от общего количества файлов
            }

            foreach (var subDir in directory.GetDirectories())
            {
                await ScanDirectoryAsync(subDir, totalFiles);
            }
        }

        private void ProcessFile(FileInfo file)
        {
            // Обработка файла, поиск запрещенных слов и замена
        }
    }
}
