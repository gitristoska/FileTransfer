using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferTool
{
    internal class ChunkResult
    {
        public int BlockNumber { get; set; }
        public long Position { get; set; }
        public int Size { get; set; }
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public int Attempts { get; set; }
    }
}
