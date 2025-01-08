
using System.Windows.Controls;
using WPFTasks.Core.ViewModels;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для ServerPage.xaml
    /// </summary>
    public partial class ServerPage : Page
    {
        public ServerPage()
        {
            InitializeComponent();
            DataContext = new CurrencyServerViewModel();
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }
    }
}
