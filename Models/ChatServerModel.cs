using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public class ChatServerModel : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cts;

        public event Action<string> MessageReceived;

        public bool IsConnected => _client?.Connected ?? false;

        private string _login;

        public async Task ConnectAsync(string serverAddress, int port, string login)
        {
            if (IsConnected) return;

            try
            {
                _login = login;
                _client = new TcpClient();
                await _client.ConnectAsync(serverAddress, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _cts = new CancellationTokenSource();

                // Отправляем логин на сервер
                await _writer.WriteLineAsync($"Join [LOGIN]:{_login}");

                _ = Task.Run(() => ReceiveMessagesAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to connect to server.", ex);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to the server.");

            var formattedMessage = $"{_login}: {message}";
            await _writer.WriteLineAsync(formattedMessage);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await _reader.ReadLineAsync();
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
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _client?.Close();
            _cts = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
