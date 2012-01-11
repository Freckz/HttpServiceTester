using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib
{
    public class SocketResponseEventArgs : EventArgs
    {
        public SocketResponseEventArgs(string message)
        {
            Message = message;
        }
        public string Message { get; set; }
    }
}
