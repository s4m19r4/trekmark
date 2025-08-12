using System;
using System.Threading.Tasks;
using Chat_Core;

namespace Console_chat
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск сервера чата на порту 5000...");
            int counter = 0;

            // создаем протокол (JSON)
            IMessageProtocol protocol = new JsonMessageSerializer();

            // создаем сервер
            var server = new ConnectionManager(5000, "server_user", true, protocol);

            // подписываемся на события
            server.Connected += () => Console.WriteLine("Клиент подключился");
            server.Disconnected += () => Console.WriteLine("Клиент отключился");
            server.MessageReceived += (msg) =>
            {
               // Console.WriteLine($"[Сообщение от клиента]:{msg}");
               // Console.WriteLine($"Тип: {msg.Type}");
                Console.WriteLine($"Пользователь: {msg.User}");
                Console.WriteLine($"Текст: {msg.Text}");               
                Console.WriteLine($"ID: {msg.Id}");
                Console.WriteLine($"Время: {msg.Timestamp}");

                counter++;

                // Эхо-ответ                

                _ = server.SendMessageAsync(new ChatMessage
                {
                    User = server.username,
                    Text = $"***** Сервер получил {counter} сообщений.",
                    Timestamp = DateTime.Now,
                    Id = Guid.Empty,
                    Type =  MessageType.Message
                });
            };
            server.ErrorOccurred += (ex) => Console.WriteLine($"Ошибка: {ex.Message}");

            // запускаем сервер
            await server.ConnectAsync();

            Console.WriteLine("Сервер запущен. Нажмите Enter для выхода.");
            Console.ReadLine();

            await server.DisconnectAsync();
        }
    }
}
