class Program
{
    private static void Main()
    {
        const int n = 1000000;
        int[] data = GenerateData(n);

        Console.WriteLine("Багатопотоковий пошук моди:");
        TimeSpan multithreadedExecutionTime = MeasureExecutionTime(() =>
        {
            int mode = FindModeMultithreaded(data);
            Console.WriteLine($"Мода: {mode}");
        });
        Console.WriteLine($"Час виконання: {multithreadedExecutionTime.TotalMilliseconds} мс");

        Console.WriteLine("\nОднопотоковий пошук моди:");
        TimeSpan singleThreadedExecutionTime = MeasureExecutionTime(() =>
        {
            int mode = FindModeSingleThreaded(data);
            Console.WriteLine($"Мода: {mode}");
        });
        Console.WriteLine($"Час виконання: {singleThreadedExecutionTime.TotalMilliseconds} мс");

        double speedUp = singleThreadedExecutionTime.TotalMilliseconds / multithreadedExecutionTime.TotalMilliseconds;
        Console.WriteLine($"Прискорення: {speedUp}");

        Console.ReadLine();
    }

    private static int[] GenerateData(int length)
    {
        Random random = new();
        int[] data = new int[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = random.Next(1, 10);
        }

        return data;
    }

    private static int FindModeMultithreaded(int[] data)
    {
        Dictionary<int, int> frequency = new();
        object lockObj = new();

        int numThreads = Environment.ProcessorCount;
        int chunkSize = data.Length / numThreads;
        ManualResetEvent[] resetEvents = new ManualResetEvent[numThreads];

        for (int i = 0; i < numThreads; i++)
        {
            int startIndex = i * chunkSize;
            int endIndex = i == numThreads - 1 ? data.Length : (i + 1) * chunkSize;

            resetEvents[i] = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
            {
                Dictionary<int, int> localFrequency = new();

                for (int j = startIndex; j < endIndex; j++)
                {
                    int num = data[j];
                    if (localFrequency.ContainsKey(num))
                    {
                        localFrequency[num]++;
                    }
                    else
                    {
                        localFrequency[num] = 1;
                    }
                }

                lock (lockObj)
                {
                    foreach ((int num, int count) in localFrequency)
                    {
                        if (frequency.ContainsKey(num))
                        {
                            frequency[num] += count;
                        }
                        else
                        {
                            frequency[num] = count;
                        }
                    }
                }

                resetEvents[(int) state].Set();
            }, i);
        }

        WaitHandle.WaitAll(resetEvents);

        int maxCount = 0;
        int mode = 0;

        foreach ((int num, int count) in frequency)
        {
            if (count > maxCount)
            {
                maxCount = count;
                mode = num;
            }
        }

        return mode;
    }

    private static int FindModeSingleThreaded(int[] data)
    {
        Dictionary<int, int> frequency = new();

        foreach (int num in data)
        {
            if (frequency.ContainsKey(num))
            {
                frequency[num]++;
            }
            else
            {
                frequency[num] = 1;
            }
        }

        int maxCount = 0;
        int mode = 0;

        foreach ((int num, int count) in frequency)
        {
            if (count > maxCount)
            {
                maxCount = count;
                mode = num;
            }
        }

        return mode;
    }

    private static TimeSpan MeasureExecutionTime(Action action)
    {
        DateTime startTime = DateTime.Now;
        action.Invoke();
        DateTime endTime = DateTime.Now;
        return endTime - startTime;
    }
}