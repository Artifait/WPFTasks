using System.Windows.Controls;

namespace WPFTasks.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage()
        {
            InitializeComponent();
            DataContext = new ViewModels.ChatViewModel();
        }
    }
}
