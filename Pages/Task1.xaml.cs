using System;
using System.Data;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using UniversalDBConnection;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        private PointDB _db;

        public Task1()
        {
            InitializeComponent();
            LoadProviders();
        }

        private void LoadProviders()
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();
            Provieders.Items.Clear();
            foreach (DataRow row in providers.Rows)
            {
                Provieders.Items.Add(row["InvariantName"]);
            }
        }

        private void Provieders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectionStrings.Items.Clear();
            string selectedProvider = Provieders.SelectedItem.ToString();

            var connectionStrings = ((App)Application.Current).Configuration.GetSection("ConnectionStrings").GetChildren();
            foreach (var connectionString in connectionStrings)
            {
                ConnectionStrings.Items.Add(connectionString.Key);
            }
        }

        private void ConnectionStrings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string selectedProvider = Provieders.SelectedItem.ToString();
                string connectionString = ((App)Application.Current).Configuration.GetConnectionString(ConnectionStrings.SelectedItem.ToString());

                _db = new PointDB(selectedProvider, connectionString);
                _db.OpenConnection();
                MessageBox.Show("Подключение успешно установлено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = QueryInputer.Text;
                DataTable result = _db.ExecuteQuery(query);

                DataGridView.ItemsSource = result.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запроса: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _db.UpdateData();
                MessageBox.Show("Данные успешно обновлены в базе данных.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}");
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.CloseConnection();
        }
    }
}
