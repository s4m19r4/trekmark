using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Chat_Core;

namespace Console_chat_client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск клиента чата...");
            
            // создаем протокол (JSON)
            IMessageProtocol protocol = new JsonMessageSerializer();

            
            var client = new ConnectionManager("127.0.0.1", 5000, "client_user", false, protocol);

            // подписываемся на события
            client.Connected += () => Console.WriteLine("Клиент подключился");
            client.Disconnected += () => Console.WriteLine("Клиент отключился");
            client.MessageReceived += (msg) =>
            {
                Console.WriteLine("[Сообщение от СЕРВЕРА]");
                Console.WriteLine($"Тип: {msg.Type}");
                Console.WriteLine($"Пользователь: {msg.User}");
                Console.WriteLine($"Текст: {msg.Text}");               
                Console.WriteLine($"ID: {msg.Id}");
                Console.WriteLine($"Время: {msg.Timestamp}");                
            };
            client.ErrorOccurred += (ex) => Console.WriteLine($"Ошибка: {ex.Message}");

            try
            {
                await client.ConnectAsync();

                Console.WriteLine("Введите сообщения для отправки (пустая строка для выхода):");

                while (true)
                {
                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                        break;

                   

                    var message = new ChatMessage
                    {
                        User = client.username,
                        Text = input,
                        Timestamp = DateTime.Now,
                        Id = Guid.Empty,
                        Type = MessageType.Message                       
                    };

                    await client.SendMessageAsync(message);
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при подключении или работе: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync();
                Console.WriteLine("Клиент завершил работу.");
            }
        }


    }
}
