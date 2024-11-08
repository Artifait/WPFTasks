using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using WPFTasks.Pages;
using Microsoft.Win32;
using System.Windows.Media;

namespace WPFTasks.ViewModels
{
    public class Task5ViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FileSearchResult> FileSearchResults { get; set; } = new ObservableCollection<FileSearchResult>();

        private string _searchWord;
        private string _result;
        private bool _shouldShowZeroFiles;

        public bool ShouldShowZeroFiles
        {
            get => _shouldShowZeroFiles;
            set
            {
                _shouldShowZeroFiles = value;
                OnPropertyChanged(nameof(ShouldShowZeroFiles));
                if(FileSearchResults.Count > 0 )
                    UpdateResult();
            }
        }
        public string Result
        {
            get => _result;
            set {
                _result = value;
                OnPropertyChanged(nameof(Result));
            }
        }
        public string SearchWord
        {
            get => _searchWord;
            set
            {
                _searchWord = value;
                OnPropertyChanged(nameof(SearchWord));
            }
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                _directoryPath = value;
                OnPropertyChanged(nameof(DirectoryPath));
            }
        }

        public ICommand SearchCommand => new AsyncRelayCommand(SearchWordInDirectoryAsync);
        public ICommand BrowseDirectoryCommand => new RelayCommand((a) => BrowseDirectory());
        public ICommand UpdateResultCommand => new RelayCommand((a) => UpdateResult());


        private async Task SearchWordInDirectoryAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchWord) || string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
            {
                MessageBox.Show("Введите корректное слово и путь к директории.");
                return;
            }

            FileSearchResults.Clear();

            await Task.Run(() =>
            {
                var files = Directory.EnumerateFiles(DirectoryPath, "*.*", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    int count = CountWordOccurrences(filePath, SearchWord);
                    App.Current.Dispatcher.Invoke(() => FileSearchResults.Add(new FileSearchResult
                    {
                        FileName = Path.GetFileName(filePath),
                        FileFolder = filePath,
                        Occurrences = count
                    }));
                }
            });
            UpdateResult();

        }

        public void UpdateResult()
        {
            if (FileSearchResults.Count == 0)
                MessageBox.Show("Вообще нигде не встричается такая последовательность битов");

            StringBuilder sb = new();
            foreach (var fsr in FileSearchResults)
            {
                if (fsr.Occurrences == 0)
                    if (!ShouldShowZeroFiles)
                        continue;
                sb.AppendLine(fsr.ToString());
            }
            Result = sb.ToString();
        }
        private int CountWordOccurrences(string filePath, string word)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                return content.Split(new[] { word }, StringSplitOptions.None).Length - 1;
            }
            catch
            {
                return 0;
            }
        }

        private void BrowseDirectory()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() ?? false)
            {
                DirectoryPath = dialog.FolderName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class FileSearchResult : INotifyPropertyChanged
    {
        private double _progress;
        private int _occurrences;
        private string _fileName;
        private string _fileFolder;

        public override string ToString()
        {
            return $"Название файла: {FileName}\nПуть к файлу: {FileFolder}\nКоличество вхождений слова: {Occurrences}\n";
        }
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(Occurrences));
            }
        }

        public string FileFolder
        {
            get => _fileFolder;
            set
            {
                _fileFolder = value;
                OnPropertyChanged(nameof(Occurrences));
            }
        }


        public int Occurrences
        {
            get => _occurrences;
            set
            {
                _occurrences = value;
                OnPropertyChanged(nameof(Occurrences));
            }
        }

        public SolidColorBrush BgColor => new SolidColorBrush(Colors.LightGray);
        public SolidColorBrush FgColor => new SolidColorBrush(Colors.Green);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
