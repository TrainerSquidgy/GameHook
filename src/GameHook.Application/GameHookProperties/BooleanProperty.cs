using GameHook.Domain.Interfaces;

namespace GameHook.Application.GameHookProperties
{
    public class BooleanProperty : BaseProperty, IGameHookProperty
    {
        public BooleanProperty(IGameHookInstance instance, GameHookMapperVariables variables) : base(instance, variables)
        {
        }

        protected override byte[] FromValue(string value)
        {
            var booleanValue = bool.Parse(value);
            return booleanValue == true ? new byte[] { 0x01 } : new byte[] { 0x00 };
        }

        protected override object? ToValue(byte[] data)
        {
            return data[0] == 0x00 ? false : true;
        }
    }
}
