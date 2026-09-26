using System;
using System.Diagnostics;
using System.IO;

namespace FileTransferTool
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.Write("Enter source file path: ");
            string source = (Console.ReadLine() ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(source))
            {
                Console.WriteLine("No source file specified.");
                return 1;
            }

            if (!File.Exists(source))
            {
                Console.WriteLine($"Source file not found: {source}");
                return 1;
            }

            Console.Write("Enter destination path: ");
            string destination = (Console.ReadLine() ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(destination))
            {
                Console.WriteLine("No destination specified.");
                return 1;
            }

            Directory.CreateDirectory(destination);
            destination = Path.Combine(destination, Path.GetFileName(source));

            FileInfo info = new FileInfo(source);

            Console.WriteLine();
            Console.WriteLine($"Source: {info.FullName}");
            Console.WriteLine($"Size: {info.Length:N0} bytes");
            Console.WriteLine($"Destination: {Path.GetFullPath(destination)}");

            int chunkSize = 4 * 1024 * 1024;
            int concurrency = 4;
            FileTransfer transfer = new FileTransfer(chunkSize, concurrency);

            Stopwatch watch = Stopwatch.StartNew();
            ChunkResult[] results = transfer.ProcessFile(source, destination);
            watch.Stop();

            foreach (ChunkResult chunkResult in results)
            {
                Console.WriteLine($"block number {chunkResult.BlockNumber} at position {chunkResult.Position} size={chunkResult.Size},hash={BitConverter.ToString(chunkResult.Hash)},attempts={chunkResult.Attempts}");
            }

            double seconds = watch.Elapsed.TotalSeconds;
            double megabytes = info.Length / 1024.0 / 1024.0;

            Console.WriteLine();
            Console.WriteLine($"Copied {megabytes:N1} MB in {seconds:F2} seconds");

            string hashSource = transfer.GetHash(source);
            string hashDestination = transfer.GetHash(destination);

            Console.WriteLine();
            Console.WriteLine($"Source SHA256: {hashSource}");
            Console.WriteLine($"Destination SHA256: {hashDestination}");

            if (hashSource == hashDestination)
            {
                Console.WriteLine("Checksum match");
                return 0;
            }
            else
            {

                Console.WriteLine("Checksum don't match");
                return 1;
            }
        }
    }
}