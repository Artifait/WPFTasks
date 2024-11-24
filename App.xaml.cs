using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
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

            _semaphore = new Semaphore(1, 1, semaphoreName, out isNewInstance);

            if (!_semaphore.WaitOne(TimeSpan.Zero))
            {
                MessageBox.Show("Максимально допустимое количество экземпляров уже запущено.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0); 
            }

            try
            {
                if (e.Args.Length == 1)
                {
                    if (AllocConsole())
                    {
                        var view = new MainView(new ViewModels.MainViewModel(e.Args[0].ToString()));
                        view.ShowInitialScreen();

                        while (true)
                        {
                            Console.Clear();
                            Console.WriteLine("1. Start Scan");
                            Console.WriteLine("2. Stop Scan");
                            Console.WriteLine("3. Generate Report");
                            Console.WriteLine("4. Exit");

                            var key = Console.ReadLine();
                            try
                            {
                                switch (key?[0])
                                {
                                    case '1':
                                        view.StartScan();
                                        break;
                                    case '2':
                                        view.StopScan();
                                        break;
                                    case '3':
                                        view.GenerateReport();
                                        break;
                                    case '4':
                                        return;
                                }
                            }
                            catch { }
                        }
                    }
                    

                }
                else
                {
                    base.OnStartup(e);
                }
            }
            catch(Exception ex) { MessageBox.Show("Мы не смогли запуститься из консоли.\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); base.OnStartup(e); }
            finally { FreeConsole(); }

        }
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        protected override void OnExit(ExitEventArgs e)
        {
            _semaphore?.Release(); 
            base.OnExit(e);
        }
    }
}
