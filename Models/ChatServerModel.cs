using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        public async Task ConnectAsync(string serverAddress, int port, string username, string password)
        {
            if (IsConnected) return;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(serverAddress, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _cts = new CancellationTokenSource();

                // Аутентификация
                var a = await _reader.ReadLineAsync();
                await _writer.WriteLineAsync(username);
                var b = await _reader.ReadLineAsync();
                await _writer.WriteLineAsync(password);

                var authResponse = await _reader.ReadLineAsync();
                if (authResponse != "AUTH_SUCCESS")
                {
                    throw new InvalidOperationException("Authentication failed: " + authResponse);
                }

                // Запускаем получение сообщений
                _ = Task.Run(() => ReceiveMessagesAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Disconnect();
                throw new InvalidOperationException("Failed to connect to server.", ex);
            }
        }

        public async Task RequestQuoteAsync()
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to the server.");

            await _writer.WriteLineAsync("QUOTE");
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
            catch
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
