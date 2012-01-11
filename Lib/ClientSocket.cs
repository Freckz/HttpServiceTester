using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Lib
{
    public class ClientSocket : ISocketEventSubscriber, IDisposable
    {
        const string KEYWORD_GET = "GET";
        const string KEYWORD_SET = "SET";
        const string KEYWORD_PING = "PING";
        const int MAX_CONNECTIONS_ATTEMPTS = 3;

        Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        ILogger _logger;
        Timer timer;
        int currentConnectionsAttempts = 0;
        object lockConnection = new object();
        bool manuallyDisconnected = false;

        private Queue<string> messagesQueue = new Queue<string>();

        private IPAddress _ip;
        public IPAddress Ip
        {
            get { return _ip; }
            private set { _ip = value; }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            private set { _port = value; }
        }

        public bool IsConnected
        {
            get { return Ping(); }
        }

        public ClientSocket(ILogger logger)
        {
            _logger = logger;
        }

        public bool Connect(string ip, string port)
        {
            if(!IPAddress.TryParse(ip, out _ip))
            {
                _logger.Log(string.Format("Ip format {0} incorrect", ip), true);
                return false;
            }
            
            if(!Int32.TryParse(port, out _port))
            {
                _logger.Log(string.Format("Port format {0} incorrect", port), true);
                return false;
            }

            return Connect();
        }

        bool Connect()
        {
            manuallyDisconnected = false;
            currentConnectionsAttempts = 0;
            while (currentConnectionsAttempts < 3)
            {
                try
                {
                    _socket.Connect(new IPEndPoint(Ip, Port));

                    currentConnectionsAttempts = 0;
                    if (SocketConnected != null)
                        SocketConnected.Invoke(this, new SocketResponseEventArgs("connected"));
                    _logger.Log("Socket connected", false);
                    return true;
                }
                catch (SocketException socketExc)
                {
                    if (socketExc.SocketErrorCode == SocketError.IsConnected)
                    {
                        _logger.Log("Socket connected", false);
                        return true;
                    }

                    _logger.Log(string.Format("Socket error during connection to {0}:{1}", Ip, Port), true);
                    _logger.Log(socketExc.ToString(), false);
                    currentConnectionsAttempts++;
                    if (currentConnectionsAttempts < 3)
                        Thread.Sleep(1000);
                }
                catch (InvalidOperationException disconnectedExc)
                {
                    _socket.Dispose();
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
                catch (Exception exc)
                {
                    _logger.Log("Error during connection", true);
                    _logger.Log(exc.ToString(), false);
                    currentConnectionsAttempts++;
                }
            }

            return currentConnectionsAttempts < 3;
        }

        public bool Disconnect()
        {
            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
                manuallyDisconnected = true;
                _socket.Shutdown(SocketShutdown.Both);
                return true;
            }
            catch
            {
                return !Ping();
            }
        }

        bool TryReconnect(SocketException socketExc, Action actionIfReconnect)
        {

            if (socketExc.SocketErrorCode == SocketError.ConnectionAborted
                || socketExc.SocketErrorCode == SocketError.ConnectionReset
                || socketExc.SocketErrorCode == SocketError.Disconnecting
                || socketExc.SocketErrorCode == SocketError.NotConnected
                || socketExc.SocketErrorCode == SocketError.OperationAborted)
            {
                if (Connect())
                {
                    try
                    {
                        actionIfReconnect();
                    }
                    catch (SocketException sexc)
                    {
                        _logger.Log(string.Format("Socket error during executing '{0}'", actionIfReconnect.Method.Name), true);
                        _logger.Log(sexc.ToString(), false);
                        return false;
                    }
                    catch (Exception exc)
                    {
                        _logger.Log("Error during connection", true);
                        _logger.Log(exc.ToString(), false);
                        return false;
                    }

                    return true;
                }
            }
            return false;
        }

        public void Start()
        {
            timer = new Timer(new TimerCallback(TickTimer), null, 0, 1000);
        }

        bool Ping()
        {
            try
            {
                _socket.Send(PrepareSocketMessage(KEYWORD_PING));
                return true;
            }
            catch
            {
                return false;
            }
        }

        void Send(string message)
        {
            if (manuallyDisconnected)
                return;

            try
            {
                _socket.Send( PrepareSocketMessage(message));
            }
            catch (SocketException socketExc)
            {
                _logger.Log(string.Format("Socket error during sending '{0}' to {1}:{2}", KEYWORD_GET, Ip, Port), true);
                _logger.Log(socketExc.ToString(), false);
                _logger.Log("Trying to reconnect...", false);
                if (SocketDisconnected != null)
                    SocketDisconnected.Invoke(this, new SocketResponseEventArgs("disconnected"));
                TryReconnect(socketExc, new Action(() => _socket.Send(PrepareSocketMessage(KEYWORD_GET))));
            }
            catch (Exception exc)
            {
                _logger.Log("Error during connection", true);
                _logger.Log(exc.ToString(), false);
                if (SocketDisconnected != null)
                    SocketDisconnected.Invoke(this, new SocketResponseEventArgs("disconnected"));
            }
        }

        void TickTimer(object state)
        {
            try
            {
                string msg = string.Empty;
                if (messagesQueue.Count == 0)
                {
                    Send(KEYWORD_GET);
                }
                else
                {
                    List<string> messages;
                    lock (messagesQueue)
                    {
                        messages = new List<string>(messagesQueue.Count);
                        while (messagesQueue.Count > 0)
                            messages.Add(messagesQueue.Dequeue());
                    }

                    string agMessage = messages.Aggregate((m, next) => m += "!" + next);

                    Send(agMessage);
                }

                //Attend 40 * 50ms = 2s une réponse du serveur qui est configuré pour renvoyer tout le temps quelque chose
                int waitingCoeff = 40;
                while (_socket.Available == 0 && waitingCoeff > 0)
                {
                    Thread.Sleep(50);
                    waitingCoeff--;
                }

                if (_socket.Available > 0)
                {
                    byte[] buffer = new byte[256];
                    string text = string.Empty;
                    int dataLenght = 0;

                    while (dataLenght < _socket.Available)
                    {
                        try
                        {
                            dataLenght += _socket.Receive(buffer, SocketFlags.None);
                        }
                        catch (SocketException sexc)
                        {
                            _logger.Log("Socket error during receiving", true);
                            _logger.Log(sexc.ToString(), false);
                            
                            if (!_socket.Connected && !manuallyDisconnected)
                            {
                                _logger.Log("Trying to reconnect..", false);
                                TryReconnect(sexc, () => { });
                            }
                            return;
                        }
                        catch (Exception exc)
                        {
                            _logger.Log("Error during connection", true);
                            _logger.Log(exc.ToString(), false);
                            if (SocketDisconnected != null)
                                SocketDisconnected.Invoke(this, new SocketResponseEventArgs("disconnected"));
                            return;
                        }

                        text += Encoding.ASCII.GetString(buffer, 0, dataLenght % 256);
                    }

                    if (SocketResponseReceived != null)
                        SocketResponseReceived.Invoke(this, new SocketResponseEventArgs(text));
                    
                }
            }
            catch (Exception e)
            {
                _logger.Log(e.ToString(), true);
            }
        }

        public void EnqueueMessage(string message)
        {
            messagesQueue.Enqueue(message);
        }

        byte[] PrepareSocketMessage(string message)
        {
            string endedMessage = message.EndsWith("\r\n") ? message : message + "\r\n";
            return Encoding.ASCII.GetBytes(endedMessage);
        }

        public event Action<ClientSocket, SocketResponseEventArgs> SocketResponseReceived;
        public event Action<ClientSocket, SocketResponseEventArgs> SocketDisconnected;
        public event Action<ClientSocket, SocketResponseEventArgs> SocketConnected;

        public void Dispose()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
            }
        }
    }
}
