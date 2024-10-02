// G3 - Encrypted Block-shuffling

using GameHook.Domain.Interfaces;

namespace GameHook.Domain.Preprocessors
{
    public static partial class Preprocessor_a245dcac
    {
        static CacheContainer? Cache;

        public class CacheContainer
        {
            public MemoryAddress Address { get; init; }
            public uint DecryptionKey { get; init; }
            public uint Checksum { get; init; }
            public int[] SubstructureOrdering { get; init; } = Array.Empty<int>();

            public byte[] EncryptedData { get; init; } = Array.Empty<byte>();
            public byte[] DecryptedData { get; init; } = Array.Empty<byte>();
        }

        public class ReadResult
        {
            public MemoryAddress Address { get; init; }

            public byte[] EncryptedData { get; init; } = Array.Empty<byte>();
            public byte[] DecryptedData { get; init; } = Array.Empty<byte>();
        }

        public class WriteResult
        {
            public MemoryAddress Address { get; init; }
            public byte[] Bytes { get; init; } = Array.Empty<byte>();
        }

        // Used beforehand to cache the data block.
        static int[] CalculateSubstructureOrder_a245dcac(uint substructureType)
        {
            return substructureType switch
            {
                0 => new int[4] { 0, 1, 2, 3 },
                1 => new int[4] { 0, 1, 3, 2 },
                2 => new int[4] { 0, 2, 1, 3 },
                3 => new int[4] { 0, 3, 1, 2 },
                4 => new int[4] { 0, 2, 3, 1 },
                5 => new int[4] { 0, 3, 2, 1 },
                6 => new int[4] { 1, 0, 2, 3 },
                7 => new int[4] { 1, 0, 3, 2 },
                8 => new int[4] { 2, 0, 1, 3 },
                9 => new int[4] { 3, 0, 1, 2 },
                10 => new int[4] { 2, 0, 3, 1 },
                11 => new int[4] { 3, 0, 2, 1 },
                12 => new int[4] { 1, 2, 0, 3 },
                13 => new int[4] { 1, 3, 0, 2 },
                14 => new int[4] { 2, 1, 0, 3 },
                15 => new int[4] { 3, 1, 0, 2 },
                16 => new int[4] { 2, 3, 0, 1 },
                17 => new int[4] { 3, 2, 0, 1 },
                18 => new int[4] { 1, 2, 3, 0 },
                19 => new int[4] { 1, 3, 2, 0 },
                20 => new int[4] { 2, 1, 3, 0 },
                21 => new int[4] { 3, 1, 2, 0 },
                22 => new int[4] { 2, 3, 1, 0 },
                23 => new int[4] { 3, 2, 1, 0 },
                _ => throw new Exception($"data_block_a245dcac returned a unknown substructure order of {substructureType}.")
            };
        }

        public static void Decrypt(IMemoryManager memoryContainer, MemoryAddress startingAddress)
        {
            // The encrypted data block starts 32 bytes from the start of the p structure.
            var encryptedDataStructureStartingAddress = startingAddress + 32;
            var encryptedData = memoryContainer.DefaultNamespace.GetBytes(encryptedDataStructureStartingAddress, 48);

            if (Cache != null && Cache.EncryptedData.SequenceEqual(encryptedData.Data))
            {
                return;
            }

            var personalityValue = memoryContainer.DefaultNamespace.get_uint32_be(startingAddress);
            var originalTrainerId = memoryContainer.DefaultNamespace.get_uint32_be(startingAddress + 4);

            // The order of the structures is determined by the personality value of the P modulo 24,
            // as shown below, where G, A, E, and M stand for the substructures growth, attacks, EVs and condition, and miscellaneous, respectively.
            var substructureType = personalityValue % 24;

            var substructureOrder = CalculateSubstructureOrder_a245dcac(substructureType);

            // To obtain the 32-bit decryption key, the entire OTID number must be XORed with the personality value of the entry.
            var decryptionKey = originalTrainerId ^ personalityValue;

            // Checksum is used later if we ever need to write back to this P structure.
            var checksum = memoryContainer.DefaultNamespace.get_uint16_be(startingAddress + 28);

            // This key can then be used to decrypt the encrypted data block (starting at offset 32)
            // by XORing it, 32 bits (or 4 bytes) at a time.
            var decryptedData = encryptedData
                .Chunk(4)
                .SelectMany(x => BitConverter.GetBytes(x.get_uint32_be() ^ decryptionKey))
                .ToArray();

            // Return the byte array decrypted.
            Cache = new CacheContainer()
            {
                Address = encryptedDataStructureStartingAddress,
                DecryptionKey = decryptionKey,
                Checksum = checksum,
                SubstructureOrdering = substructureOrder,
                EncryptedData = encryptedData.Data,
                DecryptedData = decryptedData
            };
        }

        public static ReadResult Read(IMemoryManager memoryContainer, MemoryAddress startingAddress, int structureIndex, int offset, int size)
        {
            if (Cache == null) { Decrypt(memoryContainer, startingAddress); }
            if (Cache == null) { throw new Exception("Cache is null, but should have been filled."); }

            var structurePositionForProperty = Cache.SubstructureOrdering[structureIndex];
            var propertyStartingOffset = (structurePositionForProperty * 12) + offset;
            var propertyEndingOffset = propertyStartingOffset + size;

            return new ReadResult()
            {
                Address = Cache.Address + (uint)propertyStartingOffset,
                EncryptedData = Cache.EncryptedData[propertyStartingOffset..propertyEndingOffset],
                DecryptedData = Cache.DecryptedData[propertyStartingOffset..propertyEndingOffset]
            };
        }

        public static IEnumerable<WriteResult> Write(uint address, byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}