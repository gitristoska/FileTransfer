using Microsoft.Win32.SafeHandles;

using System.Buffers;
using System.Security.Cryptography;

namespace FileTransferTool
{
    internal class FileTransfer
    {
        private readonly int chunkSize;
        private readonly int concurrency;

        public FileTransfer(int chunkSize, int concurrency)
        {
            this.chunkSize = chunkSize;
            this.concurrency = concurrency;
        }

        public ChunkResult[] ProcessFile(string source, string destination)
        {
            using SafeFileHandle sourceStream = File.OpenHandle(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            long length = RandomAccess.GetLength(sourceStream);

            using SafeFileHandle destinationStream = File.OpenHandle(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.None, preallocationSize: length);

            int chunksCount = (int)((length + chunkSize - 1) / chunkSize);
            ChunkResult[] results = new ChunkResult[chunksCount];
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = concurrency;

            Parallel.For(0, chunksCount, options, i =>
            {
                long position = (long)i * chunkSize;
                int size = (int)Math.Min(chunkSize, length - position);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
                byte[] verifyBuffer = ArrayPool<byte>.Shared.Rent(size);

                try
                {
                    ReadExactly(sourceStream, buffer, size, position);
                    byte[] sourceHash = MD5.HashData(buffer.AsSpan(0, size));

                    bool verify = false;
                    int attempt = 0;

                    while (!verify && attempt < 3)
                    {
                        attempt++;
                        RandomAccess.Write(destinationStream, buffer.AsSpan(0, size), position);
                        ReadExactly(destinationStream, verifyBuffer, size, position);

                        byte[] destinationHash = MD5.HashData(verifyBuffer.AsSpan(0, size));
                        verify = sourceHash.SequenceEqual(destinationHash);
                    }
                    if (!verify)
                    {
                        throw new IOException($"Block failed. Block number = {i + 1}, position = {position}");
                    }

                    results[i] = new ChunkResult
                    {
                        BlockNumber = i + 1,
                        Position = position,
                        Size = size,
                        Hash = sourceHash,
                        Attempts = attempt
                    };
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    ArrayPool<byte>.Shared.Return(verifyBuffer);
                }

            });
            return results;
        }
        private void ReadExactly(SafeFileHandle handle, byte[] buffer, int count, long position)
        {
            int total = 0;
            while (total < count)
            {
                int read = RandomAccess.Read(handle, buffer.AsSpan(total, count - total), position + total);
                if (read == 0)
                {
                    throw new EndOfStreamException($"Unexpected end of file at position {position + total}");
                }
                total += read;
            }
        }

        public string GetHash(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash);
            }
        }

        public static int CalculateConcurrency()
        {
            return Math.Clamp(Environment.ProcessorCount, 2, 8);
        }

        public static int CalculateChunkSize(long fileSize, int concurrency)
        {
            int preferred = 4 * 1024 * 1024;
            int min = 64 * 1024;
            long maxForBalance = fileSize / (concurrency * 8L);
            if (maxForBalance < min)
            {
                return min;
            }
            if (maxForBalance > preferred)
            {
                return preferred;
            }
            return (int)maxForBalance;
        }
    }
}