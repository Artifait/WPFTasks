using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ScannerService _scannerService;
        private int _scanProgress;
        private bool _isScanning;
        private IniFileReader _iniFileReader;

        public ObservableCollection<ScanResult> BannedFiles { get; } = [];
        public ObservableCollection<BannedWordStatistics> TopBannedWords { get; } = [];

        public int ScanProgress
        {
            get => _scanProgress;
            set { _scanProgress = value; OnPropertyChanged(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

        public ICommand StartScanCommand { get; }
        public ICommand StopScanCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public MainViewModel()
        {
            _iniFileReader = new IniFileReader("../../../BannedWords.ini");

            _scannerService = new ScannerService(_iniFileReader.BannedWords, _iniFileReader.AllowedExtensions, _iniFileReader.ReplacementWord, OnFileFound, OnProgressUpdated);

            // Инициализация команд
            StartScanCommand = new RelayCommand(_ => Task.Run(StartScan));
            StopScanCommand = new RelayCommand(_ => StopScan());
            GenerateReportCommand = new RelayCommand(_ => GenerateReport());
        }

        private async Task StartScan()
        {
            StopScan();
            IsScanning = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                BannedFiles.Clear();
                TopBannedWords.Clear();
            });

            await _scannerService.ScanAsync();
            IsScanning = false;
            UpdateTopBannedWords();
        }

        private void StopScan()
        {
            try{ _scannerService.CancelScan(); }
            catch { }
            IsScanning = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                ScanProgress = 0;
            });
        }

        private void GenerateReport()
        {
            _scannerService.GenerateReport(BannedFiles);
        }

        private void OnFileFound(ScanResult result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BannedFiles.Add(result);
                UpdateTopBannedWords();
            });
            
        }

        private void OnProgressUpdated(int progress)
        {
            ScanProgress = progress;
        }

        private void UpdateTopBannedWords()
        {
            var topWords = _scannerService.GetTopBannedWords(10);
            TopBannedWords.Clear();
            foreach (var word in topWords)
            {
                TopBannedWords.Add(word);
            }
        }
    }
}
