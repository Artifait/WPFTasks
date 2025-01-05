
using System.Windows.Controls;
using System.Windows.Input;
using WPFTasks.Core.ViewModels;

namespace WPFTasks.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage()
        {
            InitializeComponent();
            //DataContext = new ClientPageViewModel(App.Current.Dispatcher);
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //if (DataContext is ClientPageViewModel vm && vm.SendMessageCommand.CanExecute(null))
                //{
                //    vm.SendMessageCommand.Execute(null);
                //}
            }
        }
    }
}
