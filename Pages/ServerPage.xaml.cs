using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
            DataContext = new ServerPageViewModel();
        }
    }
}
