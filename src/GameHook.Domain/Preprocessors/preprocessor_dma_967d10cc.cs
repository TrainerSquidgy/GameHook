using GameHook.Domain.Interfaces;

namespace GameHook.Domain.Preprocessors
{
    public static partial class Preprocessor_967d10cc
    {
        public static MemoryAddress? Read(IMemoryManager memoryContainer, MemoryAddress memoryAddress, int size, int offset)
        {
            var dmaMemoryAddressBytes = memoryContainer.DefaultNamespace.GetBytes(memoryAddress, size);
            var dmaMemoryAddressValue = dmaMemoryAddressBytes.get_uint32_le();

            // Check to see if the DMA pointer references 0x00.
            // This can happen if the game is in the middle of a reset.
            if (dmaMemoryAddressValue == 0)
            {
                return null;
            }

            return (MemoryAddress)(dmaMemoryAddressValue + offset);
        }
    }
}
