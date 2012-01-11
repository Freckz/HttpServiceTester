namespace CreateASPNETThreadCounters
{
    using System;
    using System.Diagnostics;

    class CustomAspNetThreadCounters
    {
        [STAThread]
        static void Main(string[] args)
        {
            CreateCounters();
            Console.WriteLine("CustomAspNetThreadCounters performance counter category is created. [Press Enter]");
            Console.ReadLine();
        }

        public static void CreateCounters()
        {
            CounterCreationDataCollection col =
              new CounterCreationDataCollection();

            // Create custom counter objects
            CounterCreationData counter1 = new CounterCreationData();
            counter1.CounterName = "Available Worker Threads";
            counter1.CounterHelp = "The difference between the maximum number " +
                                   "of thread pool worker threads and the " +
                                   "number currently active.";
            counter1.CounterType = PerformanceCounterType.NumberOfItems32;

            CounterCreationData counter2 = new CounterCreationData();
            counter2.CounterName = "Available IO Threads";
            counter2.CounterHelp = "The difference between the maximum number of " +
                                   "thread pool IO threads and the number " +
                                   "currently active.";
            counter2.CounterType = PerformanceCounterType.NumberOfItems32;

            CounterCreationData counter3 = new CounterCreationData();
            counter3.CounterName = "Max Worker Threads";
            counter3.CounterHelp = "The number of requests to the thread pool " +
                                   "that can be active concurrently. All " +
                                   "requests above that number remain queued until " +
                                   "thread pool worker threads become available.";
            counter3.CounterType = PerformanceCounterType.NumberOfItems32;

            CounterCreationData counter4 = new CounterCreationData();
            counter4.CounterName = "Max IO Threads";
            counter4.CounterHelp = "The number of requests to the thread pool " +
                                   "that can be active concurrently. All " +
                                   "requests above that number remain queued until " +
                                   "thread pool IO threads become available.";
            counter4.CounterType = PerformanceCounterType.NumberOfItems32;

            // Add custom counter objects to CounterCreationDataCollection.
            col.Add(counter1);
            col.Add(counter2);
            col.Add(counter3);
            col.Add(counter4);
            // delete the category if it already exists
            if (PerformanceCounterCategory.Exists("CustomAspNetThreadCounters"))
            {
                PerformanceCounterCategory.Delete("CustomAspNetThreadCounters");
            }
            // bind the counters to the PerformanceCounterCategory
            PerformanceCounterCategory category =
                    PerformanceCounterCategory.Create("CustomAspNetThreadCounters",
                                                      "", col);
        }
    }
}
