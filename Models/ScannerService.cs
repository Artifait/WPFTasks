using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace WPFTasks.Models
{
    public class ScannerService
    {
        private readonly List<string> _bannedWords;
        private readonly List<string> _allowedExtensions;
        private readonly string _replacementWord;
        private readonly Action<ScanResult> _onFileFound;
        private readonly Action<int> _onProgressUpdated;
        private readonly Action<string> _onSetDir;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphore = new(4); // Количество потоков

        private ConcurrentDictionary<string, int> _wordStatistics = new();

        public ScannerService(List<string> bannedWords, List<string> allowedExtensions, string replacementWord, Action<ScanResult> onFileFound, Action<int> onProgressUpdated, Action<string> onSetDir)
        {
            _bannedWords = bannedWords;
            _allowedExtensions = allowedExtensions;
            _replacementWord = replacementWord;
            _onFileFound = onFileFound;
            _onProgressUpdated = onProgressUpdated;
            _onSetDir = onSetDir;
        }

        public async Task ScanAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token; 
            var drives = DriveInfo.GetDrives();
            int fileCount = 0;

            foreach (var drive in drives)
            {
                try
                {
                    if (drive.IsReady)
                    {
                        await ScanDirectoryAsync(drive.RootDirectory, fileCount, token);
                    }
                }
                catch(OperationCanceledException ex)
                {
                    MessageBox.Show("Отмена Сканирования!!!");
                    return;
                }
            }
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void GenerateReport(IEnumerable<ScanResult> results)
        {
            var reportFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScanReport.txt");

            try
            {
                using (var writer = new StreamWriter(reportFilePath, false, Encoding.GetEncoding(1251)))
                {
                    writer.WriteLine("=== Отчет по сканированию файлов ===\n");

                    writer.WriteLine("Файлы с запрещенными словами:\n");
                    foreach (var result in results)
                    {
                        writer.WriteLine($"Путь к файлу: {result.FilePath}");
                        writer.WriteLine($"Размер файла: {result.FileSize} байт");
                        writer.WriteLine($"Количество замен: {result.ReplacementCount}");

                        writer.WriteLine("Найденные запрещенные слова:");
                        foreach (var wordOccurrence in result.WordOccurrences)
                        {
                            writer.WriteLine($"  - {wordOccurrence.Key}: {wordOccurrence.Value} раз");
                        }
                        writer.WriteLine();
                    }

                    writer.WriteLine("=== Топ-10 самых популярных запрещенных слов ===\n");

                    var topBannedWords = _wordStatistics
                        .OrderByDescending(kv => kv.Value)
                        .Take(10);

                    int rank = 1;
                    foreach (var word in topBannedWords)
                    {
                        writer.WriteLine($"{rank}. {word.Key}: {word.Value} раз");
                        rank++;
                    }
                }

                MessageBox.Show($"Отчет успешно создан: {reportFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}");
            }
        }


        public List<BannedWordStatistics> GetTopBannedWords(int topCount)
        {
            return _wordStatistics.OrderByDescending(kv => kv.Value)
                                  .Take(topCount)
                                  .Select(kv => new BannedWordStatistics { Word = kv.Key, Count = kv.Value })
            .ToList();
        }

        private async Task ScanDirectoryAsync(DirectoryInfo directory, int totalFiles, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); 

            try
            {
                if (!HasAccess(directory))
                    return;
                _onSetDir(directory.FullName);
                var files = directory.GetFiles();
                foreach (var file in files)
                {
                    token.ThrowIfCancellationRequested(); 

                    if (!HasAccess(file))
                        continue;

                    await _semaphore.WaitAsync();
                    _ = Task.Run(() => ProcessFile(file), _cancellationTokenSource.Token)
                            .ContinueWith(t => _semaphore.Release());
                    _onProgressUpdated((++totalFiles) * 100 / files.Length);
                }

                foreach (var subDir in directory.GetDirectories())
                {
                    token.ThrowIfCancellationRequested();

                    await ScanDirectoryAsync(subDir, totalFiles, token);
                }
            }
            catch(OperationCanceledException) 
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"Нет доступа к каталогу: {directory.FullName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сканировании каталога {directory.FullName}: {ex.Message}");
            }
        }

        // Проверка доступа
        private bool HasAccess(FileSystemInfo fileSystemInfo)
        {
            try
            {
                if (fileSystemInfo is DirectoryInfo dir)
                {
                    dir.GetDirectories();
                }
                else if (fileSystemInfo is FileInfo file)
                {
                    using var stream = file.OpenRead();
                }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false; 
            }
            catch
            {
                return false; 
            }
        }


        private void ProcessFile(FileInfo file)
        {
            // Проверка расширения файла
            if (!_allowedExtensions.Contains(file.Extension.ToLower())) return;

            var scanResult = new ScanResult
            {
                FilePath = file.FullName,
                FileSize = file.Length,
                ReplacementCount = 0,
                WordOccurrences = []
            };

            bool hasBannedWord = false;
            StringBuilder fileContent = new();

            try
            {
                using (var reader = new StreamReader(file.FullName, Encoding.UTF8))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        foreach (var bannedWord in _bannedWords)
                        {
                            // Подсчет вхождений и замена запрещенных слов
                            int count = 0;
                            string pattern = $@"\b{Regex.Escape(bannedWord)}\b";
                            line = Regex.Replace(line, pattern, match =>
                            {
                                hasBannedWord = true;
                                scanResult.ReplacementCount++;
                                count++;
                                return _replacementWord;
                            });

                            if (count > 0)
                            {
                                scanResult.WordOccurrences[bannedWord] = scanResult.WordOccurrences.ContainsKey(bannedWord)
                                    ? scanResult.WordOccurrences[bannedWord] + count
                                    : count;
                                _wordStatistics.AddOrUpdate(bannedWord, count, (k, v) => v + count);
                            }
                        }
                        fileContent.AppendLine(line);
                    }
                }

                // Если были найдены запрещенные слова, сохраняем файл в "BadFiles"
                if (hasBannedWord)
                {
                    var badFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BadFiles");
                    Directory.CreateDirectory(badFilesDir);

                    string newFilePath = Path.Combine(badFilesDir, file.Name);
                    File.WriteAllText(newFilePath, fileContent.ToString(), Encoding.GetEncoding(1251));

                    _onFileFound(scanResult);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке файла {file.FullName}: {ex.Message}");
            }
        }
    }
}
