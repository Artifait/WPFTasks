
using System.Windows.Controls;
using System.Windows.Input;
using WPFTasks.Core.ViewModels.Core;
using WPFTasks.Core.ViewModels.RockPaperScissors;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для ServerPage.xaml
    /// </summary>
    public partial class ServerPage : Page
    {
        private static RockPaperScissorsServerVm Instance = new();

        public ServerPage()
        {
            InitializeComponent();
            DataContext = Instance;
        }

        private void HintsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string selectedHint)
            {
                var viewModel = DataContext as HintOnMessageInputBoxBaseVm;
                int index = selectedHint.IndexOf(' ');
                index = index == -1 ? selectedHint.Length : index;
                viewModel?.SelectHint(selectedHint[..index]);
                listBox.SelectedItem = null; // Сбрасываем выбор
                InputTextBox.Focus();
                CareInputTextBoxToEnd();
            }
        }


        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ServerViewModel vm && vm.SendMessageCommand.CanExecute(null))
                {
                    vm.SendMessageCommand.Execute(null);
                    vm.Hints.Clear();
                    vm.AreHintsVisible = vm.Hints.Any();
                }
                e.Handled = true;
            }
            if (e.Key == Key.Tab)
            {
                if (DataContext is HintOnMessageInputBoxBaseVm vm)
                {
                    int index = InputTextBox.Text.LastIndexOf('/');
                    if (index == -1) return;
                    string text = InputTextBox.Text[index..];
                    text = vm.TryCompleteCommand(text);
                    InputTextBox.Text = InputTextBox.Text[..index] + text;
                    CareInputTextBoxToEnd();
                    e.Handled = true;
                }
                e.Handled = true;
            }
        }
        // Переместить каретку в конец
        public void CareInputTextBoxToEnd()
            => InputTextBox.CaretIndex = InputTextBox.Text.Length;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd(); 
            }
        }
    }
}
