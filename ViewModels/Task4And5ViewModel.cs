using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class Task4And5ViewModel : BaseViewModel
    {
        private string _sourceDirectory;
        private string _destinationDirectory;
        private string _notificationMessage;
        private CancellationTokenSource _cancellationTokenSource;
        private List<OperationReport> _operationReports;

        public string SourceDirectory
        {
            get => _sourceDirectory;
            set => SetProperty(ref _sourceDirectory, value);
        }

        public string DestinationDirectory
        {
            get => _destinationDirectory;
            set => SetProperty(ref _destinationDirectory, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        public ICommand SetSourceDirectoryCommand { get; }
        public ICommand SetDestinationDirectoryCommand { get; }
        public ICommand StartDuplicateCheckCommand { get; }
        public ICommand StopDuplicateCheckCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public Task4And5ViewModel()
        {
            SetSourceDirectoryCommand = new RelayCommand(_ => SetSourceDirectory());
            SetDestinationDirectoryCommand = new RelayCommand(_ => SetDestinationDirectory());
            StartDuplicateCheckCommand = new RelayCommand(async _ => await StartDuplicateCheckAsync());
            StopDuplicateCheckCommand = new RelayCommand(_ => StopDuplicateCheck());
            GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync());

            _operationReports = new List<OperationReport>();

            SourceDirectory = Path.GetFullPath("../../../TestFolder1/");
            DestinationDirectory = Path.GetFullPath("../../../TestFolder2/");
        }

        private void SetSourceDirectory()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                SourceDirectory = dialog.FolderName;
                ShowNotification($"Источник установлен: {SourceDirectory}");
            }
        }

        private void SetDestinationDirectory()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                DestinationDirectory = dialog.FolderName;
                ShowNotification($"Приёмник установлен: {DestinationDirectory}");
            }
        }

        private async Task StartDuplicateCheckAsync()
        {
            if (string.IsNullOrWhiteSpace(SourceDirectory) || string.IsNullOrWhiteSpace(DestinationDirectory))
            {
                ShowNotification("Укажите обе директории.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _operationReports.Clear();

            try
            {
                await Task.Run(() => ProcessDuplicates(SourceDirectory, DestinationDirectory, token), token);
                ShowNotification("Обработка завершена.");
            }
            catch (OperationCanceledException)
            {
                ShowNotification("Обработка остановлена.");
            }
            catch (Exception ex)
            {
                ShowNotification($"Ошибка: {ex.Message}");
            }
        }

        private void StopDuplicateCheck()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task GenerateReportAsync()
        {
            var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DuplicatesReport.txt");

            try
            {
                using (var writer = new StreamWriter(reportPath))
                {
                    await writer.WriteLineAsync("Отчёт о проделанных действиях");
                    await writer.WriteLineAsync(new string('-', 40));
                    foreach (var report in _operationReports)
                    {
                        await writer.WriteLineAsync(report.ToString());
                    }

                    await writer.WriteLineAsync(new string('-', 40));
                    await writer.WriteLineAsync($"Всего файлов обработано: {_operationReports.Count}");
                    await writer.WriteLineAsync($"Уникальных файлов: {_operationReports.Count(r => !r.WasDuplicate)}");
                    await writer.WriteLineAsync($"Найдено дубликатов: {_operationReports.Count(r => r.WasDuplicate)}");
                }

                ShowNotification($"Отчёт сохранён: {reportPath}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = reportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowNotification($"Ошибка при создании отчёта: {ex.Message}");
            }
        }
        private string GetUniqueFilePath(string directory, string fileNameWithoutExtension, string extension)
        {
            int counter = 1;
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}({counter}){extension}");
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }

        private void ProcessDuplicates(string sourceDir, string destDir, CancellationToken token)
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            var extensionToFiles = new Dictionary<string, List<InfoOfFile>>(); // Расширение → список файлов

            // Сначала собираем информацию о всех файлах
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                // Достать расширение файла
                var fileExtension = Path.GetExtension(file);

                // Вычислить хеш содержимого файла
                var fileHash = FileDuplicateChecker.GetFileHash(SHA256.Create(), file);

                // Проверить наличие расширения в словаре
                if (!extensionToFiles.ContainsKey(fileExtension))
                {
                    extensionToFiles[fileExtension] = new List<InfoOfFile>();
                }

                // Добавить файл в список
                extensionToFiles[fileExtension].Add(new InfoOfFile
                {
                    Extension = fileExtension,
                    Hash = fileHash,
                    SourcePath = file,
                    FileName = Path.GetFileName(file)
                });
            }

            // Теперь для каждого расширения и хэша выбираем оригинал с самым коротким именем
            foreach (var extensionFiles in extensionToFiles.Values)
            {
                // Группируем файлы по хэшам
                var groupedByHash = extensionFiles
                    .GroupBy(f => Convert.ToBase64String(f.Hash)) // Группируем по хэшам
                    .ToList();

                foreach (var group in groupedByHash)
                {
                    if (group.Count() == 1)
                    {
                        // Если файлов с таким хэшем только один, он — оригинал
                        var file = group.First();
                        CopyFileToDestination(file, destDir);
                    }
                    else
                    {
                        // Если несколько файлов с одинаковым хэшем, выбираем оригинал
                        var original = group
                            .OrderBy(f => f.FileName.Length)  // Оригинал — с самым коротким именем
                            .ThenBy(f => f.FileName)         // При равной длине — по алфавиту
                            .First();

                        foreach (var duplicate in group.Where(f => f != original))
                        {
                            // Добавляем информацию о дубликате
                            _operationReports.Add(new OperationReport
                            {
                                FileName = duplicate.FileName,
                                SourcePath = duplicate.SourcePath,
                                WasDuplicate = true,
                                OriginalFileName = original.FileName,
                                OriginalFilePath = original.SourcePath
                            });
                        }

                        // Копируем оригинал
                        CopyFileToDestination(original, destDir);
                    }
                }
            }
        }
        private MessageBoxResult? ClearOrRename = null!;

        private void CopyFileToDestination(InfoOfFile file, string destDir)
        {
            var destinationPath = Path.Combine(destDir, Path.GetFileName(file.SourcePath));

            if (File.Exists(destinationPath))
            {
                // Если файл с таким именем уже существует, запрашиваем действие пользователя
                ClearOrRename ??= MessageBox.Show(
                        $"Файл \"{Path.GetFileName(file.SourcePath)}\" уже существует в приёмнике.\n" +
                        "Да - очистить папку;\nНет - сгенерировать уникальное имя;",
                        "Файл уже существует",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);


                if (ClearOrRename == MessageBoxResult.Yes)
                {
                    Directory.GetFiles(destDir).ToList().ForEach(File.Delete);
                    File.Copy(file.SourcePath, destinationPath);
                }
                else
                {
                    destinationPath = GetUniqueFilePath(destDir, Path.GetFileNameWithoutExtension(file.FileName), file.Extension);
                    File.Copy(file.SourcePath, destinationPath);
                }
            }
            else
            {
                File.Copy(file.SourcePath, destinationPath);
            }

            _operationReports.Add(new OperationReport
            {
                FileName = Path.GetFileName(file.SourcePath),
                SourcePath = file.SourcePath,
                DestinationPath = destinationPath,
                WasDuplicate = false
            });
        }


        private CancellationTokenSource _notificationCancellationTokenSource;

        private void ShowNotification(string message, int durationMs = 3000)
        {
            _notificationCancellationTokenSource?.Cancel();
            _notificationCancellationTokenSource = new CancellationTokenSource();

            var token = _notificationCancellationTokenSource.Token;

            App.Current.Dispatcher.Invoke(() =>
            {
                NotificationMessage = message;
            });

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(durationMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            NotificationMessage = string.Empty;
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    //Игнор
                }
            });
        }
    }
}
