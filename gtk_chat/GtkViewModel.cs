using Chat_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using MessageType = Chat_Core.MessageType;
using System.Net;
using GLib;
using Application = Gtk.Application;


namespace gtk_chat
{
    public class GtkViewModel
    {
        // виджеты окна
        Builder builder;

        private Window mainWindow;

        private Grid outerGrid;

        private Box headerBox;
        private CheckButton buttonIsServer;
        private Label labelIp;
        private Entry entryIp;
        private Label labelUsername;
        private Entry entryUsername;
        private Button buttonConnect;

        private Box footerBox;
        private Entry entryInputMessage;
        private Button buttonSend;
        private Label labelStatus;

        private ScrolledWindow scrollContainerChat;
        private TextView textViewChat;


        //параметры подключения
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

        public GtkViewModel(Builder b)
        {
            builder = b;
            protocol = new JsonMessageSerializer();
            currentConnection = new ConnectionManager(_port, _userName, _isServerMode, protocol);

            // получаем виджеты
            mainWindow = (Window)builder.GetObject("main_window");

            outerGrid = (Grid)builder.GetObject("outer_grid");

            headerBox = (Box)builder.GetObject("header_box");
            buttonIsServer = (CheckButton)builder.GetObject("button_isServer");
            labelIp = (Label)builder.GetObject("label_ip");
            entryIp = (Entry)builder.GetObject("entry_ip");
            labelUsername = (Label)builder.GetObject("label_username");
            entryUsername = (Entry)builder.GetObject("entry_username");
            buttonConnect = (Button)builder.GetObject("button_connect");

            footerBox = (Box)builder.GetObject("footer_box");
            entryInputMessage = (Entry)builder.GetObject("entry_inputMessage");
            buttonSend = (Button)builder.GetObject("button_send");
            labelStatus = (Label)builder.GetObject("label_status");

            scrollContainerChat = (ScrolledWindow)builder.GetObject("scroll_container_chat");
            textViewChat = (TextView)builder.GetObject("textView_chat");

            //подписка на события
            entryUsername.Changed += OnEntryUsernameChanged;
            entryIp.Changed += OnEntryIpChanged;
            entryInputMessage.Changed += OnEntryInputChanged;

            buttonIsServer.Toggled += OnButtonIsServerToggled;
            buttonConnect.Clicked += OnButtonConnectClicked;
            buttonSend.Clicked += OnButtonSendClicked;

            Update_ButtonConnect_Sensitive();
            Update_ButtonConnect_Text();
            Update_ButtonSend_Sensitive();
            Update_LabelStatus_Text();
            Update_ButtonIsServer_Sensitive();

            Set_LebelStatus_Style();

        }


        private void OnEntryUsernameChanged(object? sender, EventArgs e)
        {
            Update_ButtonConnect_Sensitive();

            if (entryUsername.Text is not null)
            {
                UserName = entryUsername.Text;
            }

        }

        private void OnEntryInputChanged(object? sender, EventArgs e)
        {
                       
            OutgoingMessage = entryInputMessage.Text;
            Update_ButtonSend_Sensitive();

        }

        private void OnEntryIpChanged(object? sender, EventArgs e)
        {
            ServerIp = entryIp.Text;
        }


        private void OnButtonIsServerToggled(object? sender, EventArgs e)
        {
            IsServerMode = buttonIsServer.Active;
            Update_EnterIp_Sensitive();
        }

        private void OnButtonConnectClicked(object? sender, EventArgs e)
        {
            ConnectOrDisconnect();
        }

        private void OnButtonSendClicked(object? sender, EventArgs e)
        {
            SendMessage();
        }

        private void AppendMessage(string message)
        {
            Application.Invoke(delegate
            {
                var buffer = textViewChat.Buffer;

                // Добавляем новое сообщение с новой строки
                if (!string.IsNullOrEmpty(buffer.Text))
                    buffer.Text += "\n" + message;
                else
                    buffer.Text = message;

                // Прокручиваем вниз
                var endIter = buffer.EndIter;
                textViewChat.ScrollToIter(endIter, 0, false, 0, 0);
            });

        }



        private void Set_LebelStatus_Style()
        {
            Application.Invoke(delegate
            {
                var context = labelStatus.StyleContext;
                var cssProvider = new Gtk.CssProvider();
                cssProvider.LoadFromData("label { font-weight: bold; color: green; }");
                context.AddProvider(cssProvider, Gtk.StyleProviderPriority.User);
            });
        }

        private void Update_ButtonIsServer_Sensitive()
        {            
            Application.Invoke(delegate
                 {
                     buttonIsServer.Sensitive = !IsRunning;
                 });
        }

        private void Update_EnterIp_Sensitive()
        {
            Application.Invoke(delegate
            {
                entryIp.Sensitive = !IsServerMode;
            });
        }

        private void Update_LabelStatus_Text()
        {
            if (IsServerMode)
            {
                ConnectionStatus = _isRunning ? "Сервер: Запущен" : "Сервер: Остановлен";
            }
            else
            {
                ConnectionStatus = _isRunning ? "Клиент: Запущен" : "Клиент: Остановлен";
            }

            Application.Invoke(delegate
            {
                labelStatus.Text = ConnectionStatus;
            });
        }
        private void Update_ButtonConnect_Text()
        {
            if (IsServerMode)
            {
                
                ConnectButtonText = _isRunning ? "Отключить" : "Включить";
            }
            else
            {
                ConnectButtonText = _isConnected ? "Отключиться" : "Подключиться";
            }

            Application.Invoke(delegate
            {
                buttonConnect.Label = ConnectButtonText;
            });
        }

        private void Update_ButtonSend_Sensitive()
        {           
                Application.Invoke(delegate
                {
                    buttonSend.Sensitive = IsConnected && !string.IsNullOrWhiteSpace(OutgoingMessage);
                });            
        }

        private void Update_ButtonConnect_Sensitive()
        {

            if (!string.IsNullOrWhiteSpace(entryUsername.Text) && IsValidIpAddress(ServerIp))
            {
                Application.Invoke(delegate
                {
                    buttonConnect.Sensitive = true;
                });
            }
            else
            {
                Application.Invoke(delegate
                {
                    buttonConnect.Sensitive = false;
                });
            }
        }

        private bool IsValidIpAddress(string input)
        {
            bool parseResult = IPAddress.TryParse(input, out IPAddress? address);

            if (!parseResult)
            {
                return false;
            }

            // Проверяем, что адрес IPv4
            bool isIPv4 = address?.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

            return isIPv4;
        }

        //***************************************
        //свойства
        public string ServerIp
        {
            get => _serverIp;
            set
            {
                if (_serverIp != value)
                {
                    _serverIp = value;
                    Update_ButtonConnect_Sensitive();

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
                    Update_LabelStatus_Text();
                    Update_EnterIp_Sensitive();
                    Update_ButtonConnect_Text();
                }
            }
        }



        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
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

                    if (IsServerMode)
                    {
                        if (_isConnected) AddChatMessage(new ChatMessage("Клиент подключился"));
                        else AddChatMessage(new ChatMessage("Клиент отключился"));
                    }
                    else
                    {
                        if (_isConnected)
                        {
                            AddChatMessage(new ChatMessage("Подключение к серверу установлено"));
                            IsRunning = true;
                        }
                        else
                        {
                            AddChatMessage(new ChatMessage("Подключение к серверу закрыто"));
                            IsRunning = false;
                        }
                    }

                    Update_ButtonSend_Sensitive();
                    Update_ButtonConnect_Text();

                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;

                    Update_ButtonConnect_Text();
                    Update_LabelStatus_Text();
                    Update_ButtonSend_Sensitive();
                    Update_ButtonIsServer_Sensitive();

                    var conn = _isServerMode ? "Сервер" : "Клиент";
                    var status = _isRunning ? " запущен." : " остановлен.";
                    AddChatMessage(new ChatMessage(conn + status));
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
                    //OnPropertyChanged();
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
                    //AppendMessage(_chatLog);
                }
            }
        }


        public string OutgoingMessage
        {
            get => _outgoingMessage;
            set
            {
                if (_outgoingMessage != value )
                {
                    _outgoingMessage = value;
                    Update_ButtonSend_Sensitive();
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

                }
            }
        }

        


        private async void ConnectOrDisconnect()
        {
            if (!IsRunning)
            {

                // создаем подключение
                if (_isServerMode)
                {
                    currentConnection = new ConnectionManager(_port, _userName, _isServerMode, protocol);
                }
                else
                {
                    currentConnection = new ConnectionManager(_serverIp, _port, _userName, _isServerMode, protocol);
                }

                // подписываемся на события
                currentConnection.Running += RunningHandler;
                currentConnection.Stopped += StoppedHandler;
                currentConnection.Connected += ConnectedHandler;
                currentConnection.Disconnected += DisconnectedHandler;
                currentConnection.MessageReceived += MessageReceivedHandler;
                currentConnection.ErrorOccurred += ErrorOccurredHandler;

                // запускаем соединение
                await currentConnection.ConnectAsync();
            }
            else
            {
                try
                {
                    IsConnected = false;
                    IsRunning = false;

                    // Отписка от событий
                    currentConnection.Running -= RunningHandler;
                    currentConnection.Stopped -= StoppedHandler;
                    currentConnection.Connected -= ConnectedHandler;
                    currentConnection.Disconnected -= DisconnectedHandler;
                    currentConnection.MessageReceived -= MessageReceivedHandler;
                    currentConnection.ErrorOccurred -= ErrorOccurredHandler;

                    await currentConnection.DisconnectAsync();

                    
                }
                catch (Exception ex)
                {
                    AddChatMessage(new ChatMessage(MessageType.Error, $"Ошибка при отключении: {ex.Message}"));
                }
            }

            // ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            //  ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
        }


        private async void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(entryInputMessage.Text) || string.IsNullOrEmpty(entryInputMessage.Text)) return;

            OutgoingMessage = entryInputMessage.Text;
            var message = new ChatMessage(MessageType.Message, _userName, OutgoingMessage);
            

            if (IsConnected && IsRunning )
            {
                await currentConnection.SendMessageAsync(message);
            }

            AddChatMessage(message);
        }

        private void AddChatMessage(ChatMessage message)
        {
            bool ShowExceptions = true;

            if (message.Type == MessageType.Error)
            {
                if (ShowExceptions)
                {
                    ChatLog += message + Environment.NewLine;
                    AppendMessage(message.ToString());


                }
                else
                {
                    //игнор
                }
            }
            else
            {
                ChatLog += message + Environment.NewLine;
                AppendMessage(message.ToString());
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

    }
}
