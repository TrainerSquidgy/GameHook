using GameHook.Domain.DTOs;

namespace GameHook.Domain.Interfaces
{
    public interface IMapperFilesystemProvider
    {
        string MapperFolder { get; }

        string? BinaryMapperFolder { get; }

        IEnumerable<MapperFilesystemDTO> MapperFiles { get; }

        void RefreshMapperFiles();
    }
}
