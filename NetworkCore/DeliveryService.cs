
using System.Net.Sockets;

namespace TopNetwork.Core
{
    public static class DeliveryService
    {
        //private static readonly SemaphoreSlim _senderSemaphore = new(1, 1); Вроде SendMessageAsync и так потокобезопасный
        private static readonly SemaphoreSlim _accepterSemaphore = new(1, 1);

        /// <summary>
        /// Отправляет сообщение через предоставленный поток.
        /// </summary>
        /// <param name="stream">Сетевой поток.</param>
        /// <param name="msg">Сообщение для отправки.</param>
        /// <returns>Задача для отслеживания завершения отправки.</returns>
        public static async Task SendMessageAsync(NetworkStream stream, Message msg)
        {
            byte[] messageBytes = msg.ToBytes();
            //await _senderSemaphore.WaitAsync(); // Ожидание доступа к потоку
            try
            {
                // Отправляем сообщение
                await stream.WriteAsync(messageBytes.AsMemory());
                //FlushAsync - гарантия отправки данных
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при отправке сообщения.", ex);
            }
            //finally { _senderSemaphore.Release(); }
        }

        /// <summary>
        /// Принимает сообщение из предоставленного потока.
        /// </summary>
        /// <param name="stream">Сетевой поток.</param>
        /// <param name="token">Токен отмены.</param>
        /// <returns>Полученное сообщение.</returns>
        [Obsolete("Эта версия работает только с одним потокомб, при возможности используйте версию с передечей TopClient.")]
        public static async Task<Message> AcceptMessageAsync(NetworkStream stream, CancellationToken token)
        {
            await _accepterSemaphore.WaitAsync(); // Ожидание доступа к потоку
            try
            {
                // Читаем длину сообщения (4 байта)
                byte[] lengthBytes = new byte[4];
                int bytesRead = await ReadExactAsync(stream, lengthBytes, 4, token);

                int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                if (messageLength <= 0)
                    throw new InvalidOperationException("Некорректная длина сообщения.");


                // Читаем само сообщение
                byte[] messageBytes = new byte[messageLength];
                await ReadExactAsync(stream, messageBytes, messageLength, token);

                using MemoryStream finalStream = new();
                finalStream.Write(lengthBytes, 0, lengthBytes.Length);
                finalStream.Write(messageBytes, 0, messageBytes.Length);

                var msg = Message.FromBytes(finalStream.ToArray());
                return msg;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при приёме сообщения." + ex.Message, ex);
            }
            finally { _accepterSemaphore.Release(); }
        }

        /// <summary>
        /// Принимает сообщение из предоставленного клиента.
        /// </summary>
        /// <param name="client">Клиент.</param>
        /// <param name="token">Токен отмены.</param>
        /// <returns>Полученное сообщение.</returns>
        public static async Task<Message> AcceptMessageAsync(TopClient client, CancellationToken token)
        {
            await client.StreamSemaphore.WaitAsync(); // Ожидание доступа к потоку

            NetworkStream stream = client.Stream;
            try
            {
                // Читаем длину сообщения (4 байта)
                byte[] lengthBytes = new byte[4];
                int bytesRead = await ReadExactAsync(stream, lengthBytes, 4, token);

                int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                if (messageLength <= 0)
                    throw new InvalidOperationException("Некорректная длина сообщения.");


                // Читаем само сообщение
                byte[] messageBytes = new byte[messageLength];
                await ReadExactAsync(stream, messageBytes, messageLength, token);

                using MemoryStream finalStream = new();
                finalStream.Write(lengthBytes, 0, lengthBytes.Length);
                finalStream.Write(messageBytes, 0, messageBytes.Length);

                var msg = Message.FromBytes(finalStream.ToArray());
                return msg;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка при приёме сообщения." + ex.Message, ex);
            }
            finally { client.StreamSemaphore.Release(); }
        }

        /// <summary>
        /// Читает строго указанное количество байтов из потока.
        /// </summary>
        /// <param name="stream">Сетевой поток.</param>
        /// <param name="buffer">Буфер для записи данных.</param>
        /// <param name="count">Ожидаемое количество байтов.</param>
        /// <param name="token">Токен отмены.</param>
        /// <returns>Количество прочитанных байтов.</returns>
        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, count - totalBytesRead), token);

                if (bytesRead == 0)
                    throw new InvalidOperationException("Соединение закрыто до завершения чтения данных.");

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
