using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Core
{
    public class TcpClientConnection : IChatConnection
    {

        private bool _lastPongReceived = true;
        private CancellationTokenSource _pingCts;

        // события
        public event Action Connected;
        public event Action Disconnected;
        public event Action<ChatMessage> MessageReceived;
        public event Action<Exception> ErrorOccurred;
        public event Action Running;
        public event Action Stopped;

        IMessageProtocol Protocol;
        String Ip;
        int Port;

        private bool _IsRunning = false;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        public bool IsConnected => _client?.Connected??false;

        public TcpClientConnection(string ip, int port, IMessageProtocol protocol)
        {
            Ip = ip;
            Port = port;
            Protocol = protocol;
        }

        public bool IsRunning
        {
            get
            {
                return _IsRunning;
            }
            set
            {
                _IsRunning = value;
                if (_IsRunning) Running?.Invoke();
                else Stopped?.Invoke();
            }
        }


        public async Task ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
               
                await _client.ConnectAsync(Ip, Port);
                IsRunning = true;
                //StartPingLoop(2500);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                if (IsConnected)
                {                    
                    Connected?.Invoke();
                } 

                _ = Task.Run(ListenForMessagesAsync);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally 
            {
               
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                CloseClient();
                IsRunning = false;
                Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }

        private void CloseClient()
        {
            try
            {
                StopPingLoop();

                _reader?.Dispose();
                _reader = null;

                _writer?.Dispose();
                _writer = null;

                _stream?.Dispose();
                _stream = null;

                _client?.Close();
                _client?.Dispose();
                _client = null;

                IsRunning = false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            if (IsConnected && message != null)
            {
                try
                {                   
                        string json = Protocol.Serialize(message);
                    await _writer.WriteLineAsync(json);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
        }

        private async Task ListenForMessagesAsync()
        {
            try
            {
                while (IsConnected)
                {
                    string json = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(json))
                    {
                        // Сервер закрыл соединение
                        break;
                    }

                    var message = Protocol.Deserialize(json);
                    if (message != null)
                    {
                        if (message.Text.ToLower().Contains("ping"))
                        {
                            Console.WriteLine("ping получен");
                            await SendMessageAsync(new ChatMessage ( MessageType.Ping, "pong" ));
                            Console.WriteLine("pong отправлен");
                        }
                        else if (message.Text.ToLower().Contains("pong"))
                        {
                            _lastPongReceived = true;
                            Console.WriteLine("pong получен");
                        }
                        else
                        {
                            
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
                if (IsConnected == false)
                {
                    Console.WriteLine("Соединение с сервером разорвано");
                    IsRunning = false;  
                    Disconnected?.Invoke();
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    // Игнорируем ошибку разрыва соединения — нормальное событие
                }
                else
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
            finally
            {
                DisconnectAsync();
            }
        }

        // Запуск пинга после подключения
        private void StartPingLoop(int delayVal)
        {

            _pingCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_pingCts.IsCancellationRequested)
                {

                    _lastPongReceived = false;


                    try
                    {
                        await SendMessageAsync(new ChatMessage { Type = MessageType.Ping, Text = "ping" });
                        Console.WriteLine("ping отправлен");
                    }
                    catch
                    {
                        // ошибка при отправке — разрыв
                        DisconnectAsync();

                        break;
                    }

                    await Task.Delay(delayVal); // ждём 5 секунд

                    if (_lastPongReceived == false) // Проверяем результат прошлого пинга
                    {
                        Console.WriteLine("Сервер не отвечает на пинг — отключаем.");
                        DisconnectAsync();
                        break;
                    }


                }
            }, _pingCts.Token);
        }

        private void StopPingLoop()
        {
            _pingCts?.Cancel();
            _pingCts = null;
        }
    }
}
