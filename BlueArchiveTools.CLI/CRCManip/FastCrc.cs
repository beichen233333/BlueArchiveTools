using System;

namespace BlueArchiveTools.CLI.CRCManip
{
    public static class FastCrc
    {
        public static uint CrcNext(BaseCRC crc, byte[] source, uint value)
        {
            int numBytes = crc.NumBytes;
            int numBits = crc.NumBits;
            bool bigEndian = crc.BigEndian;
            uint[] lookupTable = crc.LookupTable;

            ulong mask = (1UL << numBits) - 1UL;
            int shift = (numBytes << 3) - 8;

            if (bigEndian)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    byte c = source[i];
                    byte index = (byte)(c ^ (value >> shift));
                    value = lookupTable[index] ^ (value << 8);
                    value &= (uint)mask;
                }
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                {
                    byte c = source[i];
                    byte index = (byte)(c ^ value);
                    value = lookupTable[index] ^ (value >> 8);
                    value &= (uint)mask;
                }
            }

            return value;
        }

        public static uint CrcPrev(BaseCRC crc, byte[] source, uint value)
        {
            int numBytes = crc.NumBytes;
            int numBits = crc.NumBits;
            bool bigEndian = crc.BigEndian;
            uint[] lookupTableReverse = crc.LookupTableReverse;

            ulong mask = (1UL << numBits) - 1UL;
            int shift = (numBytes << 3) - 8;

            if (bigEndian)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    byte c = source[i];
                    byte index = (byte)value;
                    value = (uint)(
                        (c << shift)
                        ^ lookupTableReverse[index]
                        ^ (value << shift)
                        ^ (value >> 8)
                    );
                    value &= (uint)mask;
                }
            }
            else
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    byte c = source[i];
                    byte index = (byte)(value >> shift);
                    value = (uint)(c ^ lookupTableReverse[index] ^ (value << 8));
                    value &= (uint)mask;
                }
            }
            return value;
        }
    }
}
