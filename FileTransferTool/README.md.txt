# FileTransferTool 

A console tool that copies a large file in chunks, verifying every block on the way and the whole file at the end.

Built with C# and .NET 8.

## Running it

dotnet run --project FileTransferTool

The tool asks for two paths:

```
Enter source file path: D:\videos\movie.mkv
Enter destination path: D:\backup
```

The destination is always treated as folder. The folder is created if doesn't exist, and the copy keeps the source file name.


## Output
Each block is listed with its position in the source file, its size, its MD5 hash and
the number of attempts required. Both SHA256 checksums are printed at the end.

```
block number 1 at position 0 size=4194304, hash=7D-D6-05-C8-..., attempts=1
block number 2 at position 4194304 size=4194304, hash=0F-24-22-BC-..., attempts=1
...
Source SHA256:      EB-B2-18-80-...
Destination SHA256: EB-B2-18-80-...
Checksum match
```

Exit code is 0 on success and 1 on failure.

## How it works

1. The source file is divided into fixed-size blocks.
2. Each block is read from the source file and hashed using MD5.
3. The block is written to the destination at the same position.
4. The destination block is read back and its MD5 hash is compared with the source hash.
5. If the hashes do not match, the block is written and verified again, up to 3 attempts.
6. After all blocks are successfully transferred, both files are hashed using SHA256 and the checksums are compared.

Blocks can be processed concurrently with a limited number of workers.

## Measurements

_To be added._

| Chunk size | c=1 | c=2 | c=4 | c=8 |
|------------|-----|-----|-----|-----|
| 1 MB       |-----|-----|-----|-----|
| 4 MB       |-----|-----|-----|-----|
| 16 MB      |-----|-----|-----|-----|



## Project Structure

```
FileTransferTool/
  Program.cs        console input, validation, output
  FileTransfer.cs   chunking, hashing, verification, retry
  ChunkResult.cs    per-block result
```