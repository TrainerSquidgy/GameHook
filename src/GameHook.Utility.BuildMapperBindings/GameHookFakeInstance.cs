using GameHook.Domain.Interfaces;

namespace GameHook.Utility.BuildMapperBindings
{
    internal class GameHookFakeInstance : IGameHookInstance
    {
        public bool Initalized => throw new NotImplementedException();

        public Dictionary<string, object?> State => throw new NotImplementedException();

        public List<IClientNotifier> ClientNotifiers => throw new NotImplementedException();

        public IGameHookDriver? Driver => throw new NotImplementedException();

        public IGameHookMapper? Mapper => throw new NotImplementedException();

        public IPlatformOptions? PlatformOptions => throw new NotImplementedException();

        public Dictionary<string, object?> Variables => throw new NotImplementedException();

        public Task Load(IGameHookDriver driver, string mapperId)
        {
            throw new NotImplementedException();
        }

        public Task Read()
        {
            throw new NotImplementedException();
        }

        public Task ReadLoop()
        {
            throw new NotImplementedException();
        }

        public Task ResetState()
        {
            throw new NotImplementedException();
        }

        public object? Evalulate(string function, object? x, object? y)
        {
            throw new NotImplementedException();
        }
    }
}
