
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WPFTasks.Core.ViewModels.HttpsPartOne;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task3.xaml
    /// </summary>
    public partial class Task3 : Page
    {
        private readonly GutenbergApiClient _client = new();
        private readonly Stopwatch _stopwatch = new();
        private ObservableCollection<Book> _searchResults = new();
        private readonly DispatcherTimer _dispatcherTimer = new();

        public Task3()
        {
            InitializeComponent();
            ResultsListView.ItemsSource = _searchResults;

            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50); 
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            TimerTextBlock.Text = $"Время поиска: {_stopwatch.Elapsed.TotalSeconds:F2} сек";
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Введите текст для поиска!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _searchResults.Clear();
            StatusTextBlock.Text = "Статус: Поиск...";
            TimerTextBlock.Text = "Время поиска: 0.00 сек";

            _stopwatch.Restart();
            _dispatcherTimer.Start();

            try
            {
                var results = await _client.SearchBooksAsync(query);
                foreach (var book in results)
                {
                    _searchResults.Add(book);
                }

                if (_searchResults.Count == 0)
                {
                    StatusTextBlock.Text = "Статус: Ничего не найдено.";
                }
                else
                {
                    StatusTextBlock.Text = $"Статус: Найдено {_searchResults.Count} книг.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Статус: Ошибка поиска.";
            }
            finally
            {
                _dispatcherTimer.Stop();
                _stopwatch.Stop();
                TimerTextBlock.Text = $"Время поиска: {_stopwatch.Elapsed.TotalSeconds:F2} сек";
            }
        }
    }

}

