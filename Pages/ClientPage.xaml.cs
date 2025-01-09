
using System.Windows.Controls;
using System.Windows.Input;
using WPFTasks.Core.ViewModels.Currency;

namespace WPFTasks.Pages
{
    public partial class ClientPage : Page
    {
        private static CurrencyClientViewModel Instance = new();
        public ClientPage()
        {
            InitializeComponent();
            DataContext = Instance;
            Instance.OnUpdateMessages += ScrollToEnd;
        }

        private void HintsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string selectedHint)
            {
                var viewModel = DataContext as CurrencyClientViewModel;
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
                if (DataContext is CurrencyClientViewModel vm && vm.SendMessageCommand.CanExecute(null))
                {
                    vm.SendMessageCommand.Execute(null);
                }
                e.Handled = true;
            }
            if(e.Key == Key.Tab)
            {
                if (DataContext is CurrencyClientViewModel vm)
                {
                    int index = InputTextBox.Text.LastIndexOf('/');
                    if (index == -1) return;
                    string text = InputTextBox.Text[index..];
                    text = vm.TryCompleteCommand(text);
                    InputTextBox.Text = InputTextBox.Text[..index] + text;
                    CareInputTextBoxToEnd();
                    e.Handled = true;
                }
            }
        }
        // Переместить каретку в конец
        public void CareInputTextBoxToEnd()
            => InputTextBox.CaretIndex = InputTextBox.Text.Length;

        private void ScrollToEnd()
        {
            MessagesListBox.ScrollIntoView(MessagesListBox.Items[^1]);
        }
    }
}
