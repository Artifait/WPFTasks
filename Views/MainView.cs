using WPFTasks.ViewModels;

public class MainView
{
    //Когда уже в папке с exe шником - WPFTasks.exe ..\..\..\BannedWords.ini
    private readonly MainViewModel _viewModel;
    private readonly object _consoleLock = new object(); // Объект для блокировки
    private bool _isWas = false;
    public MainView(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        // Подписка на изменения свойств ViewModel
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_viewModel.ScanProgress):
                UpdateProgressBar();
                break;
            case nameof(_viewModel.IsScanning):
                UpdateScanStatus();
                break;
            case nameof(_viewModel.CurrentScanningDir):
                UpdateScanningDir();
                break;
            case nameof(_viewModel.BannedFiles):
                UpdateBannedFiles();
                break;
            case nameof(_viewModel.TopBannedWords):
                UpdateTopBannedWords();
                break;
        }
    }

    private void IsWas()
    {
        if (!_isWas)
        {
            _isWas = true;
            UpdateProgressBar();
            UpdateScanStatus();
            UpdateScanningDir();
            UpdateBannedFiles();
            UpdateTopBannedWords();
        }
    }
    // Метод для отображения прогресса сканирования
    private void UpdateProgressBar()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.SetCursorPosition(0, 6); // Позиция для прогресса
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"Progress: {Math.Clamp(_viewModel.ScanProgress, 0, 100)}%          ");
            Console.ResetColor();
        }
    }

    // Метод для обновления статуса сканирования
    private void UpdateScanStatus()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.SetCursorPosition(0, 4); // Позиция для статуса
            Console.ForegroundColor = _viewModel.IsScanning ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(_viewModel.IsScanning ? "Scanning in progress..." : "Scan stopped.");
            Console.ResetColor();
        }
    }

    // Метод для обновления текущей директории сканирования
    private void UpdateScanningDir()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.SetCursorPosition(0, 8); // Позиция для директории
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Scanning directory: {_viewModel.CurrentScanningDir}                                                                                                ");
            Console.ResetColor();
        }
    }

    // Метод для обновления списка найденных файлов с запрещенными словами
    private void UpdateBannedFiles()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.SetCursorPosition(0, 10); // Позиция для файлов
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Banned Files:");
            int lineIndex = 11;
            foreach (var file in _viewModel.BannedFiles)
            {
                Console.SetCursorPosition(0, lineIndex++);
                Console.WriteLine($"{lineIndex - 11}. - ({file.FilePath})");
            }
            Console.ResetColor();
        }
    }

    // Метод для обновления списка топовых запрещенных слов
    private void UpdateTopBannedWords()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.SetCursorPosition(0, 16); // Позиция для топовых слов
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Top 10 Banned Words:");
            int lineIndex = 17;
            foreach (var word in _viewModel.TopBannedWords)
            {
                Console.SetCursorPosition(0, lineIndex++);
                Console.WriteLine($"{word.Word}: {word.Count}");
            }
            Console.ResetColor();
        }
    }

    // Метод для вывода начального экрана
    public void ShowInitialScreen()
    {
        lock (_consoleLock) // Блокируем доступ к консоли для других потоков
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("=== Scan Application ===");
            Console.ResetColor();
            Console.WriteLine("1. Start Scan");
            Console.WriteLine("2. Stop Scan");
            Console.WriteLine("3. Generate Report");
            Console.WriteLine("4. Exit");
        }
    }

    // Метод для старта сканирования
    public void StartScan()
    {
        _viewModel.StartScanCommand.Execute(null);
        IsWas();
    }

    // Метод для остановки сканирования
    public void StopScan()
    {
        _viewModel.StopScanCommand.Execute(null);
    }

    // Метод для генерации отчета
    public void GenerateReport()
    {
        _viewModel.GenerateReportCommand.Execute(null);
    }
}
