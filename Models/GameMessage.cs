using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public enum CellType
    {
        NoBody = 0,
        Cross = 1,
        Zero = 2,
    }
    public enum PlayerType
    {
        None = 0,
        Cross = 1,
        Zero = 2,
    }
    public class GameMessage
    {
        public CellType[,] Cells { get; set; }
        public PlayerType? NextPlayer { get; set; }
        public PlayerType PastPlayer { get; set; }
        public bool GameIsEnd { get; set; }
        public string? ServerMessage { get; set; }

        public static string Serialize(GameMessage message)
        {
            // Настройка для поддержки многомерных массивов
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(message, options);
        }

        public static GameMessage Deserialize(string json)
        {
            return JsonSerializer.Deserialize<GameMessage>(json) ?? throw new InvalidOperationException("Failed to deserialize GameMessage.");
        }
    }
    public class UserGameMessage
    {
        public int x { get; set; }
        public int y { get; set; }
    }
}
