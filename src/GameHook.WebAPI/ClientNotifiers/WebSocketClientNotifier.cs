using GameHook.Domain.DTOs;
using GameHook.Domain.Interfaces;
using GameHook.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameHook.WebAPI.ClientNotifiers
{
    public class WebSocketClientNotifier : IClientNotifier
    {
        private readonly IHubContext<UpdateHub> _hubContext;
        public WebSocketClientNotifier(IHubContext<UpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendGameHookError(ProblemDetailsForClientDTO problemDetails) =>
            _hubContext.Clients.All.SendAsync("GameHookError", problemDetails);

        public Task SendInstanceReset() =>
            _hubContext.Clients.All.SendAsync("InstanceReset");

        public Task SendMapperLoaded(IGameHookMapper mapper) =>
            _hubContext.Clients.All.SendAsync("MapperLoaded");

        public Task SendDriverError(ProblemDetailsForClientDTO problemDetails) =>
            _hubContext.Clients.All.SendAsync("DriverError", problemDetails);

        public Task SendPropertiesChanged(IEnumerable<IGameHookProperty> properties) =>
            _hubContext.Clients.All.SendAsync("PropertiesChanged", properties.Select(x => new
            {
                path = x.Path,
                address = x.Address,
                frozen = x.Frozen,
                value = x.Value,
                bytes = x.Bytes?.Select(x => (int)x).ToArray(),
                fieldsChanged = x.FieldsChanged
            }).ToArray());
    }
}
