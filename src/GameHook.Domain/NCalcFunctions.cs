namespace GameHook.Domain
{
    public static class NCalcFunctions
    {
        public static int ReadBitRange(int bits, int upperRange, int lowerRange)
        {
            return (bits & ((1 << (upperRange + 1)) - 1)) >> lowerRange;
        }

        public static byte[] WriteBitRange(byte[] data, byte[] bitsToWrite, int upperRange, int lowerRange)
        {
            int bitCount = upperRange - lowerRange + 1;
            byte[] newData = new byte[data.Length];
            Array.Copy(data, newData, data.Length);

            int remainingBits = bitCount;
            int dataBitIndex = lowerRange;
            int bitsToWriteIndex = 0;

            while (remainingBits > 0)
            {
                int dataByteIndex = dataBitIndex / 8;
                int dataBitOffset = dataBitIndex % 8;

                int bitsToWriteByteIndex = bitsToWriteIndex / 8;
                int bitsToWriteBitOffset = bitsToWriteIndex % 8;

                int bitsToWriteRemaining = Math.Min(remainingBits, 8 - bitsToWriteBitOffset);
                int bitsToWriteMask = (1 << bitsToWriteRemaining) - 1;

                int bitsToWriteValue = bitsToWrite[bitsToWriteByteIndex] >> bitsToWriteBitOffset;
                bitsToWriteValue &= bitsToWriteMask;

                int dataMask = ~(bitsToWriteMask << dataBitOffset);
                int dataValue = (bitsToWriteValue << dataBitOffset) & ~dataMask;

                newData[dataByteIndex] &= (byte)dataMask;
                newData[dataByteIndex] |= (byte)dataValue;

                dataBitIndex += bitsToWriteRemaining;
                bitsToWriteIndex += bitsToWriteRemaining;
                remainingBits -= bitsToWriteRemaining;
            }

            return newData;
        }
    }
}
