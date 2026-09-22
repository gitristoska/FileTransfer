using System.Security.Cryptography;

namespace FileTransferTool
{
    internal class ChunkHasher
    {
        private readonly int chunkSize;

        public ChunkHasher(int chunkSize)
        {
            this.chunkSize = chunkSize;
        }

        public void ProcessFile(string source)
        {
            using (FileStream stream = new FileStream(source, FileMode.Open, FileAccess.Read))
            using (MD5 md5 = MD5.Create())
            {
                byte[] buffer = new byte[chunkSize];
                long position = 0;
                int blockNumber = 1;
                int bytesRead;
                while((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] hash = md5.ComputeHash(buffer, 0, bytesRead);
                    string hashText = BitConverter.ToString(hash);
                    Console.WriteLine($"blockNumber {blockNumber}: position = {position}, size = {bytesRead}, hash = {hashText}");
                    position += bytesRead;
                    blockNumber++;
                }
            }
        }
    }
}