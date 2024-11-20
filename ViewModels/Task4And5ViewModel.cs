using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        public Task4ViewModel()
        {
            SetSourceDirectoryCommand = new RelayCommand(_ => SetSourceDirectory());
            SetDestinationDirectoryCommand = new RelayCommand(_ => SetDestinationDirectory());
            StartDuplicateCheckCommand = new RelayCommand(async _ => await StartDuplicateCheckAsync());
            StopDuplicateCheckCommand = new RelayCommand(_ => StopDuplicateCheck());
            GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync());

            _operationReports = new List<OperationReport>();
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
                    foreach (var report in _operationReports)
                    {
                        await writer.WriteLineAsync(report.ToString());
                    }
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

        private void ProcessDuplicates(string sourceDir, string destDir, CancellationToken token)
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            var processedHashes = new Dictionary<string, string>();

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                var fileHash = FileDuplicateChecker.GetFileHash(SHA256.Create(), file);

                if (processedHashes.Values.Any(hash => StructuralComparisons.StructuralEqualityComparer.Equals(hash, fileHash)))
                {
                    _operationReports.Add(new OperationReport
                    {
                        FileName = Path.GetFileName(file),
                        SourcePath = file,
                        WasDuplicate = true
                    });
                    continue;
                }

                var destinationPath = Path.Combine(destDir, Path.GetFileName(file));
                File.Move(file, destinationPath);
                processedHashes[file] = Convert.ToBase64String(fileHash);

                _operationReports.Add(new OperationReport
                {
                    FileName = Path.GetFileName(file),
                    SourcePath = file,
                    DestinationPath = destinationPath,
                    WasDuplicate = false
                });
            }
        }

        private void ShowNotification(string message, int durationMs = 3000)
        {
            NotificationMessage = message;
            Task.Run(async () =>
            {
                await Task.Delay(durationMs);
                NotificationMessage = string.Empty;
            });
        }
    }
}
