using GameHook.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace GameHook.Application.GameHookProperties
{
    public enum NibblePosition
    {
        MostSignificant,
        LeastSignificant
    }

    public class NibbleProperty : BaseProperty, IGameHookProperty
    {
        public NibblePosition NewPosition { get; }
        public NibbleProperty(IGameHookInstance instance, GameHookMapperVariables variables) : base(instance, variables)
        {
            NewPosition = (NibblePosition)(variables.Position ?? throw new Exception($"Position is required for {Path}."));
        }

        protected override byte[] FromValue(string value)
        {
            var nibbleValue = int.Parse(value);

            if (nibbleValue < 0)
            {
                throw new ValidationException("Nibble cannot be less than 0.");
            }

            if (nibbleValue > 15)
            {
                throw new ValidationException("Nibble cannot be greater than 15.");
            }

            if (Bytes == null || Bytes.Length == 0)
            {
                throw new Exception("Byte array cannot be null or empty.");
            }

            var bytes = new byte[Bytes.Length];
            Array.Copy(Bytes, bytes, bytes.Length);

            byte newValue = (byte)(nibbleValue & 0x0F);
            byte targetNibble = (byte)(bytes[0] & (NewPosition == NibblePosition.MostSignificant ? 0x0F : 0xF0));

            if (NewPosition == NibblePosition.MostSignificant)
            {
                bytes[0] = (byte)((newValue << 4) | targetNibble);
            }
            else
            {
                bytes[0] = (byte)(targetNibble | newValue);
            }

            return bytes;
        }

        protected override object? ToValue(byte[] data)
        {
            if (NewPosition == NibblePosition.LeastSignificant)
            {
                return data[0] & 0x0F;
            }
            else if (NewPosition == NibblePosition.MostSignificant)
            {
                return (data[0] >> 4) & 0x0F;
            }
            else
            {
                throw new Exception($"Unknown nibble position supplied: {NewPosition}");
            }
        }
    }
}
