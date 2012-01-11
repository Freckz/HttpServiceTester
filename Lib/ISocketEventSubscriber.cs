using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib
{
    public interface ISocketEventSubscriber
    {
        event Action<ClientSocket, SocketResponseEventArgs> SocketResponseReceived;
    }
}
