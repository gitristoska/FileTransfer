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
            using (FileStream sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read))
            using (FileStream destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write))
            using (MD5 md5 = MD5.Create())
            {
                byte[] buffer = new byte[chunkSize];
                long position = 0;
                int blockNumber = 1;
                int bytesRead;
                while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] hash = md5.ComputeHash(buffer, 0, bytesRead);
                    string hashText = BitConverter.ToString(hash);
                    destinationStream.Write(buffer, 0, bytesRead);
                    Console.WriteLine($"blockNumber {blockNumber}: position = {position}, size = {bytesRead}, hash = {hashText}");
                    position += bytesRead;
                    blockNumber++;
                }
            }
        }
    }
}