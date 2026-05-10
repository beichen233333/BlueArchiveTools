using System;
using System.Collections.Generic;

namespace BlueArchiveTools.CLI.CRCManip
{
    public abstract class BaseCRC
    {
        public abstract int NumBits { get; }
        public abstract uint Polynomial { get; }
        public virtual uint InitialXor => 0;
        public virtual uint FinalXor => 0;
        public virtual bool BigEndian => false;
        public virtual bool UseFileSize => false;

        public int NumBytes { get; private set; }
        public uint[] LookupTable { get; private set; }
        public uint[] LookupTableReverse { get; private set; }

        private uint _value;
        private long _consumed;

        protected BaseCRC()
        {
            if (NumBits % 8 != 0) throw new Exception("num_bits must be a multiple of 8");
            NumBytes = NumBits / 8;

            LookupTable = CreateLookupTable(Polynomial, NumBits, BigEndian);
            LookupTableReverse = CreateReverseLookupTable(Polynomial, NumBits, BigEndian);

            _value = InitialXor;
            _consumed = 0;
        }

        public static uint[] CreateLookupTable(uint poly, int numBits, bool bigEndian)
        {
            uint polyRev = Utils.GetPolynomialReverse(poly, numBits);
            uint mask = 1U << (numBits - 1);
            uint[] table = new uint[0x100];

            for (int num = 0; num < 0x100; num++)
            {
                uint val = (uint)num;
                if (bigEndian)
                {
                    val = Utils.SwapEndian(val, numBits);
                }
                for (int bit = 0; bit < 8; bit++)
                {
                    if (bigEndian)
                    {
                        val = (val & mask) != 0 ? (val << 1) ^ poly : (val << 1);
                    }
                    else
                    {
                        val = (val & 1) != 0 ? (val >> 1) ^ polyRev : (val >> 1);
                    }
                }
                table[num] = val & ((1U << numBits) - 1);
            }
            return table;
        }

        public static uint[] CreateReverseLookupTable(uint poly, int numBits, bool bigEndian)
        {
            uint polyRev = Utils.GetPolynomialReverse(poly, numBits);
            uint mask = 1U << (numBits - 1);
            uint[] table = new uint[0x100];

            for (int num = 0; num < 0x100; num++)
            {
                uint val = (uint)num;
                if (!bigEndian)
                {
                    val = Utils.SwapEndian(val, numBits);
                }
                for (int bit = 0; bit < 8; bit++)
                {
                    if (bigEndian)
                    {
                        val = (val & 1) != 0 ? ((val ^ poly) >> 1) | mask : (val >> 1);
                    }
                    else
                    {
                        val = (val & mask) != 0 ? ((val ^ polyRev) << 1) | 1 : (val << 1);
                    }
                }
                if (bigEndian)
                {
                    val ^= Utils.SwapEndian((uint)num, numBits);
                }
                table[num] = val & ((1U << numBits) - 1);
            }
            return table;
        }

        public BaseCRC Reset(uint? rawValue = null)
        {
            _value = rawValue ?? InitialXor;
            _consumed = 0;
            return this;
        }

        public BaseCRC Update(byte[] source)
        {
            _value = GetNextValue(source, _value) & ((1U << NumBits) - 1);
            _consumed += source.Length;
            return this;
        }

        public BaseCRC UpdateReverse(byte[] source)
        {
            _value = GetPrevValue(source, _value) & ((1U << NumBits) - 1);
            _consumed += source.Length;
            return this;
        }

        public uint Digest()
        {
            uint value = _value;

            if (UseFileSize)
            {
                List<byte> patch = new List<byte>();
                long tmp = _consumed;
                while (tmp > 0)
                {
                    patch.Add((byte)(tmp & 0xFF));
                    tmp >>= 8;
                }
                value = GetNextValue(patch.ToArray(), value);
            }

            value ^= FinalXor;
            value &= ((1U << NumBits) - 1);
            return value;
        }

        public string HexDigest()
        {
            return Digest().ToString($"X{NumBytes * 2}");
        }

        public uint GetPrevValue(byte[] source, uint value)
        {
            return FastCrc.CrcPrev(this, source, value);
        }

        public uint GetNextValue(byte[] source, uint value)
        {
            return FastCrc.CrcNext(this, source, value);
        }

        public uint RawValue => _value;
    }

    public class CRC32 : BaseCRC
    {
        public override int NumBits => 32;
        public override uint Polynomial => 0x04C11DB7;
        public override uint InitialXor => 0xFFFFFFFF;
        public override uint FinalXor => 0xFFFFFFFF;
    }

    public class CRC32POSIX : BaseCRC
    {
        public override int NumBits => 32;
        public override uint Polynomial => 0x04C11DB7;
        public override uint FinalXor => 0xFFFFFFFF;
        public override bool BigEndian => true;
        public override bool UseFileSize => true;
    }

    public class CRC16CCITT : BaseCRC
    {
        public override int NumBits => 16;
        public override uint Polynomial => 0x1021;
    }

    public class CRC16XMODEM : BaseCRC
    {
        public override int NumBits => 16;
        public override uint Polynomial => 0x1021;
        public override bool BigEndian => true;
    }

    public class CRC16IBM : BaseCRC
    {
        public override int NumBits => 16;
        public override uint Polynomial => 0x8005;
    }
}
