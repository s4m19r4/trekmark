using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Chat_Core
{
    public class ConnectionManager: IChatConnection
    {
        private IChatConnection CurrentConnection; // активное соединение

        IMessageProtocol protocol = new JsonMessageSerializer();

        public bool IsConnected => CurrentConnection?.IsConnected ?? false;
        public bool IsRunning => CurrentConnection?.IsRunning ?? false;

        bool isServerMode; // режим работы

        public string username; // имя пользователя

        string ip; //ip
        
        int port; //порт

        
        public event Action<ChatMessage> MessageReceived;
        public event Action Connected;
        public event Action Disconnected;
        public event Action<Exception> ErrorOccurred;
        public event Action Running;
        public event Action Stopped;

       
        public ConnectionManager(int port, string username, bool isServerMode, IMessageProtocol protocol)
        {
            this.port = port;
            this.username = username;
            this.isServerMode = isServerMode;
            this.protocol = protocol;
        }

        public ConnectionManager(string ip, int port, string username, bool isServerMode, IMessageProtocol protocol ) :
            this(port, username, isServerMode, protocol)
        {
            this.ip = ip;            
        }

        public async Task ConnectAsync()
        {
            if (isServerMode)
                CurrentConnection = new TcpServerConnection(port, protocol);
            else
                CurrentConnection = new TcpClientConnection(ip, port, protocol);

            SubscribeEvents();

            // await CurrentConnection.ConnectAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    await CurrentConnection.ConnectAsync();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            });
        }

        public async Task SendMessageAsync(ChatMessage msg) // создать Message и отправить его через текущее соединение
        {
            if (IsConnected)
            {
                await CurrentConnection.SendMessageAsync(msg);
            }
        }

        public async Task DisconnectAsync()
        {
            
           // if (IsConnected)
           // {
                await CurrentConnection.DisconnectAsync();
           // }
            
        }

        private void SubscribeEvents() // подписка+проброс
        {
            CurrentConnection.MessageReceived += msg => MessageReceived?.Invoke(msg);
            CurrentConnection.Connected += () => Connected?.Invoke();
            CurrentConnection.Disconnected += () => Disconnected?.Invoke();
            CurrentConnection.ErrorOccurred += ex => ErrorOccurred?.Invoke(ex);
            CurrentConnection.Running += () => Running?.Invoke();
        }

    }
}
