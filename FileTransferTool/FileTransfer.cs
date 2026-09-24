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
            using (FileStream destinationStream = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite))
            using (MD5 md5 = MD5.Create())
            {
                byte[] buffer = new byte[chunkSize];
                byte[] verifybuffer = new byte[chunkSize];

                long position = 0;
                int blockNumber = 1;
                int bytesRead;

                while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] sourceHash = md5.ComputeHash(buffer, 0, bytesRead);
                    string sourceHashText = BitConverter.ToString(sourceHash);

                    bool verify = false;
                    int attempt = 0;

                    while(!verify && attempt < 3)
                    {
                        attempt++;
                        destinationStream.Position = position;
                        destinationStream.Write(buffer, 0, bytesRead);

                        destinationStream.Position = position;
                        destinationStream.ReadExactly(verifybuffer, 0, bytesRead);

                        byte[] destinationHash = md5.ComputeHash(verifybuffer, 0, bytesRead);
                        verify = sourceHash.SequenceEqual(destinationHash);
                    }
                    if (!verify)
                    {
                        throw new IOException($"Block failed. Block number = {blockNumber}, position = {position}");
                    }
                    
                    Console.WriteLine($"blockNumber {blockNumber}: position = {position}, size = {bytesRead}, hash = {sourceHashText}, attempts = {attempt}");
                    position += bytesRead;
                    blockNumber++;
                }
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