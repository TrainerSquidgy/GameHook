using GameHook.Domain.DTOs;

namespace GameHook.Domain.Interfaces
{
    public interface IClientNotifier
    {
        Task SendGameHookError(ProblemDetailsForClientDTO details);
        Task SendInstanceReset();
        Task SendMapperLoaded(IGameHookMapper mapper);
        Task SendDriverError(ProblemDetailsForClientDTO details);
        Task SendPropertiesChanged(IEnumerable<IGameHookProperty> properties);
    }
}
