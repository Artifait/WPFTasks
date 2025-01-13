
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.HttpsPartOne
{
    public class Task2_Vm : BaseViewModel
    {
        private readonly GutenbergApiClient _apiClient;
        private bool _isLoading;

        public ObservableCollection<Book> Books { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadBooksCommand { get; }
        public ICommand LoadBookCommand { get; }

        public Task2_Vm()
        {
            _apiClient = new GutenbergApiClient();
            LoadBooksCommand = new RelayCommand(async _ => await LoadBooksAsync());
            LoadBookCommand = new RelayCommand<Book>(async book => await LoadBookAsync(book));

            // Автозагрузка топ-100 книг при инициализации
            LoadBooksCommand.Execute(null);
        }

        private async Task LoadBooksAsync()
        {
            IsLoading = true;

            try
            {
                var books = await _apiClient.GetTop100BooksAsync();
                Books.Clear();
                foreach (var book in books)
                {
                    book.CoverUrl = book.Formats.ContainsKey("image/jpeg") ? book.Formats["image/jpeg"] : null;
                    Books.Add(book);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при загрузке книг", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBookAsync(Book selectedBook)
        {
            if (selectedBook == null) return;

            try
            {
                string fileName = $"{selectedBook.Title}.txt";
                if (!File.Exists(fileName))
                {
                    // Загружаем текст книги по её URL
                    var bookText = await _apiClient.GetBookTextAsync(selectedBook.Id);
                    File.WriteAllText(fileName, bookText);
                }

                System.Diagnostics.Process.Start("notepad.exe", fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при загрузке текста книги", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
