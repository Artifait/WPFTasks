using System.Data;
using System.Data.Common;

namespace UniversalDBConnection
{
    public class PointDB
    {
        private DbProviderFactory _factory;
        private DbConnection _connection;
        private DbDataAdapter _adapter;
        private DbCommandBuilder _commandBuilder;
        private DataSet _dataSet;

        public ConnectionState StateDB { get => _connection.State; }

        public PointDB(string providerName, string connectionString)
        {
            _factory = DbProviderFactories.GetFactory(providerName);

            _connection = _factory.CreateConnection();
            if (_connection == null)
            {
                throw new InvalidOperationException("Не удалось создать подключение.");
            }

            _connection.ConnectionString = connectionString;
        }

        public void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        public void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        public DataTable ExecuteQuery(string query)
        {
            DataTable dataTable = new();
            _adapter = _factory.CreateDataAdapter();

            if (_adapter == null) return dataTable;

            using DbCommand command = _connection.CreateCommand();
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
    }
}
