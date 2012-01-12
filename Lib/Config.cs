using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib
{
    public static class Config
    {
        private static int _parallelRequests = 4;
        private static object lockParallelRequests = new object();
        public static int ParallelRequests
        {
            get
            {
                return _parallelRequests;
            }
            set
            {
                 _parallelRequests = value;
            }
        }

        public static int Timeout { get; set; }
        public static int ReadWriteTimeout { get; set; }
    }
}
