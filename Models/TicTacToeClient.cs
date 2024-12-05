using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public class TicTacToeClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        public event Action<GameMessage> MessageReceived;

        public TicTacToeClient()
        {
            _tcpClient = new TcpClient();
        }

        public async Task ConnectAsync(string serverIp, int serverPort)
        {
            await _tcpClient.ConnectAsync(serverIp, serverPort);
            _networkStream = _tcpClient.GetStream();

        }

        /// <summary>
        /// Получение сообщения от сервера.
        /// </summary>
        public async Task ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = GameMessage.Deserialize(await DeliveryServis.ReadAsync(_networkStream, cancellationToken));
                        if (message != null)
                        {
                            MessageReceived?.Invoke(message);
                        }
                        else
                        {
                            Disconnect();
                            break;
                        }
                    }
                    catch { }    
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }


        public async Task SendUserMoveAsync(UserGameMessage move)
        {
            // Сериализация и отправка хода пользователя.
            string serializedMove = JsonSerializer.Serialize(move);
            await DeliveryServis.WriteAsync(_networkStream, serializedMove);
        }

        /// <summary>
        /// Закрытие соединения с сервером.
        /// </summary>
        public void Disconnect()
        {
            _networkStream?.Dispose();
            _tcpClient?.Close();
        }
    }
}
