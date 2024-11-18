
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    public partial class Task2 : Page
    {
        public Task2()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        //private void DisplayData(object sender, RoutedEventArgs e)
        //{
        //    AppendText(OutputTextBox, Models.Simulation.core.GetReportOfSimulationState());
        //}

        private void AppendText(TextBox textBox, string text)
        {
            Dispatcher.Invoke(() =>
            {
                textBox.AppendText(text + Environment.NewLine);
            });
        }
    }
}
