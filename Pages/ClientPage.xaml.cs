
using System.Windows.Controls;
using System.Windows.Input;
using WPFTasks.Core.ViewModels;

namespace WPFTasks.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage()
        {
            InitializeComponent();
            //DataContext = new ClientPageViewModel(App.Current.Dispatcher);
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
                    InputTextBox.Text = vm.TryCompleteCommand(InputTextBox.Text);
                    CareInputTextBoxToEnd();
                    e.Handled = true;
                }
            }
        }
        // Переместить каретку в конец
        public void CareInputTextBoxToEnd()
            => InputTextBox.CaretIndex = InputTextBox.Text.Length;
    }
}
