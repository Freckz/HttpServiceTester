using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib
{
    public interface ILogger
    {
        void Log(string Message, bool isError);
    }
}
