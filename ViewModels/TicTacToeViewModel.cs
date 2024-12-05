using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class TicTacToeViewModel : BaseViewModel
    {
        private TicTacToeClient _client;
        private string _ipAddress = "127.0.0.1";
        private string _port = "52222";
        private string _statusMessage;
        private CellType[,] _gameBoard;
        private PlayerType _currentPlayer;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;

        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public CellType[,] GameBoard
        {
            get => _gameBoard;
            set { _gameBoard = value; OnPropertyChanged(); }
        }

        public PlayerType CurrentPlayer
        {
            get => _currentPlayer;
            set { _currentPlayer = value; OnPropertyChanged(); }
        }

        public ICommand ConnectCommand { get; }
        public ICommand PlayCommand { get; }

        public TicTacToeViewModel()
        {
            _client = new TicTacToeClient();
            _gameBoard = new CellType[3, 3];
            ConnectCommand = new RelayCommand(Connect);
            PlayCommand = new RelayCommand<string>(MakeMove, (string a) => _isConnected);

            _client.MessageReceived += OnGameMessageReceived;
        }

        public async void Connect()
        {
            try
            {
                int port = int.TryParse(Port, out int p) ? p : 0;
                await _client.ConnectAsync(IpAddress, port);
                StatusMessage = "Connected to the server.";
                _isConnected = true;

                _cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => _client.ReceiveMessageAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error connecting: {ex.Message}";
            }
        }
        private void OnGameMessageReceived(GameMessage message)
        {
            // Обновление игрового поля и статуса
            GameBoard = message.Cells;
            CurrentPlayer = message.NextPlayer ?? PlayerType.None;

            if (message.GameIsEnd)
            {
                StatusMessage = "Game over! " + (message.PastPlayer == PlayerType.Cross ? "Cross wins!" : "Zero wins!");
            }
            else
            {
                StatusMessage = $"Past Player: {(message.PastPlayer == PlayerType.Cross ? "Cross" : "Zero")}. Next Player: {(CurrentPlayer == PlayerType.Cross ? "Cross" : "Zero")}.";
            }
        }

        private async void MakeMove(string cellIndexs)
        {
            MessageBox.Show($"MakeMove with cellindexs: {cellIndexs}");
            try
            {
                int cellIndex = int.Parse(cellIndexs);
                if (!_isConnected || CurrentPlayer == PlayerType.None || GameBoard[cellIndex / 3, cellIndex % 3] != CellType.NoBody)
                    return;

                int x = cellIndex / 3;
                int y = cellIndex % 3;

                var move = new UserGameMessage { x = x, y = y };

                // Отправка хода на сервер
                await _client.SendUserMoveAsync(move);
            }
            catch(Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            _client.Disconnect();
            StatusMessage = "Disconnected from the server.";
            _isConnected = false;
        }
    }
}
