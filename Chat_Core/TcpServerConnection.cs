using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Core
{
    internal class TcpServerConnection : IChatConnection
    {
        private bool _IsRunning = false;
        public bool IsConnected=>_client?.Connected ?? false;

        private bool _lastPongReceived = true; // флаг, что мы получили последний PONG
        private CancellationTokenSource _pingCts;

        public event Action Running;
        public event Action Stopped;
        public event Action Connected;
        public event Action Disconnected;
        public event Action<ChatMessage> MessageReceived;
        public event Action<Exception> ErrorOccurred;

        int Port;
        IMessageProtocol _protocol;


        private TcpListener tcpListener;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;


        

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

        public TcpServerConnection(int port, IMessageProtocol protocol)
        {
            Port = port;
            _protocol = protocol;

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        }

        public async Task ConnectAsync()
        {
            try
            {
                tcpListener.Start();
                IsRunning = true;
                Console.WriteLine("Сервер запущен, ожидаем подключения...");

                while (IsRunning && tcpListener != null)  // основной цикл ожидания клиентов
                {

                    _client = await tcpListener.AcceptTcpClientAsync();
                    Connected?.Invoke();
                    StartPingLoop(2500);

                    _stream = _client.GetStream();
                    _reader = new StreamReader(_stream, Encoding.UTF8);
                    _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };


                    await ListenForMessagesAsync();
                    
                }
            }
            catch (ObjectDisposedException)
            {
                // tcpListener был остановлен извне — выход из цикла
            }
            catch (SocketException ex) 
            { 
            
            }  
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                
            }
        }

        private void CloseServer()
        {
            CloseClient();

            try
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                    tcpListener.Server?.Dispose();
                }
                tcpListener = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                IsRunning = false;
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
                _reader?.Dispose();
                _reader = null;

                _writer?.Dispose();
                _writer = null;

                _stream?.Dispose();
                _stream = null;

                //_client.Client.LingerState = new LingerOption(true, 0);
                //_client?.Client.Shutdown(SocketShutdown.Both);
                _client?.Close();
                //_client?.Dispose();
                _client = null;

            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }


        public async Task DisconnectAsync()
        {
            StopPingLoop();
            CloseServer();
            Disconnected?.Invoke();
        }



        private async Task ListenForMessagesAsync()
        {
            try
            {
                while (IsConnected && IsRunning)
                {
                    string json = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(json))
                    {
                        CloseClient();
                        Disconnected?.Invoke();
                        break;
                    }

                    ChatMessage message = _protocol.Deserialize(json);
                    if (message != null)
                    {
                        if (message.Text.ToLower().Contains("ping"))
                        {
                            Console.WriteLine("ping получен");
                            await SendMessageAsync(new ChatMessage(MessageType.Ping, "pong"));
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
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    // Игнорируем ошибку разрыва соединения
                }
                else
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
            finally
            {


            }
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            if (IsConnected && message != null)
            {
                try
                {
                    string json = _protocol.Serialize(message);
                    await _writer.WriteLineAsync(json);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
        }

        // Запуск пинга после подключения клиента
        private void StartPingLoop(int delayVal)
        {
            
            _pingCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (_pingCts!=null && !_pingCts.IsCancellationRequested)
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
                        //await DisconnectAsync();
                        CloseClient();
                        Disconnected?.Invoke();
                        break;
                    }

                    
                        await Task.Delay(delayVal); // ждём 5 секунд

                    if (_lastPongReceived == false) // Проверяем результат прошлого пинга
                    {
                        Console.WriteLine("Клиент не отвечает на пинг — отключаем.");
                        //await DisconnectAsync();
                        CloseClient();
                        Disconnected?.Invoke();
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
