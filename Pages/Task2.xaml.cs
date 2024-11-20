using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    public partial class Task2 : Page
    {
        public Task2()
        {
            InitializeComponent();
            DataContext = new Task4And5ViewModel();
        }
    }
}
