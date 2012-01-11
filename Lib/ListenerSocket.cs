using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Lib
{
    public sealed class ListenerSocket : IDisposable
    {
        class State
        {
            public byte[] buffer = new byte[bufferSize];
            public Socket socket;
            public StringBuilder sBuilder;
        }

        static int bufferSize = 256;
        
        Socket listener;
        IPAddress ip;
        int port;
        ISocketMessageDispatcher dispatcher;

        public bool listening = false;

        public ListenerSocket(string _ip, int _port, ISocketMessageDispatcher _dispatcher)
        {
            if (!IPAddress.TryParse(_ip, out ip))
                throw new FormatException("Bad format for ip argument");

            port = _port;
            dispatcher = _dispatcher;
        }

        public void Listen()
        {
            if (listening)
                throw new Exception("Socket is already listening");

            listening = true;
            InitListener();
            listener.BeginAccept(Accept, listener);
        }

        void InitListener()
        {
            listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.IP);// ip.AddressFamily == AddressFamily.InterNetwork ? ProtocolType.IPv4 : ProtocolType.IPv6);
            if(!listener.IsBound)
                listener.Bind(new IPEndPoint(ip, port));
            
            listener.Listen(100);
        }

        void Accept(IAsyncResult result)
        {
            Socket _listenerSocket = result.AsyncState as Socket;
            if (_listenerSocket == null)
            {
                Close();
                return;
            }

            byte[] buffer = new byte[bufferSize];

            try
            {
                Socket listenedSocket = _listenerSocket.EndAccept(result);
                listenedSocket.BeginReceive(buffer, 0, bufferSize, SocketFlags.None, Receive,
                    new State() { buffer = buffer, socket = listenedSocket, sBuilder = new StringBuilder() });
            }
            catch 
            {
                InitListener();
            }
            listener.BeginAccept(Accept, listener);
        }

        void Receive(IAsyncResult result)
        {
            SocketError error;
            State state = result.AsyncState as State;
            int count = state.socket.EndReceive(result, out error);
            if ( count == 0)
            {
                Dispatch(state);
                Close();
                return;
            }
            else
            {
                state.sBuilder.Append(Encoding.ASCII.GetString(state.buffer, 0, count));
                if (state.sBuilder.ToString().EndsWith("\r\n"))
                    Dispatch(state);
                Array.Clear(state.buffer, 0, state.buffer.Length);
            }

            state.socket.BeginReceive(state.buffer, 0, bufferSize, SocketFlags.None, Receive, state);
        }

        void Dispatch(State state)
        {
            if (state.socket.Connected && state.sBuilder.Length > 0)
            {
                IEnumerable<string> messages = state.sBuilder.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None).Distinct();
                foreach (string s in messages)
                {
                    string response = dispatcher.Action(s);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        state.socket.Send(Encoding.ASCII.GetBytes(response));
                        state.sBuilder.Clear();
                    }
                }
            }
        }

        void Close()
        {
            listening = false;
            listener.Close();
            listener.Dispose();
        }

        public void Stop()
        {
            listening = false;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
