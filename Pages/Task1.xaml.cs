using System.Windows.Controls;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        public Task1()
        {
            InitializeComponent();
            DataContext = new ViewModels.TicTacToeViewModel();
        }
    }
}
