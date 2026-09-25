using Microsoft.Win32.SafeHandles;
using System.Security.Cryptography;

namespace FileTransferTool
{
    internal class FileTransfer
    {
        private readonly int chunkSize;

        public FileTransfer(int chunkSize)
        {
            this.chunkSize = chunkSize;
        }

        public void ProcessFile(string source,string destination)
        {
            using SafeFileHandle sourceStream = File.OpenHandle(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            long length = RandomAccess.GetLength(sourceStream);

            using SafeFileHandle destinationStream = File.OpenHandle(destination, FileMode.Create, FileAccess.ReadWrite,FileShare.None,preallocationSize:length);
            using MD5 md5 = MD5.Create();
                byte[] buffer = new byte[chunkSize];
                byte[] verifybuffer = new byte[chunkSize];
                long chunksCount = (int)((length+chunkSize-1)/chunkSize);
            for (int i = 0; i < chunksCount; i++)
            {
                long position = (long)i* chunkSize;
                int size = (int)Math.Min(chunkSize,length-position);
                ReadExactly(sourceStream, buffer, size, position);
                byte[] sourceHash = md5.ComputeHash(buffer, 0, size);
                string sourceHashText = BitConverter.ToString(sourceHash);

                bool verify = false;
                int attempt = 0;

                while (!verify && attempt < 3)
                {
                    attempt++;
                    RandomAccess.Write(destinationStream, buffer.AsSpan(0, size), position);
                    ReadExactly(destinationStream, verifybuffer,size, position);
                    byte[] destinationHash = md5.ComputeHash(verifybuffer, 0,size);
                    verify = sourceHash.SequenceEqual(destinationHash);
                }
                if (!verify)
                {
                    throw new IOException($"Block failed. Block number = {i+1}, position = {position}");
                }

                Console.WriteLine($"blockNumber {i+1}: position = {position}, size = {size}, hash = {sourceHashText}, attempts = {attempt}");
            }
        }
        private void ReadExactly(SafeFileHandle handle, byte[] buffer ,int count, long position)
        {
            int total = 0;
            while (total < count)
            {
                int read = RandomAccess.Read(handle, buffer.AsSpan(total,count-total),position+total); 
                if(read == 0)
                {
                    throw new EndOfStreamException($"Unexpected end of file at position {position+total}");
                }
                total += read;
            }
        }
        public string GetHash(string path)
        {
            using(FileStream stream = new FileStream(path,FileMode.Open, FileAccess.Read))
            using(SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash);
            }
        }
    }
}