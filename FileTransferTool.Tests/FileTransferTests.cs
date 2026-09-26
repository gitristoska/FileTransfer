using System.Security.Cryptography;

namespace FileTransferTool.Tests
{
    public class FileTransferTests : IDisposable
    {
        private const int ChunkSize = 1024 * 1024;

        private readonly string testFolder;

        public FileTransferTests()
        {
            testFolder = Path.Combine(Path.GetTempPath(), "FileTransferTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testFolder);
        }

        public void Dispose()
        {
            Directory.Delete(testFolder, true);
        }

        private string CreateSourceFile(string name, int size)
        {
            string path = Path.Combine(testFolder, name);
            byte[] data = new byte[size];
            Random.Shared.NextBytes(data);
            File.WriteAllBytes(path, data);
            return path;
        }

        private string DestinationFor(string name)
        {
            return Path.Combine(testFolder, "copy-" + name);
        }

        [Fact]
        public void EmptyFileProducesNoChunks()
        {
            string source = CreateSourceFile("empty.bin", 0);
            string destination = DestinationFor("empty.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            ChunkResult[] results = transfer.ProcessFile(source, destination);

            Assert.Empty(results);
            Assert.Equal(transfer.GetHash(source), transfer.GetHash(destination));
        }

        [Fact]
        public void FileSmallerThanOneChunkProducesOneChunk()
        {
            string source = CreateSourceFile("small.bin", 1000);
            string destination = DestinationFor("small.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            ChunkResult[] results = transfer.ProcessFile(source, destination);

            Assert.Single(results);
            Assert.Equal(0, results[0].Position);
            Assert.Equal(1000, results[0].Size);
        }

        [Fact]
        public void LastChunkIsSmallerWhenFileIsNotAMultipleOfChunkSize()
        {
            int size = ChunkSize * 3 + 500;
            string source = CreateSourceFile("partial.bin", size);
            string destination = DestinationFor("partial.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            ChunkResult[] results = transfer.ProcessFile(source, destination);

            Assert.Equal(4, results.Length);
            Assert.Equal(ChunkSize, results[0].Size);
            Assert.Equal(500, results[3].Size);
        }

        [Fact]
        public void ChunkSizesSumToFileLength()
        {
            int size = ChunkSize * 5 + 12345;
            string source = CreateSourceFile("sum.bin", size);
            string destination = DestinationFor("sum.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            ChunkResult[] results = transfer.ProcessFile(source, destination);

            long total = results.Sum(result => (long)result.Size);

            Assert.Equal(size, total);
        }

        [Fact]
        public void EveryChunkHashMatchesTheBytesAtItsPosition()
        {
            int size = ChunkSize * 3 + 777;
            string source = CreateSourceFile("hash.bin", size);
            string destination = DestinationFor("hash.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            ChunkResult[] results = transfer.ProcessFile(source, destination);

            byte[] data = File.ReadAllBytes(source);

            foreach (ChunkResult result in results)
            {
                byte[] expected = MD5.HashData(data.AsSpan((int)result.Position, result.Size));
                Assert.Equal(expected, result.Hash);
            }
        }

        [Fact]
        public void CopiedFileMatchesTheSource()
        {
            string source = CreateSourceFile("copy.bin", ChunkSize * 4 + 999);
            string destination = DestinationFor("copy.bin");

            FileTransfer transfer = new FileTransfer(ChunkSize, 4);
            transfer.ProcessFile(source, destination);

            Assert.Equal(transfer.GetHash(source), transfer.GetHash(destination));
        }

        [Fact]
        public void ConcurrencyDoesNotChangeTheResult()
        {
            string source = CreateSourceFile("concurrent.bin", ChunkSize * 8 + 777);

            ChunkResult[] sequential = new FileTransfer(ChunkSize, 1)
                .ProcessFile(source, DestinationFor("sequential.bin"));

            ChunkResult[] parallel = new FileTransfer(ChunkSize, 4)
                .ProcessFile(source, DestinationFor("parallel.bin"));

            Assert.Equal(sequential.Length, parallel.Length);

            for (int i = 0; i < sequential.Length; i++)
            {
                Assert.Equal(sequential[i].BlockNumber, parallel[i].BlockNumber);
                Assert.Equal(sequential[i].Position, parallel[i].Position);
                Assert.Equal(sequential[i].Size, parallel[i].Size);
                Assert.Equal(sequential[i].Hash, parallel[i].Hash);
            }
        }
    }
}