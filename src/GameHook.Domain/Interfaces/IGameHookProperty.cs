namespace GameHook.Domain.Interfaces
{
    public class PropertyValueResult
    {
        public IEnumerable<string> FieldsChanged { get; init; } = new List<string>();
    }

    public class GameHookMapperVariables
    {
        public string Path { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
        public string? MemoryContainer { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int? Length { get; set; } = 1;
        public int? Size { get; set; }
        public int? Position { get; set; }
        public string? Reference { get; set; }
        public string? Description { get; set; }

        public string? StaticValue { get; set; }

        public string? YamlPreprocessor { get; set; }
        public string? YamlPostprocessorReader { get; set; }
        public string? YamlPostprocessorWriter { get; set; }

        public string? ReadFunction { get; set; }
        public string? WriteFunction { get; set; }
    }

    public interface IGameHookProperty
    {
        GameHookMapperVariables MapperVariables { get; }
        GlossaryList? Glossary { get; }

        string Path { get; }
        string Type { get; }
        int? Length { get; }
        uint? Address { get; }

        int? Position { get; }

        string? Reference { get; }

        object? Value { get; set; }
        byte[]? Bytes { get; }
        byte[]? BytesFrozen { get; }

        bool Frozen { get; }

        string? Description { get; }

        bool IsReadOnly { get; }

        HashSet<string> FieldsChanged { get; }

        void ProcessLoop(IMemoryManager container);

        Task<byte[]> WriteValue(string value, bool? freeze);
        Task WriteBytes(byte[] bytes, bool? freeze);

        Task FreezeProperty(byte[] bytesFrozen);
        Task UnfreezeProperty();
    }


}
