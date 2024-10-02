namespace GameHook.Domain.Interfaces
{
    public enum MapperFormats
    {
        NONE,
        YAML,
        XML
    }

    public class MapperMetadata
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string GameName { get; init; } = string.Empty;
        public string GamePlatform { get; init; } = string.Empty;
    }

    public interface IGameHookMapper
    {
        public MapperFormats Format { get; }
        MapperMetadata Metadata { get; }
        Dictionary<string, IGameHookProperty> Properties { get; }
        Dictionary<string, GlossaryList> Glossary { get; }

        string? GlobalScript { get; }
        bool HasGlobalPreprocessor { get; }
        bool HasGlobalPostprocessor { get; }
    }
}
