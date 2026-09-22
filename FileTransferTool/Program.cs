using System;
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

            FileInfo info = new FileInfo(source);

            Console.WriteLine();
            Console.WriteLine($"Source: {info.FullName}");
            Console.WriteLine($"Size: {info.Length:N0} bytes");
            Console.WriteLine($"Destination: {Path.GetFullPath(destination)}");

            int chunkSize = 4 * 1024 * 1024;
            ChunkHasher hasher = new ChunkHasher(chunkSize);
            hasher.ProcessFile(source);

            return 0;
        }
    }
}