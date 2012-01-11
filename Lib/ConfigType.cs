using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib
{
    public enum ConfigType
    {
        MaxConnections,
        MaxWorkerThreads,
        MaxIOThreads,
        AvailableWorkerThreads,
        AvailableIOThreads,
        RequestQueueLimit,
        ParallelDistantRequestValue,
        MinWorkerThreads,
        MinIOThreads,
    }
}
