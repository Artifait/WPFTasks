
using System.Windows;
using System.Windows.Controls;
using WPFTasks.Core.ViewModels.HttpsPartOne;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task2.xaml
    /// </summary>
    public partial class Task2 : Page
    {
        private Task2_Vm? Instance = null;

        public Task2()
        {
            InitializeComponent();
            Instance = (Task2_Vm)DataContext;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var book = (Book)((Button)sender).CommandParameter;

            Instance!.LoadBookCommand.Execute(book);
        } 
    }
}
