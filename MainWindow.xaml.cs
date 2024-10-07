
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;


namespace WPFTasks
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tg_Btn.IsChecked = false;
        }
        private void HidePopup(object sender, MouseEventArgs e)
        {
            Popup.Visibility = Visibility.Collapsed;
            Popup.IsOpen = false;
        }
        private void ShowPopup(UIElement element, string text)
        {
            if (Tg_Btn.IsChecked == false)
            {
                Popup.PlacementTarget = element;
                Popup.Placement = PlacementMode.Right;
                Popup.IsOpen = true;
                Header.PopupText.Text = text;
            }
        }

        // Start: MenuLeft PopupButton //
        private void btnHome_MouseEnter(object sender, MouseEventArgs e) => ShowPopup(btnHome, "Домик");
        private void btnTask1_MouseEnter(object sender, MouseEventArgs e) => ShowPopup(btnTask1, "Task 1");
        private void btnSetting_MouseEnter(object sender, MouseEventArgs e) => ShowPopup(btnSetting, "Настройки");
        

        // End: MenuLeft PopupButton //

        // Start: Button Close | Restore | Minimize 
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        // End: Button Close | Restore | Minimize

        private void btnHome_Click(object sender, RoutedEventArgs e)
            => fContainer.Navigate(new System.Uri("Pages/Home.xaml", UriKind.RelativeOrAbsolute));

        private void btnSettings_Click(object sender, RoutedEventArgs e)
            => fContainer.Navigate(new System.Uri("Pages/Settings.xaml", UriKind.RelativeOrAbsolute));

    }
}
