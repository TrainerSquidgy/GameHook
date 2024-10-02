using GameHook.Domain;
using GameHook.Domain.Interfaces;

namespace GameHook.Application
{
    public class GameHookMapper : IGameHookMapper
    {
        public GameHookMapper(
            MapperFormats format,
            MapperMetadata metadata,
            IEnumerable<IGameHookProperty> properties,
            IEnumerable<GlossaryList> glossary,
            string? globalScript,
            bool hasGlobalPreprocessor,
            bool hasGlobalPostprocessor)
        {
            Format = format;
            Metadata = metadata;
            Properties = properties.ToDictionary(x => x.Path, x => x);
            Glossary = glossary.ToDictionary(x => x.Name, x => x);

            GlobalScript = globalScript;
            HasGlobalPreprocessor = hasGlobalPreprocessor;
            HasGlobalPostprocessor = hasGlobalPostprocessor;
        }

        public MapperFormats Format { get; }
        public MapperMetadata Metadata { get; }
        public Dictionary<string, IGameHookProperty> Properties { get; }
        public Dictionary<string, GlossaryList> Glossary { get; }

        public string? GlobalScript { get; }
        public bool HasGlobalPreprocessor { get; }
        public bool HasGlobalPostprocessor { get; }

        public IGameHookProperty[] GetAllProperties() => Properties.Values.ToArray();
    }
}