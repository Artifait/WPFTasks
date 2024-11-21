using System.Configuration;
using System.Data;
using System.Windows;

namespace WPFTasks
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Semaphore _semaphore;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string semaphoreName = "LimitedInstancesWPFTasks";
            bool isNewInstance = false;

            _semaphore = new Semaphore(5, 5, semaphoreName, out isNewInstance);

            if (!_semaphore.WaitOne(TimeSpan.Zero))
            {
                MessageBox.Show("Максимально допустимое количество экземпляров уже запущено.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0); 
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _semaphore?.Release(); 
            base.OnExit(e);
        }
    }
}
