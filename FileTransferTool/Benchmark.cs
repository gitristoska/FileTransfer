using System.Diagnostics;

namespace FileTransferTool
{
    internal class Benchmark
    {
        private const int Runs = 3;

        private readonly int[] chunkSizes =
        {
             64 * 1024 * 1024,
             16 * 1024 * 1024,
             4 * 1024 * 1024,
             1024 * 1024
        };

        private readonly int[] concurrencyLevels = { 1, 2, 4, 8, 16 };

        public void Run(string source, string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);

            string destination = Path.Combine(destinationFolder, "benchmark-copy.bin");
            long length = new FileInfo(source).Length;
            double megabytes = length / 1024.0 / 1024.0;

            Console.WriteLine($"File: {source}");
            Console.WriteLine($"Size: {megabytes:N1} MB");
            Console.WriteLine($"Throughput in MB/s, median of {Runs} runs, one warm-up discarded");
            Console.WriteLine();

            Console.Write("| Chunk size |");
            foreach (int concurrency in concurrencyLevels)
            {
                Console.Write($" c={concurrency} |");
            }
            Console.WriteLine();

            Console.Write("|---|");
            foreach (int concurrency in concurrencyLevels)
            {
                Console.Write("---|");
            }
            Console.WriteLine();

            foreach (int chunkSize in chunkSizes)
            {
                Console.Write($"| {chunkSize / 1024 / 1024} MB |");

                foreach (int concurrency in concurrencyLevels)
                {
                    double speed = Measure(source, destination, chunkSize, concurrency, megabytes);
                    Console.Write($" {speed:N1} |");
                }

                Console.WriteLine();
            }

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }

        private double Measure(string source, string destination, int chunkSize, int concurrency, double megabytes)
        {
            FileTransfer transfer = new FileTransfer(chunkSize, concurrency);
            List<double> speeds = new List<double>();

            // run 0 is a warm-up and is not recorded
            for (int run = 0; run <= Runs; run++)
            {
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                Stopwatch watch = Stopwatch.StartNew();
                transfer.ProcessFile(source, destination);
                watch.Stop();

                if (run > 0)
                {
                    speeds.Add(megabytes / watch.Elapsed.TotalSeconds);
                }
            }

            speeds.Sort();
            return speeds[speeds.Count / 2];
        }
    }
}