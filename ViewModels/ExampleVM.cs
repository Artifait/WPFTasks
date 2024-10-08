using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Models;


namespace WPFTasks.ViewModels;

public class ExampleVM : INotifyPropertyChanged
{
    private DBManager _dbManager;
    private DBConfig _selectedDb;
    private DataTable _dataTable;

    public string Query { get; set; }
    public DataTable DataTable
    {
        get => _dataTable;
        set
        {
            _dataTable = value;
            OnPropertyChanged(nameof(DataTable));
        }
    }

    public ObservableCollection<DBConfig> Databases { get; set; }
    public ICommand ConnectCommand { get; }
    public ICommand ExecuteQueryCommand { get; }
    public ICommand UpdateDataCommand { get; }

    public ExampleVM()
    {
        _dbManager = new DBManager("../../../dbConfig.json");
        Databases = new ObservableCollection<DBConfig>(_dbManager.GetDBList());
        ConnectCommand = new RelayCommand(Connect);
        ExecuteQueryCommand = new RelayCommand(ExecuteQuery);
        UpdateDataCommand = new RelayCommand(UpdateData);
    }

    private void Connect(object parameter)
    {
        if (_selectedDb != null)
        {
            _dbManager.Connect(_selectedDb);
            MessageBox.Show(_dbManager.IsOpen() ? "Подключились!" : "Не удалось подключиться.");
        }
        else { MessageBox.Show("Выберите валидную бд."); }
    }
    public void ChangeDB(DBConfig dBConfig)
    {
        _selectedDb = dBConfig;
    }
    private void ExecuteQuery(object parameter)
    {
        if (!string.IsNullOrEmpty(Query))
        {
            try
            {
                DataTable = _dbManager.ExecuteQuery(Query);
            }
            catch (Exception)
            {
                MessageBox.Show("НЕ пОЛУЧИЛОСЬ");
            }
        }
    }

    private void UpdateData(object parameter)
    {
        if (DataTable != null)
        {
            _dbManager.UpdateData(DataTable);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}