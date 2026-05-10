using System;
using System.Diagnostics;

namespace BlueArchiveTools.CLI.CRCManip
{
    public static class Utils
    {
        public static bool PROGRESSBARS_ENABLED = true;

        public static uint GetPolynomialReverse(uint polynomial, int numBits)
        {
            uint result = 0;
            for (int i = 0; i < numBits; i++)
            {
                result <<= 1;
                result |= polynomial & 1;
                polynomial >>= 1;
            }
            return result;
        }

        public static uint SwapEndian(uint value, int numBits)
        {
            uint result = 0;
            int numBytes = numBits / 8;
            for (int i = 0; i < numBytes; i++)
            {
                result <<= 8;
                result |= value & 0xFF;
                value >>= 8;
            }
            return result;
        }

        public static byte[] NumToBytes(long val, int? numBytes = null)
        {
            if (numBytes.HasValue)
            {
                long mask = (1L << (numBytes.Value * 8)) - 1;
                long maskedVal = val & mask;
                byte[] result = new byte[numBytes.Value];
                for (int i = 0; i < numBytes.Value; i++)
                {
                    result[i] = (byte)(maskedVal & 0xFF);
                    maskedVal >>= 8;
                }
                return result;
            }
            else
            {
                long temp = val;
                int bitLength = 0;
                while (temp > 0)
                {
                    bitLength++;
                    temp >>= 1;
                }
                int bytesNeeded = (bitLength + 7) / 8;
                if (bytesNeeded == 0) bytesNeeded = 1;
                byte[] result = new byte[bytesNeeded];
                long maskedVal = val;
                for (int i = 0; i < bytesNeeded; i++)
                {
                    result[i] = (byte)(maskedVal & 0xFF);
                    maskedVal >>= 8;
                }
                return result;
            }
        }

        public static void DisableProgressbars()
        {
            PROGRESSBARS_ENABLED = false;
        }

        public static Tqdm TrackProgress(string desc, long total)
        {
            return new Tqdm(desc, total, !PROGRESSBARS_ENABLED);
        }
    }

    public class Tqdm : IDisposable
    {
        private string desc;
        private long total;
        private bool disable;
        private long current;
        private Stopwatch stopwatch;

        public Tqdm(string desc, long total, bool disable)
        {
            this.desc = desc;
            this.total = total;
            this.disable = disable;
            this.current = 0;
            this.stopwatch = Stopwatch.StartNew();
        }

        public void Update(long n)
        {
            if (disable) return;
            current += n;
        }

        public void Dispose()
        {
            stopwatch.Stop();
        }
    }
}
