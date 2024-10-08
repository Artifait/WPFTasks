using System.Data;
using System.Data.Common;
using System.IO;
using Newtonsoft.Json;

namespace WPFTasks.Models;

public class DBConfig
{
    public string Provider { get; set; }
    public string ConnectionString { get; set; }
    public string Alias { get; set; }
    public override string ToString()
    {
        return Alias;
    }
}

public class DBManager
{
    private List<DBConfig> _databases = new();
    private DbProviderFactory _factory;
    private DbConnection _connection;
    private DbDataAdapter _adapter;
    private DbCommandBuilder _commandBuilder;
    private DataSet _dataSet;

    public DBManager(string jsonFilePath)
    {
        if (File.Exists(jsonFilePath))
        {
            string json = File.ReadAllText(jsonFilePath);
            List<DBConfig> dbConfigs = JsonConvert.DeserializeObject<List<DBConfig>>(json);
            if (dbConfigs != null)
            {
                _databases.AddRange(dbConfigs);
            }
        }
    }

    public void AddDB(string provider, string connStr, string alias)
        => _databases.Add(new DBConfig { Provider = provider, ConnectionString = connStr, Alias = alias });
    public void AddDB(DBConfig db) => _databases.Add(db);
    public void AddDB(List<DBConfig> dbList) => _databases.AddRange(dbList);
    public void ClearDBList() => _databases.Clear();
    public List<DBConfig> GetDBList() => _databases;

    public void CloseConnection() => _connection.Close();
    public void Connect(DBConfig db)
    {
        _factory = DbProviderFactories.GetFactory(db.Provider);
        _connection = _factory.CreateConnection();
        if (_connection == null) throw new InvalidOperationException("Не удалось создать подключение.");
        _connection.ConnectionString = db.ConnectionString;
        _connection.Open();
    }
    public bool IsOpen() => _connection.State == ConnectionState.Open;

    public DataTable ExecuteQuery(string query)
    {
        _adapter = _factory.CreateDataAdapter();
        if (_adapter == null) return new DataTable();

        using var command = _connection.CreateCommand();
        command.CommandText = query;
        _adapter.SelectCommand = command;

        _dataSet = new DataSet();
        _adapter.Fill(_dataSet);

        _commandBuilder = _factory.CreateCommandBuilder();
        _commandBuilder.DataAdapter = _adapter;

        return _dataSet.Tables[0];
    }

    public void UpdateData()
    {
        if (_dataSet != null && _adapter != null)
        {
            _adapter.Update(_dataSet.Tables[0]); 
        }
    }

    public void UpdateData(DataTable dataTable)
    {
        if (dataTable != null && _adapter != null)
        {
            _adapter.Update(dataTable);
        }
    }
}
