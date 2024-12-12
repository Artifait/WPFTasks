
using System.Windows.Controls;
using WPFTasks.Core.ViewModels;

namespace WPFTasks.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage()
        {
            InitializeComponent();
            DataContext = new ClientPageViewModel();
        }
    }
}
