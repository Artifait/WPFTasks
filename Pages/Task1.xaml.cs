using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using WindowsInput;
using WindowsInput.Native;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        private static ManualResetEvent _generationCompletedEvent = new(false);
        private CountdownEvent _countdownEvent;
        private readonly InputSimulator _inputSimulator = new();
        private readonly int ret = LoadKeyboardLayout("00000409", 1);
        private List<(int, int)> _numberPairs = null!;

        private const string PairsFile = "NumberPairs.txt";
        private const string SumsFile = "Sums.txt";
        private const string ProductsFile = "Products.txt";

        public Task1()
        {
            InitializeComponent();
        }

        // WinAPI функции
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern int LoadKeyboardLayout(string pwszKLID, uint Flags);

        private const int SW_RESTORE = 9; // Восстанавливает окно, если оно свернуто
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private async void StartThreads(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();

            _countdownEvent = new CountdownEvent(3);
            _generationCompletedEvent = new(false);
            var generateTask = Task.Run(() => GenerateNumberPairs());
            var sumTask = Task.Run(() => CalculateSums());
            var productTask = Task.Run(() => CalculateProducts());

            await Task.WhenAll(generateTask, sumTask, productTask);

            // Запускаем блокноты
            var pairProcess = Process.Start("notepad.exe", PairsFile);
            var sumsProcess = Process.Start("notepad.exe", SumsFile);
            var productsProcess = Process.Start("notepad.exe", ProductsFile);

            // Ожидаем, пока процессы запустятся
            await Task.Delay(500);

            // Задаем размеры окон и смещения
            AdjustNotepadWindow(pairProcess, 0);
            AdjustNotepadWindow(sumsProcess, 100);
            AdjustNotepadWindow(productsProcess, 200);

            // Настроим шрифт (имитация клавиш)
            SetFontStyle(pairProcess);
            SetFontStyle(sumsProcess);
            SetFontStyle(productsProcess);
        }
        private void CloseNotepads(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => OutputTextBox.AppendText("\n"));
            CloseNotepadWindow(PairsFile);
            CloseNotepadWindow(SumsFile);
            CloseNotepadWindow(ProductsFile);
        }
        private void CloseNotepadWindow(string file)
        {
            string targetFile = file; 

            Process[] processes = Process.GetProcessesByName("notepad");

            foreach (var process in processes)
            {
                try
                {
                    var mainWindowTitle = process.MainWindowTitle;

                    if (mainWindowTitle.Contains(targetFile))
                    {
                        Dispatcher.Invoke(() => OutputTextBox.AppendText($"Закрытие блокнота с файлом: {mainWindowTitle}\n"));
                        process.Kill(); 
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => OutputTextBox.AppendText("Ошибка: " + ex.Message + "\n"));
                }
            }
        }
        private void AdjustNotepadWindow(Process process, int xOffset)
        {
            if (process == null) return;

            // Получаем дескриптор окна блокнота
            IntPtr hWnd = FindWindow(null, process.MainWindowTitle);
            if (hWnd == IntPtr.Zero) return;

            ShowWindow(hWnd, SW_RESTORE); // Восстанавливаем окно, если оно свернуто

            // Устанавливаем размер и позицию окна
            int width = 260;
            int height = (int)SystemParameters.PrimaryScreenHeight - 100;
            SetWindowPos(hWnd, HWND_TOP, xOffset, 0, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }
        object locker = new();
        private async void SetFontStyle(Process process)
        {
            if (process == null) return;

            IntPtr hWnd = FindWindow(null, process.MainWindowTitle);
            if (hWnd == IntPtr.Zero) return;
            lock (locker)
            {
                SendKeysToNotepad(hWnd);
            }
        }

        private void SendKeysToNotepad(IntPtr hWnd)
        {
            SetForegroundWindow(hWnd);
            PostMessage(hWnd, 0x50, 1, ret);

            // Пауза, чтобы Notepad успел отобразить меню
            Thread.Sleep(200);

            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MENU);  // Alt
            Thread.Sleep(200);

            _inputSimulator.Keyboard.TextEntry("м");
            Thread.Sleep(100);

            _inputSimulator.Keyboard.TextEntry("ш");
            Thread.Sleep(100);

            _inputSimulator.Keyboard.TextEntry("Impact");
            Thread.Sleep(100);

            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(100);

            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(100);

            _inputSimulator.Keyboard.TextEntry("18");
            Thread.Sleep(100);

            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(100);
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(100);
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(100);
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(100);
        }

        private async Task GenerateNumberPairs()
        {
            try
            {
                var random = new Random();
                _numberPairs = new List<(int, int)>();

                // Генерация пар чисел и запись в файл
                using (StreamWriter writer = new(PairsFile))
                {
                    for (int i = 0; i < 50; i++)
                    {
                        int num1 = random.Next(1, 100);
                        int num2 = random.Next(1, 100);
                        _numberPairs.Add((num1, num2));
                        writer.WriteLine($"{num1}, {num2}");
                    }
                }

                Dispatcher.Invoke(() => OutputTextBox.AppendText("Первый поток: Пары чисел сгенерированы и записаны в файл.\n"));
            }
            finally
            {
                _generationCompletedEvent.Set(); // Уведомляем другие потоки, что генерация завершена
                _countdownEvent.Signal(); // Уменьшаем счетчик на 1
            }
        }

        private async Task CalculateSums()
        {
            _generationCompletedEvent.WaitOne(); // Ждем завершения первого потока

            try
            {
                var sums = _numberPairs.Select(pair => pair.Item1 + pair.Item2).ToList();

                await File.WriteAllLinesAsync(SumsFile, sums.Select(sum => sum.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText("Второй поток: Суммы пар чисел записаны в файл.\n"));
            }
            finally
            {
                _countdownEvent.Signal(); // Уменьшаем счетчик на 1 после завершения работы
            }
        }

        private async Task CalculateProducts()
        {
            _generationCompletedEvent.WaitOne(); // Ждем завершения первого потока

            try
            {
                var products = _numberPairs.Select(pair => pair.Item1 * pair.Item2).ToList();

                await File.WriteAllLinesAsync(ProductsFile, products.Select(product => product.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText("Третий поток: Произведения пар чисел записаны в файл.\n"));
            }
            finally
            {
                _countdownEvent.Signal(); // Уменьшаем счетчик на 1 после завершения работы
            }
        }
    }
}
