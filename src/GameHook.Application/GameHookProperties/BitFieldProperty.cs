using GameHook.Domain.Interfaces;
using System.Collections;

namespace GameHook.Application.GameHookProperties
{
    public class BitFieldProperty : BaseProperty, IGameHookProperty
    {
        public BitFieldProperty(IGameHookInstance instance, GameHookMapperVariables variables) : base(instance, variables)
        {
        }

        protected override byte[] FromValue(string value)
        {
            throw new NotImplementedException();
        }

        protected override object? ToValue(byte[] data)
        {
            var bitArray = new BitArray(data);

            var boolArray = new bool[bitArray.Length];
            bitArray.CopyTo(boolArray, 0);

            return boolArray;
        }
    }
}
