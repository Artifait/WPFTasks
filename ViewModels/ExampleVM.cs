using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Models;


namespace WPFTasks.ViewModels;

public class ExampleVM : INotifyPropertyChanged
{
    private DBManager _dbManager;
    private DBConfig _selectedDb;
    private DataTable _dataTable;
    public ObservableCollection<DBConfig> Databases { get; set; }
    public ICommand ConnectCommand { get; }
    public ICommand ExecuteQueryCommand { get; }
    public ICommand UpdateDataCommand { get; }
    public string Query { get; set; }
    public bool IsAsyncMode { get; set; }
    public DataTable DataTable {
        get => _dataTable;
        set {
            _dataTable = value;
            OnPropertyChanged(nameof(DataTable));
        }
    }
    public void ChangeDB(DBConfig dBConfig)
    => _selectedDb = dBConfig;


    public ExampleVM()
    {
        _dbManager = new DBManager("../../../dbConfig.json");
        Databases = new ObservableCollection<DBConfig>(_dbManager.GetDBList());
        ConnectCommand = new RelayCommand(Connect);
        ExecuteQueryCommand = new RelayCommand(ExecuteQuery);
        UpdateDataCommand = new RelayCommand(UpdateData);
    }

    private async void Connect(object parameter) 
    {
        if (_selectedDb != null) {
            if (IsAsyncMode) await Task.Run(() => _dbManager.SetCurrentDB(_selectedDb));
            else _dbManager.SetCurrentDB(_selectedDb);
            MessageBox.Show(_dbManager.IsOpen() ? "Подключились!" : "Не удалось подключиться.");
        }
        else { MessageBox.Show("Выберите валидную бд."); }
    }

    private async void ExecuteQuery(object parameter)
    {
        if (!string.IsNullOrWhiteSpace(Query)) {
            try {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if(IsAsyncMode) DataTable = await Task.Run(() => _dbManager.ExecuteQuery(Query));
                else DataTable = _dbManager.ExecuteQuery(Query);
                stopwatch.Stop();
                MessageBox.Show($"Запрос выполнялся на протяжении: {stopwatch.Elapsed.TotalSeconds} c.");
            }
            catch (Exception) {
                MessageBox.Show("НЕ пОЛУЧИЛОСЬ");
            }
        }
    }

    private async void UpdateData(object parameter)
    {
        try {
            if (IsAsyncMode) await Task.Run(() => _dbManager.UpdateData(DataTable));
            else _dbManager.UpdateData(DataTable); 
        }
        catch(Exception ex) { MessageBox.Show(ex.Message); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}