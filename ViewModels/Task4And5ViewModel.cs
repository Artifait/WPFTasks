using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class Task4ViewModel : BaseViewModel
    {
        private string _sourceDirectory;
        private string _destinationDirectory;
        private string _notificationMessage;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _notificationCancellationTokenSource;

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
            set
            {
                _notificationMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand SetSourceDirectoryCommand { get; }
        public ICommand SetDestinationDirectoryCommand { get; }
        public ICommand StartDuplicateCheckCommand { get; }
        public ICommand StopDuplicateCheckCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public Task4ViewModel()
        {
            SetSourceDirectoryCommand = new RelayCommand(_ => SetSourceDirectory());
            SetDestinationDirectoryCommand = new RelayCommand(_ => SetDestinationDirectory());
            StartDuplicateCheckCommand = new RelayCommand(async _ => await StartDuplicateCheckAsync());
            StopDuplicateCheckCommand = new RelayCommand(_ => StopDuplicateCheck());
            GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync());
        }

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
                    // Игнорируем отмену
                }
            });
        }

        private void SetSourceDirectory()
        {
            var dialog = new OpenFolderDialog();
            bool? result = dialog.ShowDialog();

            if (result != null && result == true)
            {
                SourceDirectory = dialog.FolderName;
                ShowNotification($"Источник установлен: {SourceDirectory}");
            }
        }

        private void SetDestinationDirectory()
        {
            var dialog = new OpenFolderDialog();
            bool? result = dialog.ShowDialog();

            if (result != null && result == true)
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
            var reportPath = "DuplicatesReport.txt";
            try
            {
                await File.WriteAllTextAsync(reportPath, "Отчёт будет здесь.\n"); // Логика генерации отчёта
                ShowNotification($"Отчёт сохранён в файл: {reportPath}");
            }
            catch (Exception ex)
            {
                ShowNotification($"Ошибка при сохранении отчёта: {ex.Message}");
            }
        }

        private void ProcessDuplicates(string sourceDir, string destDir, CancellationToken token)
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            var fileHashes = new Dictionary<string, string>();
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                var hash = CalculateFileHash(file);

                if (!fileHashes.ContainsKey(hash))
                {
                    fileHashes[hash] = file;
                    var destinationPath = Path.Combine(destDir, Path.GetFileName(file));
                    File.Move(file, destinationPath);
                }
            }
        }

        private string CalculateFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(stream));
        }
    }
}
