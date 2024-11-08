using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task4.xaml
    /// </summary>
    public partial class Task4 : Page
    {
        public Task4()
        {
            InitializeComponent();
        }

        private async void SearchWordInFileAsync_Click(object sender, RoutedEventArgs e)
        {
            string word = SearchWordTextBox.Text;
            string filePath = FilePathTextBox.Text;

            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                ResultTextBlock.Text = "Введите корректные слово и путь к файлу.";
                return;
            }

            ResultTextBlock.Text = "Выполняется поиск...";
            int count = await Task.Run(() => CountWordOccurrences(filePath, word));
            Result.Text = $"Результат: Слово '{word}' найдено {count} раз(а).";
            ResultTextBlock.Text = "Готово";
        }

        private int CountWordOccurrences(string filePath, string word)
        {
            int count = 0;
            string content = File.ReadAllText(filePath, Encoding.UTF8);
            count = Regex.Matches(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase).Count;
            return count;
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }
    }
}
