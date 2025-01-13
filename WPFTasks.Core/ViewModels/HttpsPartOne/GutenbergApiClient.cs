
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace WPFTasks.Core.ViewModels.HttpsPartOne
{
    public class GutenbergApiClient
    {
        private const string BaseUrl = "https://gutendex.com/books";
        private static readonly HttpClient HttpClient = new(new RedirectHandler(new HttpClientHandler()));
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public GutenbergApiClient()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GutenbergApiClient)");
        }

        // Метод для получения 100 самых популярных книг
        public async Task<Book[]> GetTop100BooksAsync()
        {
            const int maxBooks = 100;
            const string baseUrl = $"{BaseUrl}?sort=popular&page=";
            var books = new List<Book>();
            int page = 1;

            while (books.Count < maxBooks)
            {
                string url = baseUrl + page;

                var jsonResponse = await GetFromCacheOrFetchAsync(url);
                var result = JsonConvert.DeserializeObject<GutenbergResponse>(jsonResponse);

                if (result?.Results == null || result.Results.Length == 0)
                {
                    break; 
                }

                books.AddRange(result.Results);

                if (result.Results.Length < 32)
                {
                    break;
                }

                page++;
            }

            return books.Take(maxBooks).ToArray();
        }


        // Метод для поиска книг
        public async Task<Book[]> SearchBooksAsync(string query)
        {
            var url = $"{BaseUrl}?search={Uri.EscapeDataString(query)}";
            var jsonResponse = await GetFromCacheOrFetchAsync(url);
            var result = JsonConvert.DeserializeObject<GutenbergResponse>(jsonResponse);
            return result?.Results ?? [];
        }

        // Метод для загрузки текста книги
        public async Task<string> GetBookTextAsync(int bookId)
        {
            var url = $"{BaseUrl}/{bookId}/";
            var jsonResponse = await GetFromCacheOrFetchAsync(url);
            var bookDetails = JsonConvert.DeserializeObject<BookDetails>(jsonResponse);
            var textUrl = bookDetails?.Formats.FirstOrDefault(f => f.Key.Contains("text/plain", StringComparison.CurrentCultureIgnoreCase)).Value;

            if (string.IsNullOrEmpty(textUrl))
            {
                throw new Exception("Текст книги недоступен.");
            }

            return await HttpClient.GetStringAsync("https://www.gutenberg.org/ebooks/46.txt.utf-8");
        }

        // Метод для скачивания всех книг автора
        public async Task DownloadBooksByAuthorAsync(string author, string downloadDirectory)
        {
            var books = await SearchBooksAsync(author);
            Directory.CreateDirectory(downloadDirectory);
            Process.Start("explorer.exe", downloadDirectory);

            var downloadTasks = books.Select(async book =>
            {
                try
                {
                    var text = await GetBookTextAsync(book.Id);
                    var filePath = Path.Combine(downloadDirectory, $"{SanitizeFileName(book.Title)}.txt");
                    await File.WriteAllTextAsync(filePath, text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, $"Ошибка при загрузке книги {book.Title}", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            await Task.WhenAll(downloadTasks);
        }

        // Вспомогательный метод: получение данных с кэшированием
        private async Task<string> GetFromCacheOrFetchAsync(string url)
        {
            if (_cache.TryGetValue(url, out var cachedResponse))
            {
                return cachedResponse;
            }

            var response = await HttpClient.GetStringAsync(url);
            _cache[url] = response;
            return response;
        }

        // Вспомогательный метод: удаление недопустимых символов из имени файла
        private static string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }

    // Модель данных для ответа API
    // МОжно сгенировать через -> Правка -> специальная вставка -> Вставить JSON как классы
    public class GutenbergResponse
    {
        public int count { get; set; }
        public string next { get; set; }
        public string? previous { get; set; }
        public Book[] Results { get; set; }
    }

    public class BookDetails
    {
        public Dictionary<string, string> Formats { get; set; }
    }

    public class Book
    {
        public string? CoverUrl { get; set; }

        public int Id { get; set; }
        public string Title { get; set; }
        public Author[] Authors { get; set; }
        public Translator[] Translators { get; set; }
        public string[] Subjects { get; set; }
        public string[] Bookshelves { get; set; }
        public string[] Languages { get; set; }
        public bool Copyright { get; set; }
        public string Media_type { get; set; }
        public Dictionary<string, string> Formats { get; set; }
        public int Download_count { get; set; }
    }

    public class Author
    {
        public string name { get; set; }
    }

    public class Translator
    {
        public string name { get; set; }
    }
}
