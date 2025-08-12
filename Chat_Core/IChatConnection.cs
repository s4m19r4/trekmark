using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Core
{
    public interface IChatConnection
    {
        bool IsConnected { get; } //статус подключения
        bool IsRunning { get; } //запущен/не запущен

        event Action Connected; //подключение установлено
        event Action Disconnected; //соединение закрыто
        event Action<ChatMessage> MessageReceived; //событие при получении сообщения
        event Action<Exception> ErrorOccurred; //ошибка
        public event Action Running;
        public event Action Stopped;

        Task ConnectAsync(); // подключиться

        Task DisconnectAsync(); //отключиться
        Task SendMessageAsync(ChatMessage message); //отправить сообщение

    }
}
