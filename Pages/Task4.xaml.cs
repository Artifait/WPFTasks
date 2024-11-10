using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task4.xaml
    /// </summary>
    public partial class Task4 : Page
    {
        private Semaphore tableSemaphore = new(5, 5); 
        private List<Player> allPlayers = [];
        private List<Task> playerTasks = [];
        private static Random random = new();
        private int totalPlayers = random.Next(20, 101); 
        private int winningNumber;

        private void StartSimulation()
        {
            allPlayers.Clear();

            Task.Run(() =>
            {
                List<Player> activePlayers = new List<Player>();
                int playersProcessed = 0;

                for (int i = 0; i < 5 && playersProcessed < totalPlayers; i++)
                {
                    var player = new Player(playersProcessed + 1, random.Next(50, 201));
                    activePlayers.Add(player);
                    allPlayers.Add(player);
                    playersProcessed++;
                }

                while (playersProcessed < totalPlayers || activePlayers.Any(p => p.CurrentAmount > 0 && p.GamesPlayed < p.TargetGames))
                {
                    winningNumber = random.Next(0, 37);

                    List<Task> roundTasks = activePlayers
                        .Where(player => player.CurrentAmount > 0 && player.GamesPlayed < player.TargetGames)
                        .Select(player => Task.Run(() =>
                        {
                            tableSemaphore.WaitOne();
                            try
                            {
                                player.PlaceBet(winningNumber);
                            }
                            finally
                            {
                                tableSemaphore.Release();
                            }
                        }))
                        .ToList();

                    Task.WhenAll(roundTasks).Wait();

                    for (int i = 0; i < activePlayers.Count; i++)
                    {
                        if ((activePlayers[i].CurrentAmount == 0 || activePlayers[i].GamesPlayed >= activePlayers[i].TargetGames) && playersProcessed < totalPlayers)
                        {
                            var newPlayer = new Player(playersProcessed + 1, random.Next(50, 201));
                            activePlayers[i] = newPlayer;
                            allPlayers.Add(newPlayer);
                            playersProcessed++;
                        }
                    }
                }

                GenerateReport();
            });
        }



        private void GenerateReport()
        {
            StringBuilder report = new();
            StringBuilder winners = new();
            StringBuilder losers = new();

            winners.AppendLine("Победили:");
            losers.AppendLine("Проиграли:");

            foreach (var player in allPlayers)
            {
                string playerStr = $"Игрок{player.PlayerId} [Начальная сумма: {player.StartingAmount}] [Конечная сумма: {player.CurrentAmount}] [Сыграл: {player.GamesPlayed} раз]";
                if (player.CurrentAmount - player.StartingAmount >= 0)
                    winners.AppendLine(playerStr);
                else
                    losers.AppendLine(playerStr);
            }
            report.AppendLine(winners.ToString());
            report.AppendLine(losers.ToString());

            File.WriteAllText("CasinoReport.txt", report.ToString());

            Dispatcher.Invoke(() => { OutputTextBox.Text = report.ToString(); });
        }
        private void StartThreads(object sender, RoutedEventArgs e)
        {
            StartSimulation();
        }

        public Task4()
        {
            InitializeComponent();
        }
    }

    public class Player
    {
        private static Random random = new Random();
        public int PlayerId { get; }
        public int StartingAmount { get; }
        public int CurrentAmount { get; private set; }
        public int GamesPlayed { get; private set; }
        public int TargetGames { get; } 

        public Player(int playerId, int startingAmount)
        {
            PlayerId = playerId;
            StartingAmount = startingAmount;
            CurrentAmount = startingAmount;
            GamesPlayed = 0;
            TargetGames = random.Next(1, 13); 
        }

        public bool PlaceBet(int winningNumber)
        {
            if (GamesPlayed >= TargetGames || CurrentAmount <= 0)
                return false;

            int betAmount = random.Next(1, CurrentAmount / 2 + 1);
            int chosenNumber = random.Next(0, 37);
            GamesPlayed++;

            if (chosenNumber == winningNumber)
            {
                CurrentAmount += betAmount;
                return true;
            }
            else
            {
                CurrentAmount -= betAmount;
                return false;
            }
        }
    }
}
