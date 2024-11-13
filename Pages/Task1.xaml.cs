
using System.Windows;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        public Task1()
        {
            InitializeComponent();
        }

        private void MethodOne(object sender, RoutedEventArgs e)
        {
            var task = new Task(() => ShowDate("Способ 1"));
            task.Start();
        }
        private void MethodTwo(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => ShowDate("Способ 2"));
        }
        private void MethodThree(object sender, RoutedEventArgs e)
        {
            Task.Run(() => ShowDate("Способ 3"));
        }

        private void ShowDate(string who)
        {
            ClearTextBox(OutputTextBox);
            AppendText(OutputTextBox, $"From: {who}");
            AppendText(OutputTextBox, DateTime.Now.ToString());
        }
        private void ClearTextBox(TextBox textBox)
            => Dispatcher.Invoke(textBox.Clear);
        private void AppendText(TextBox textBox, string text)
        {
            Dispatcher.Invoke(() =>
            {
                textBox.AppendText(text + Environment.NewLine);
            });
        }
    }
}
