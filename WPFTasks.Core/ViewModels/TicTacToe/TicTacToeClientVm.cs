
using System.Windows;
using WPFTasks.Core.Models.TicTacToe;
using WPFTasks.Core.ViewModels.Core;
using WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic;
using System.Text;

namespace WPFTasks.Core.ViewModels.TicTacToe
{
    public class TicTacToeClientVm : ClientViewModel
    {
        private readonly TicTacToeClient _client;
        private string _lastStateBoard = "Покачто нету...";
        public TicTacToeClientVm() : base(new TicTacToeClient())
        {
            _client = (TicTacToeClient)Client;

            _commandProcessor!
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip: str> <Port: int> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds: int>", HandleDelay)
                .AddCommand("/FindGame", "/FindGame <Type: int> 0 -> Human-Human; 1 -> Human-Pc; 2 -> Pc-Pc", HandleFindGame)
                .AddCommand("/SendMove", "/SendMove <CellNumber: int>", HandleSendMove)
                .AddCommand("/GetLastBoardState", "/GetLastBoardState", HandleGetLastBoardState);

            _client.OnEndSession += data
                => AddMessage("Server", data.Payload);

            _client.OnGameStarted += data 
                => AddMessage("Server", "Игра началась!");

            _client.OnPlayerTurn += data =>
            {
                _lastStateBoard = BoardToString(data.Board);
                AddMessage("Server", $"{_lastStateBoard}\nТвой ход!");
            };

            _client.OnUpdateGameBoard += data =>
            {
                _lastStateBoard = BoardToString(data.Board);
                AddMessage("Server", $"{_lastStateBoard}\nХодит второй игрок!");
            };
        }

        protected override async Task ExecuteCommandAsync(string input)
        {
            try
            {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex)
            {
                AddMessage("_commandProcessor", ex.Message);
            }
        }



        private async Task HandleFindGame(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
            {
                await _client.SendFindGameRequest((GameTypes)int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Неверный формат /FindGame...");
                throw new ArgumentException("Неверный формат /FindGame...");
            }
        }
        private async Task HandleSendMove(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
            {
                await _client.SendUserMove(int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Неверный формат /SendMove...");
                throw new ArgumentException("Неверный формат /SendMove...");
            }
        }

        private async Task HandleGetLastBoardState(string _)
            => AddMessage("Save", _lastStateBoard);

        private async Task HandleDisconnect(string input)
            => _client.Disconnect();

        private async Task HandleClear(string input)
            => Application.Current.Dispatcher.Invoke(() => Messages.Clear());

        private async Task HandleConnect(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _client.ConnectAsync(parts[1], int.Parse(parts[2]));
            }
            else if (parts.Length == 1)
            {
                await _client.ConnectAsync(IpAddress, int.Parse(Port));
            }
            else
            {
                ShowMessageBox("Команда /Connect должна быть в формате: { /Connect <Ip> <Port> or /Connect }");
            }
        }

        private async Task HandleDelay(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
            {
                await Task.Delay(int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Команда /Delay должна быть в формате: { /Delay <milliseconds> }");
                throw new ArgumentException("Неверный формат /Delay...");
            }
        }

        private string BoardToString(char[,] board)
        {
            try
            {
                // Разделитель между строками
                string divider = "------------------\n";

                // Формирование строк доски
                string row1 = $"  {FormatCell(board[0, 0], 0)}  |  {FormatCell(board[0, 1], 1)}  |  {FormatCell(board[0, 2], 2)}  \n";
                string row2 = $"  {FormatCell(board[1, 0], 3)}  |  {FormatCell(board[1, 1], 4)}  |  {FormatCell(board[1, 2], 5)}  \n";
                string row3 = $"  {FormatCell(board[2, 0], 6)}  |  {FormatCell(board[2, 1], 7)}  |  {FormatCell(board[2, 2], 8)}  \n";

                // Объединение всех строк в итоговую доску
                string result = 
                                divider +
                                row1 + divider +
                                row2 + divider +
                                row3 + divider;

                return result;
            }
            catch (Exception ex)
            {
                ShowMessageBox(ex.Message);
                return "Ошибка при отображении игровой доски...";
            }
        }

        // Вспомогательный метод для форматирования содержимого клетки
        private string FormatCell(char cell, int cellIndex)
        {
            // Если клетка пуста, отображаем её номер
            return cell == ' ' ? $" {cellIndex} " : $" {cell} ";
        }
    }
}
