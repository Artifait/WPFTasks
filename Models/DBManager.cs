using System.Data;
using System.Data.Common;
using System.IO;
using Newtonsoft.Json;
using System.Windows;

namespace WPFTasks.Models
{
    public class DBConfig
    {
        public string Provider { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
        public string Alias { get; set; } = null!;
        public string ViewStr { get => ToString(); }
        public override string ToString() => Alias + ' ' + Provider;
    }

    /// <summary> Режим соединения </summary>
    public abstract class DBConnectionState
    {
        protected virtual void OnStartExecute(DBManager manager)
            { if (!IsOpen(manager)) Connect(manager); }

        protected virtual void OnEndExecute(DBManager manager) { }

        public static bool IsOpen(DBManager manager) 
            => manager.Connection?.State == ConnectionState.Open;

        public static void CloseConnection(DBManager manager)
        {
            manager.Connection?.Close();
        }

        public void Connect(DBManager manager)
            => Connect(manager, manager.CurrentDB);

        public void Connect(DBManager manager, DBConfig db)
        {
            if (!IsOpen(manager)) {
                manager.Factory = DbProviderFactories.GetFactory(db.Provider);
                manager.Connection = manager.Factory.CreateConnection();
                if (manager.Connection == null) throw new InvalidOperationException("Не удалось создать подключение.");
                manager.Connection.ConnectionString = db.ConnectionString;
                manager.Connection.Open();
            }
        }

        private DataTable _ExecuteQuery(DBManager manager, string query)
        {
            OnStartExecute(manager);

            if (!IsOpen(manager))
                throw new InvalidOperationException("Нет соединения с бд.");

            using var command = manager.Connection.CreateCommand();
            command.CommandText = query;
            manager.Adapter = manager.Factory.CreateDataAdapter();
            manager.Adapter.SelectCommand = command;
            manager.DataSet = new DataSet();
            manager.Adapter.Fill(manager.DataSet);
            manager.CommandBuilder = manager.Factory.CreateCommandBuilder();
            manager.CommandBuilder.DataAdapter = manager.Adapter;
            OnEndExecute(manager);
            return manager.DataSet.Tables[0];
        }
        public virtual DataTable ExecuteQuery(DBManager manager, string query)
            => _ExecuteQuery(manager, query);

        public virtual async Task<DataTable> ExecuteQueryAsync(DBManager manager, string query)
            => await Task.Run(() => _ExecuteQuery(manager, query));
    }

    /// <summary> Присоединенный режим </summary>
    public class ConnectedState : DBConnectionState { }

    /// <summary> Отсоединенный режим </summary>
    public class DisconnectedState : DBConnectionState
    {
        protected override void OnEndExecute(DBManager manager)
            => CloseConnection(manager);
    }

    public class DBManager
    {
        private List<DBConfig> _databases = [];
        private DBConnectionState _connectionState = null!;
        internal DbCommandBuilder CommandBuilder { get; set; }
        public DBConfig CurrentDB { get; internal set; } = null!;
        public DbDataAdapter Adapter { get; internal set; } = null!;
        public DataSet DataSet { get; internal set; } = null!;
        public DbProviderFactory Factory { get; internal set; } = null!;
        public DbConnection Connection { get; internal set; } = null!;

        public DBManager(string jsonFilePath)
        {
            //Прочитает из файла и потом с помощью GetDBList() => ComboBox
            if (File.Exists(jsonFilePath)) {
                string json = File.ReadAllText(jsonFilePath);
                List<DBConfig>? dbConfigs = JsonConvert.DeserializeObject<List<DBConfig>>(json);
                if (dbConfigs != null)
                   AddDB(dbConfigs);
            }
            else {
                MessageBox.Show("Не найден файл с конфигурацией для бд менеджера");
            }
            SetConnectionState(true);
        }

        public void UpdateData()
        {
            if (DataSet != null && Adapter != null)
                Adapter.Update(DataSet.Tables[0]);
        }

        public void UpdateData(DataTable dataTable)
        {
            if (dataTable != null && Adapter != null)
                Adapter.Update(dataTable!);
        }

        /// <summary> Установить режим: Присоединенный or Отсоединенный </summary>
        public void SetConnectionState(bool connected)
            => _connectionState = connected ? new ConnectedState() : new DisconnectedState();
        /// <summary> Установить бд, с которой работаем </summary>
        public void SetCurrentDB(DBConfig dB)
        {
            if (!_databases.Contains(dB)) AddDB(dB);
            CurrentDB = dB;

            if (_connectionState is ConnectedState)
                _connectionState.Connect(this);
        }
        public void CloseConnection() 
            => DBConnectionState.CloseConnection(this);
        public bool IsOpen() 
            => DBConnectionState.IsOpen(this);
        public DataTable ExecuteQuery(string query) 
            => _connectionState.ExecuteQuery(this, query);
        public async Task<DataTable> ExecuteQueryAsync(string query) 
            => await _connectionState.ExecuteQueryAsync(this, query);
        public void AddDB(string provider, string connStr, string alias)
            => _databases.Add(new DBConfig { Provider = provider, ConnectionString = connStr, Alias = alias });
        public void AddDB(DBConfig db)
            => _databases.Add(db);
        public void AddDB(List<DBConfig> dbList) 
            => _databases.AddRange(dbList);
        public List<DBConfig> GetDBList() 
            => _databases;
    }
}
