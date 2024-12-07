using System.Text;

namespace TopNetwork.Core
{
    public class Message
    {
        //к примеру Information, Error...
        public string MessageType { get; set; } = string.Empty; 
        public Dictionary<string, string> Headers { get; set; } = [];
        public string Payload { get; set; } = string.Empty;

        public byte[] ToBytes()
        {
            using MemoryStream memoryStream = new();
            using BinaryWriter writer = new(memoryStream, Encoding.UTF8, leaveOpen: true);

            // Записываем тип сообщения
            writer.Write(MessageType);

            // Упаковка заголовков
            writer.Write(Headers.Count); // Сначала записываем количество заголовков
            foreach (var header in Headers)
            {
                writer.Write(header.Key);   // Записываем ключ заголовка
                writer.Write(header.Value); // Записываем значение заголовка
            }

            // Упаковка тела
            byte[] payloadBytes = Encoding.UTF8.GetBytes(Payload);
            writer.Write(payloadBytes.Length); // Записываем длину тела
            writer.Write(payloadBytes);        // Записываем само тело

            // Итоговые данные
            byte[] messageBytes = memoryStream.ToArray();

            // Добавляем длину всего сообщения в начало
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            using MemoryStream finalStream = new();
            finalStream.Write(lengthBytes, 0, lengthBytes.Length); // Пишем длину сообщения
            finalStream.Write(messageBytes, 0, messageBytes.Length); // Пишем само сообщение

            return finalStream.ToArray();
        }

        public static Message FromBytes(byte[] data)
        {
            if (data.Length < 4)
                throw new ArgumentException("Недостаточно данных для извлечения сообщения.");

            // Извлечение длины сообщения
            byte[] lengthBytes = data[0..4];
            int messageLength = BitConverter.ToInt32(lengthBytes, 0);

            // Проверяем длину
            if (messageLength != data.Length - 4)
                throw new ArgumentException($"Длина сообщения не совпадает с указанной. Указано: {messageLength}, а всего: {data.Length - 4}");

            using MemoryStream memoryStream = new(data, 4, messageLength);
            using BinaryReader reader = new(memoryStream, Encoding.UTF8);

            var message = new Message();

            // Распаковка типа сообщения
            message.MessageType = reader.ReadString();

            // Распаковка заголовков
            int headersCount = reader.ReadInt32();
            for (int i = 0; i < headersCount; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                message.Headers[key] = value;
            }

            // Распаковка тела
            int payloadLength = reader.ReadInt32();
            if (payloadLength > 0)
            {
                byte[] payloadBytes = reader.ReadBytes(payloadLength);
                message.Payload = Encoding.UTF8.GetString(payloadBytes);
            }

            return message;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Clear();

            if(!string.IsNullOrWhiteSpace(MessageType))
                sb.AppendLine($"MessageType: {MessageType}");

            if (Headers != null && Headers.Count > 0)
            {
                sb.AppendLine("Headers\n{");
                foreach (var header in Headers)
                {
                    sb.AppendLine($"    {header.Key}: {header.Value}");
                }
                sb.AppendLine("}");
            }

            if(!string.IsNullOrWhiteSpace(Payload))
                sb.AppendLine($"Payload: {Payload}\n");

            string result = sb.ToString();

            if(string.IsNullOrWhiteSpace(result))
                result = "Пустое сообщение.";

            return result;
        }
    }
}
