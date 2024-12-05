using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public class DeliveryServis
    {
        public static async Task<string> ReadAsync(NetworkStream stream, CancellationToken token)
        {
            //Узнаём размер входящего сообщения
            var size = new byte[4];
            await stream.ReadExactlyAsync(size, token);
            //Считываем сообщение
            var buffer = new byte[BitConverter.ToInt32(size)];
            await stream.ReadAsync(buffer, 0, buffer.Length, token);

            return Encoding.UTF8.GetString(buffer);
        }
        public static async Task<string> ReadAsync(NetworkStream stream)
        {
            //Узнаём размер входящего сообщения
            var size = new byte[4];
            await stream.ReadExactlyAsync(size);
            //Считываем сообщение
            var buffer = new byte[BitConverter.ToInt32(size)];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

        public static async Task WriteAsync(NetworkStream stream, string message, CancellationToken token)
        {
            var bytesMessage = Encoding.UTF8.GetBytes(message);
            var size = BitConverter.GetBytes(bytesMessage.Length);
            //Отправляем размер сообщения
            await stream.WriteAsync(size, token);
            //Отправляем сообщение
            await stream.WriteAsync(bytesMessage, token);
        }
        public static async Task WriteAsync(NetworkStream stream, string message)
        {
            var bytesMessage = Encoding.UTF8.GetBytes(message);
            var size = BitConverter.GetBytes(bytesMessage.Length);
            //Отправляем размер сообщения
            await stream.WriteAsync(size);
            //Отправляем сообщение
            await stream.WriteAsync(bytesMessage);
        }
    }
}
