using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WPFTasks.Models.SimulationOfBus;

namespace WPFTasks.Pages
{
    public partial class Task2 : Page
    {
        BusSimulation simulation = new();
        public Task2()
        {
            InitializeComponent();
            simulation.InitializeSimulation("Config.ini");
        }

        private void DisplayData(object sender, RoutedEventArgs e)
        {
            AppendText(OutputTextBox, simulation.GetReportOfSimulationState());
        }

        private void AppendText(TextBox textBox, string text)
        {
            Dispatcher.Invoke(() =>
            {
                textBox.AppendText(text + Environment.NewLine);
            });
        }
    }
}
