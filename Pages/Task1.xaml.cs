
using System.Windows;
using System.Windows.Controls;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        public Task1()
        {
            InitializeComponent();
            DataContext = new Task1ViewModel();
        }
    }
}
