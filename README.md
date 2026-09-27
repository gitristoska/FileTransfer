# FileTransferTool

A console application for copying large files in chunks.

The application:

* copies the file in chunks
* calculates an MD5 hash for each chunk
* verifies the chunk after writing it
* retries a chunk if the verification fails
* calculates a final SHA256 hash for the source and destination

Built with C# and .NET 8.

## How to run

```bash
dotnet run --project FileTransferTool
```

The application asks for the source file and destination folder.

```text
Enter source file path: D:\videos\movie.mkv
Enter destination path: D:\backup
```

The destination folder is created if it does not exist. The original file name is kept.

The chunk size and number of workers are selected automatically.

## Example output

```text
Source: D:\crtani\Moana 2.mp4
Size: 1.977.077.251 bytes
Destination: D:\crtani\copy\Moana.2.mp4
Chunk size: 4096 KB
Workers: 8
block number 1 at position 0 size=4194304,hash=90-7E-49-5A-...,attempts=1
block number 2 at position 4194304 size=4194304,hash=1E-EF-DC-56-...,attempts=1
block number 3 at position 8388608 size=4194304,hash=0B-4B-4E-53-...,attempts=1
...

Copied 1.885,5 MB in 2,23 seconds
Throughput: 845,9 MB/s

Source SHA256: A0-DB-8F-C2-9E-...
Destination SHA256: A0-DB-8F-C2-9E-...
Checksum match
```

The application returns exit code `0` when the transfer succeeds and `1` when it fails.

## Transfer process

For each chunk:

1. Read the chunk from the source file.
2. Calculate its MD5 hash.
3. Write the chunk to the destination at the same position.
4. Read the destination chunk and calculate its MD5 hash.
5. Compare the two hashes.
6. If they do not match, retry the write and verification, up to 3 attempts.

After all chunks are copied, SHA256 is calculated for both complete files.

The chunks are processed concurrently. Positional file access is used so that different workers can work on different parts of the file.

## Chunk size and workers

The default chunk size is 4 MB.

I also added automatic chunk size selection for smaller files so that the available workers have enough chunks to process.

The number of workers is based on `Environment.ProcessorCount` and is limited to 2–8 workers.

## Benchmark

I added a separate benchmark mode to test different chunk sizes and worker counts.

```bash
dotnet run --project FileTransferTool -- benchmark <source-file> [destination-folder]
```

The benchmark tests:

* 64 MB, 16 MB, 4 MB and 1 MB chunks
* 1, 2, 4, 8 and 16 workers
* 1 warm-up run
* 3 recorded runs
* median throughput

### Test machine

* CPU: 12 cores / 16 logical processors
* RAM: 16 GB
* Disk: WD PC SN740 512 GB NVMe SSD

The source and destination were on partitions of the same physical disk.

### 512 MB test

| Chunk size |   c=1 |   c=2 |   c=4 |   c=8 |  c=16 |
| ---------- | ----: | ----: | ----: | ----: | ----: |
| 64 MB      | 239.4 | 295.4 | 451.5 | 609.8 | 640.3 |
| 16 MB      | 184.9 | 268.9 | 534.4 | 813.4 | 880.7 |
| 4 MB       | 190.7 | 356.1 | 627.6 | 932.3 | 936.2 |
| 1 MB       | 176.3 | 262.5 | 276.7 | 340.0 | 243.3 |

A second run gave different results, especially for the smaller chunks. This is expected because disk cache and system load can affect the measurements.

Based on tests, I kept 4 MB as the default chunk size.

## Tests

Run:

```bash
dotnet test
```

The tests cover:

* empty files
* files smaller than one chunk
* files exactly one chunk
* files with a smaller last chunk
* total chunk size matching the file size
* MD5 hash for each chunk
* copied file checksum
* sequential vs concurrent processing
* automatic chunk size calculation
* minimum chunk size
* worker count

## Project structure

```text
FileTransferTool/
  Program.cs
  FileTransfer.cs
  ChunkResult.cs
  Benchmark.cs

FileTransferTool.Tests/
  FileTransferTests.cs
```

`Program.cs` handles the console input and output.

`FileTransfer.cs` contains the file transfer, chunking, hashing, verification, retry and concurrency logic.

`ChunkResult.cs` stores the result for each chunk.

`Benchmark.cs` contains the performance tests.