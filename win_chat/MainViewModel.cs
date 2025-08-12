using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Chat_Core;


namespace win_chat
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private string _serverIp = "127.0.0.1";
        private bool _isServerMode = false;
        private string _userName = "";
        private bool _isConnected = false;
        private bool _isRunning = false;
        private string _connectButtonText = "Подключиться";
        private string _chatLog = "";
        private string _outgoingMessage = "";
        private string _connectionStatus = "Остановлен";

        private int _port = 5000;
        int counter = 0;

        ConnectionManager currentConnection;
        IMessageProtocol protocol;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(ConnectOrDisconnect, CanConnectOrDisconnect);
            SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);
        }

        public string ServerIp
        {
            get => _serverIp;
            set
            {
                if (_serverIp != value)
                {
                    _serverIp = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsServerMode
        {
            get => _isServerMode;
            set
            {
                if (_isServerMode != value)
                {
                    _isServerMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsIpEnabled));
                }
            }
        }

        public bool IsIpEnabled => !IsServerMode;

        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();

                    if (IsServerMode)
                    {
                        if (_isConnected) AddChatMessage(new ChatMessage("Клиент подключился"));
                        else AddChatMessage(new ChatMessage("Клиент отключился"));
                    }
                    else
                    {
                        if (_isConnected) AddChatMessage(new ChatMessage("Подключение к серверу установлено"));
                        else AddChatMessage(new ChatMessage("Подключение к серверу закрыто"));
                    }


                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning != value )
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConnectionStatus));

                    if (IsServerMode)
                    {
                        ConnectButtonText = _isRunning ? "Отключиться" : "Подключиться";
                        ConnectionStatus = _isRunning ? "Сервер: Запущен" : "Сервер: Остановлен";
                    }
                    else 
                    {
                        ConnectButtonText = _isRunning ? "Отключиться" : "Подключиться";
                        ConnectionStatus = _isRunning ? "Клиент: Запущен" : "Клиент: Остановлен";
                    }
                }
            }
        }

        public string ConnectButtonText
        {
            get => _connectButtonText;
            private set
            {
                if (_connectButtonText != value)
                {
                    _connectButtonText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ChatLog
        {
            get => _chatLog;
            private set
            {
                if (_chatLog != value)
                {
                    _chatLog = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OutgoingMessage
        {
            get => _outgoingMessage;
            set
            {
                if (_outgoingMessage != value)
                {
                    _outgoingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }


        // Команды 
        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        private async void ConnectOrDisconnect(object parameter)
        {
            if (!IsRunning)
            {
                // создаем протокол (JSON)
                protocol = new JsonMessageSerializer();

                // создаем подключение
                if (_isServerMode)
                {
                    currentConnection = new ConnectionManager(_port, _userName, _isServerMode, protocol);

                    // подписываемся на события
                    currentConnection.Running += RunningHandler;
                    currentConnection.Stopped += StoppedHandler;
                    currentConnection.Connected += ConnectedHandler;
                    currentConnection.Disconnected += DisconnectedHandler;
                    currentConnection.MessageReceived += MessageReceivedHandler;
                    currentConnection.ErrorOccurred += ErrorOccurredHandler;

                    // запускаем сервер
                    await currentConnection.ConnectAsync();
                    AddChatMessage(new ChatMessage(_userName, "Сервер запущен."));
                }
                else
                {
                    currentConnection = new ConnectionManager(_serverIp, _port, _userName, _isServerMode, protocol);

                    currentConnection.Running += RunningHandler;
                    currentConnection.Stopped += StoppedHandler;
                    currentConnection.Connected += ConnectedHandler;
                    currentConnection.Disconnected += DisconnectedHandler;
                    currentConnection.MessageReceived += MessageReceivedHandler;
                    currentConnection.ErrorOccurred += ErrorOccurredHandler;

                    /*
                    // подписываемся на события
                    currentConnection.Running += () => IsRunning = true;
                    currentConnection.Stopped += () => IsRunning = false;
                    currentConnection.Connected += () =>
                    {
                        IsConnected = true;
                        //AddChatMessage(new ChatMessage("Подключение к серверу установлено"));
                    };
                    currentConnection.Disconnected += () =>
                    {
                        IsConnected = false;
                        //AddChatMessage(new ChatMessage("Подключение к серверу закрыто"));
                    };
                  
                    currentConnection.MessageReceived += (msg) => AddChatMessage(msg);
             
                    currentConnection.ErrorOccurred += (ex) => AddChatMessage(new ChatMessage($"Ошибка: {ex.Message}"));
                    */

                    // запускаем клиент
                    await currentConnection.ConnectAsync();
                    AddChatMessage(new ChatMessage("Клиент запущен."));
                }


                // IsConnected = true;
                // AddChatMessage("Система: Подключено.");
            }
            else
            {
                try
                {
                    IsConnected = false;
                    IsRunning = false;

                    /*
                    currentConnection.Running -= ()=> { };
                    currentConnection.Stopped -= () => { };
                    currentConnection.Connected -= () => { };
                    currentConnection.Disconnected -= () => { };
                    currentConnection.MessageReceived -= (msg) => { };
                    currentConnection.ErrorOccurred -= (ex) => { };
                    */
                    // Отписка от событий
                    currentConnection.Running -= RunningHandler;
                    currentConnection.Stopped -= StoppedHandler;
                    currentConnection.Connected -= ConnectedHandler;
                    currentConnection.Disconnected -= DisconnectedHandler;
                    currentConnection.MessageReceived -= MessageReceivedHandler;
                    currentConnection.ErrorOccurred -= ErrorOccurredHandler;


                    await currentConnection.DisconnectAsync();

                    var tempstr = _isServerMode ? "Сервер" : "Клиент";
                    AddChatMessage(new ChatMessage(tempstr + " остановлен."));
                }
                catch (Exception ex)
                {
                    AddChatMessage(new ChatMessage(MessageType.Error, $"Ошибка при отключении: {ex.Message}"));
                }
            }

           ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
        }

        private bool CanConnectOrDisconnect(object parameter)
        {
            if (IsConnected)
                return true;

            bool validUserName = !string.IsNullOrWhiteSpace(UserName);
            bool validIp = IsServerMode || (!string.IsNullOrWhiteSpace(ServerIp));
            return validUserName && validIp;
        }

        private async void SendMessage(object parameter)
        {
            if (string.IsNullOrWhiteSpace(OutgoingMessage)) return;

            var message = new ChatMessage(MessageType.Message, _userName, OutgoingMessage);

            if (IsConnected && IsRunning)
            {
                await currentConnection.SendMessageAsync(message);
            }

            AddChatMessage(message);
        }

        private bool CanSendMessage(object parameter)
        {
            return IsConnected && !string.IsNullOrWhiteSpace(OutgoingMessage);
        }

        private void AddChatMessage(ChatMessage message)
        {
            bool ShowExceptions = true;

            if (message.Type == MessageType.Error)
            {
                if (ShowExceptions)
                {
                    ChatLog += message + Environment.NewLine;
                }
                else
                { 
                //игнор
                }
            }
            else
            {
                ChatLog += message + Environment.NewLine;
            }

        }

        // Объявление обработчиков
        private void RunningHandler()
        {
            IsRunning = true;
        }

        private void StoppedHandler()
        {
            IsRunning = false;
        }

        private void ConnectedHandler()
        {
            IsConnected = true;
        }

        private void DisconnectedHandler()
        {
            IsConnected = false;
        }

        private void MessageReceivedHandler(ChatMessage msg)
        {
            AddChatMessage(msg);

            if (_isServerMode)
            {
                // Эхо-ответ
                counter++;
                _ = currentConnection.SendMessageAsync(
                    new ChatMessage(MessageType.Message, _userName, $"***** Сервер получил {counter} сообщений."));
            }
        }

        private void ErrorOccurredHandler(Exception ex)
        {
            AddChatMessage(new ChatMessage($"Ошибка: {ex.Message}"));
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
