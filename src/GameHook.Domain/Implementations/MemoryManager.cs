using GameHook.Domain.Interfaces;

namespace GameHook.Domain.Implementations
{
    public class MemoryManager : IMemoryManager
    {
        public MemoryManager()
        {
            Namespaces = new Dictionary<string, IMemoryNamespace>()
            {
                { "default", new MemoryNamespace() }
            };

            DefaultNamespace = Namespaces["default"];
        }

        public Dictionary<string, IMemoryNamespace> Namespaces { get; private set; }
        public IMemoryNamespace DefaultNamespace { get; }

        public void Fill(string area, MemoryAddress memoryAddress, byte[] data)
        {
            if (Namespaces.ContainsKey(area) == false)
            {
                Namespaces[area] = new MemoryNamespace();
            }

            Namespaces[area].Fill(memoryAddress, data);
        }
    }

    public class MemoryNamespace : IMemoryNamespace
    {
        public ICollection<IByteArray> Fragments { get; } = new List<IByteArray>();

        public void Fill(MemoryAddress memoryAddress, byte[] data)
        {
            int filledFragments = 0;

            foreach (var fragment in Fragments)
            {
                if (fragment.Contains(memoryAddress))
                {
                    var offset = (int)(memoryAddress - fragment.StartingAddress);
                    fragment.Fill(offset, data);

                    filledFragments += 1;
                }
            }

            if (filledFragments == 0)
            {
                Fragments.Add(new ByteArray(memoryAddress, data));
            }
        }

        public bool Contains(MemoryAddress memoryAddress) => Fragments.Any(fragment => fragment.Contains(memoryAddress));

        public IByteArray GetBytes(MemoryAddress memoryAddress, int length)
        {
            foreach (var fragment in Fragments)
            {
                if (fragment.Contains(memoryAddress))
                {
                    var offset = memoryAddress - fragment.StartingAddress;
                    return fragment.Slice((int)offset, length);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(memoryAddress), $"Memory address {memoryAddress.ToHexdecimalString()} is not contained in any fragment in the namespace.");
        }

        public byte get_byte(MemoryAddress memoryAddress)
        {
            return GetBytes(memoryAddress, 1).get_byte(0);
        }
    }

    public class ByteArray : IByteArray
    {
        public ByteArray(MemoryAddress startingAddress, byte[] data)
        {
            StartingAddress = startingAddress;
            Data = data;
        }

        public MemoryAddress StartingAddress { get; }
        public byte[] Data { get; }

        public void Fill(int offset, byte[] data)
        {
            if (offset < 0 || offset + data.Length > Data.Length)
            {
                throw new ArgumentException("Invalid offset or data length.");
            }

            Array.Copy(data, 0, Data, offset, data.Length);
        }

        public bool Contains(MemoryAddress memoryAddress)
        {
            var relativeAddr = memoryAddress - StartingAddress;
            return relativeAddr < Data.Length;
        }

        public IByteArray Slice(int offset, int length)
        {
            return new ByteArray(StartingAddress + (uint)offset, Data[offset..(offset + length)]);
        }

        public IByteArray[] Chunk(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            int chunkCount = (Data.Length + size - 1) / size;
            var chunks = new List<IByteArray>();

            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * size;
                int chunkSize = Math.Min(size, Data.Length - offset);

                chunks.Add(Slice(offset, chunkSize));
            }

            return chunks.ToArray();
        }

        public byte get_byte(int offset)
        {
            return Data[offset];
        }
    }
}
