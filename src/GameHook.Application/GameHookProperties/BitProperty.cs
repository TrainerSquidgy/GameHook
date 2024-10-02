using GameHook.Domain.Interfaces;
using System.Collections;

namespace GameHook.Application.GameHookProperties
{
    public class BitProperty : BaseProperty, IGameHookProperty
    {
        public int NewPosition { get; }
        public BitProperty(IGameHookInstance instance, GameHookMapperVariables variables) : base(instance, variables)
        {
            NewPosition = variables.Position ?? throw new Exception($"Position is required for {Path}.");
        }

        private static byte SetBit(byte value, int bitIndex, bool bitValue)
        {
            // Create a mask with only the bit at the specified index set to 1.
            byte mask = (byte)(1 << bitIndex);

            // Clear the bit at the specified index.
            value &= (byte)~mask;

            // Set the bit to the specified value.
            if (bitValue)
            {
                value |= mask;
            }

            return value;
        }

        protected override byte[] FromValue(string value)
        {
            if (Bytes == null) throw new Exception("Bytes is NULL.");

            var result = (byte[])Bytes.Clone();
            result[0] = SetBit(result[0], NewPosition, bool.Parse(value));

            return result;
        }

        protected override object? ToValue(byte[] bytes)
        {
            return new BitArray(bytes).Get(NewPosition);
        }
    }
}
