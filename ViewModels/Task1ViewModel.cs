using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class Task1ViewModel : BaseViewModel
    {
        private string _inputText;
        private string _reportText;
        private CancellationTokenSource _cancellationTokenSource;
        private string _notificationMessage;

        public string NotificationMessage
        {
            get => _notificationMessage;
            set
            {
                _notificationMessage = value;
                OnPropertyChanged();
            }
        }
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public string ReportText
        {
            get => _reportText;
            set => SetProperty(ref _reportText, value);
        }

        public bool IsSentenceCountChecked { get; set; } = true;
        public bool IsCharCountChecked { get; set; } = true;
        public bool IsWordCountChecked { get; set; } = true;
        public bool IsQuestionCountChecked { get; set; } = false;
        public bool IsExclamationCountChecked { get; set; } = false;

        public ICommand AnalyzeCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SaveReportCommand { get; }

        public Task1ViewModel()
        {
            AnalyzeCommand = new RelayCommand(async _ => await AnalyzeTextAsync());
            StopCommand = new RelayCommand(_ => StopAnalysis());
            SaveReportCommand = new RelayCommand(async _ => await SaveReportAsync());
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
        private async Task AnalyzeTextAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                ShowNotification("Введите текст для анализа.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                ReportText = await Task.Run(() => AnalyzeText(InputText, token), token);
            }
            catch (OperationCanceledException)
            {
                ShowNotification("Анализ был остановлен.");
            }
        }

        private void StopAnalysis()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task SaveReportAsync()
        {
            if (string.IsNullOrWhiteSpace(ReportText))
            {
                ShowNotification("Отчёт отсутствует, нечего сохранять.");
                return;
            }

            var savePath = "Report.txt";
            try
            {
                await File.WriteAllTextAsync(savePath, ReportText);
                ShowNotification($"Отчёт сохранён в файл: {savePath}");
            }
            catch (Exception ex)
            {
                ShowNotification($"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        private string AnalyzeText(string text, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var report = "Отчёт анализа текста:\n";

            if (IsSentenceCountChecked)
            {
                int sentenceCount = Regex.Matches(text, @"[.!?]").Count;
                report += $"Количество предложений: {sentenceCount}\n";
            }

            token.ThrowIfCancellationRequested();

            if (IsCharCountChecked)
            {
                int charCount = text.Length;
                report += $"Количество символов: {charCount}\n";
            }

            token.ThrowIfCancellationRequested();

            if (IsWordCountChecked)
            {
                int wordCount = Regex.Matches(text, @"\b\w+\b").Count;
                report += $"Количество слов: {wordCount}\n";
            }

            token.ThrowIfCancellationRequested();

            if (IsQuestionCountChecked)
            {
                int questionCount = Regex.Matches(text, @"\?").Count;
                report += $"Вопросительных предложений: {questionCount}\n";
            }

            token.ThrowIfCancellationRequested();

            if (IsExclamationCountChecked)
            {
                int exclamationCount = Regex.Matches(text, @"!").Count;
                report += $"Восклицательных предложений: {exclamationCount}\n";
            }

            return report;
        }
    }
}