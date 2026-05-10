using System;
using System.IO;

namespace BlueArchiveTools.CLI.CRCManip
{
    public class InvalidPositionError : ValueError
    {
        public InvalidPositionError() : base("patch position is located outside available input") { }
    }

    public class ValueError : Exception
    {
        public ValueError(string message) : base(message) { }
    }

    public static class Algorithm
    {
        public const int DEFAULT_CHUNK_SIZE = 1024 * 1024;

        public static (long, long) FixStartEndPos(long? startPos, long? endPos, Stream handle)
        {
            long start = startPos ?? 0;
            long end = endPos ?? 0;

            if (!endPos.HasValue)
            {
                long oldPos = handle.Position;
                handle.Seek(0, SeekOrigin.End);
                end = handle.Position;
                handle.Seek(oldPos, SeekOrigin.Begin);
            }

            if (start > end)
            {
                long temp = start;
                start = end;
                end = temp;
            }

            if (start < 0 || end < 0 || start > end)
            {
                throw new Exception("Assertion failed: Invalid start/end position");
            }

            return (start, end);
        }

        public static void Consume(
            BaseCRC crc,
            Stream handle,
            long? startPos = null,
            long? endPos = null,
            int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var (start, end) = FixStartEndPos(startPos, endPos, handle);
            long remaining = end - start;
            if (remaining == 0) return;

            using (var progress = Utils.TrackProgress("checksum", remaining))
            {
                handle.Seek(start, SeekOrigin.Begin);
                byte[] buffer = new byte[chunkSize];
                while (remaining > 0)
                {
                    int currentChunkSize = (int)Math.Min(chunkSize, remaining);
                    int bytesRead = handle.Read(buffer, 0, currentChunkSize);
                    if (bytesRead == 0) break;

                    byte[] chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    
                    crc.Update(chunk);
                    remaining -= bytesRead;
                    progress.Update(bytesRead);
                }
            }
        }

        public static void ConsumeReverse(
            BaseCRC crc,
            Stream handle,
            long? startPos,
            long? endPos,
            int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var (start, end) = FixStartEndPos(startPos, endPos, handle);
            long remaining = end - start;
            if (remaining == 0) return;

            using (var progress = Utils.TrackProgress("checksum 2", remaining))
            {
                byte[] buffer = new byte[chunkSize];
                while (remaining > 0)
                {
                    int currentChunkSize = (int)Math.Min(chunkSize, remaining);
                    handle.Seek(start + remaining - currentChunkSize, SeekOrigin.Begin);
                    int bytesRead = handle.Read(buffer, 0, currentChunkSize);
                    if (bytesRead == 0) break;

                    byte[] chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    
                    crc.UpdateReverse(chunk);
                    remaining -= bytesRead;
                    progress.Update(bytesRead);
                }
            }
        }

        public static uint ComputePatch(
            BaseCRC crc,
            Stream handle,
            uint targetChecksum,
            long targetPos,
            bool overwrite)
        {
            handle.Seek(0, SeekOrigin.End);
            long origFileSize = handle.Position;
            if (targetPos < 0 || targetPos > origFileSize)
                throw new InvalidPositionError();

            long targetFileSize;
            if (overwrite)
            {
                targetFileSize = origFileSize;
                if (targetPos + crc.NumBytes > origFileSize)
                    targetFileSize = targetPos + crc.NumBytes;
            }
            else
            {
                targetFileSize = origFileSize + crc.NumBytes;
            }

            targetChecksum ^= crc.FinalXor;
            if (crc.UseFileSize)
            {
                targetChecksum = crc.GetPrevValue(
                    Utils.NumToBytes(targetFileSize), targetChecksum
                );
            }

            long posStart = 0;
            long posBeforePatch = targetPos;
            long posAfterPatch = targetPos + (overwrite ? crc.NumBytes : 0);
            long posEnd = origFileSize;

            crc.Reset(crc.InitialXor);
            Consume(crc, handle, posStart, posBeforePatch);
            uint checksum1 = crc.RawValue;

            crc.Reset(targetChecksum);
            ConsumeReverse(crc, handle, posEnd, posAfterPatch);
            uint checksum2 = crc.RawValue;

            if (crc.BigEndian)
            {
                checksum1 = Utils.SwapEndian(checksum1, crc.NumBits);
            }

            uint patch = crc.GetPrevValue(
                Utils.NumToBytes(checksum1, crc.NumBytes), checksum2
            );
            
            if (crc.BigEndian)
            {
                patch = Utils.SwapEndian(patch, crc.NumBits);
            }

            return patch;
        }

        public static void ApplyPatch(
            BaseCRC crc,
            uint targetChecksum,
            Stream inputHandle,
            Stream outputHandle,
            long targetPos,
            bool overwrite,
            int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            inputHandle.Seek(0, SeekOrigin.End);
            long endPos = inputHandle.Position;
            if (targetPos < 0 || targetPos > endPos)
                throw new InvalidPositionError();

            uint patch = ComputePatch(crc, inputHandle, targetChecksum, targetPos, overwrite);
            inputHandle.Seek(0, SeekOrigin.Begin);
            long pos = 0;

            using (var progress = Utils.TrackProgress("output", endPos))
            {
                byte[] buffer = new byte[chunkSize];

                while (pos < targetPos)
                {
                    int curChunkSize = (int)Math.Min(chunkSize, targetPos - inputHandle.Position);
                    int bytesRead = inputHandle.Read(buffer, 0, curChunkSize);
                    outputHandle.Write(buffer, 0, bytesRead);
                    pos += bytesRead;
                    progress.Update(bytesRead);
                }

                byte[] patchBytes = Utils.NumToBytes(patch, crc.NumBytes);
                outputHandle.Write(patchBytes, 0, patchBytes.Length);
                
                if (overwrite)
                {
                    pos += crc.NumBytes;
                    inputHandle.Seek(pos, SeekOrigin.Begin);
                }

                while (pos < endPos)
                {
                    int curChunkSize = (int)Math.Min(chunkSize, endPos - inputHandle.Position);
                    int bytesRead = inputHandle.Read(buffer, 0, curChunkSize);
                    outputHandle.Write(buffer, 0, bytesRead);
                    pos += bytesRead;
                    progress.Update(bytesRead);
                }
            }
        }
    }
}
