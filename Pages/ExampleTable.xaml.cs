using System.Windows;
using System.Windows.Controls;
using WPFTasks.Models;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для ExampleTable.xaml
    /// </summary>
    public partial class ExampleTable : Page
    {
        public ExampleTable()
        {
            InitializeComponent();
        }

        private void OnChangeDB(object sender, SelectionChangedEventArgs e)
        {
            ((ExampleVM)DataContext).ChangeDB((DBConfig)dbComboBox.Items[dbComboBox.SelectedIndex]);
        }
    }
}
