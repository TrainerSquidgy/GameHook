using GameHook.Domain;
using GameHook.Domain.Interfaces;
using GameHook.Domain.Preprocessors;
using NCalc;

namespace GameHook.Application.GameHookProperties
{
    public abstract class BaseProperty : IGameHookProperty
    {
        public BaseProperty(IGameHookInstance instance, GameHookMapperVariables mapperVariables)
        {
            Instance = instance;
            MapperVariables = mapperVariables;
            Path = MapperVariables.Path;
            Type = MapperVariables.Type;
            Length = MapperVariables.Length;
            SetAddress(MapperVariables.Address);
            Position = MapperVariables.Position;
            Reference = MapperVariables.Reference;
            Value = MapperVariables.StaticValue;
            Description = MapperVariables.Description;
        }

        private MemoryAddress? _address { get; set; }
        private object? _value { get; set; }
        private byte[]? _bytes { get; set; }

        private bool IsAddressMathSolved { get; set; }
        private bool ShouldRunReferenceTransformer
        {
            get { return (Type == "bit" || Type == "bool" || Type == "int" || Type == "uint") && Glossary != null; }
        }

        protected abstract object? ToValue(byte[] bytes);
        protected abstract byte[] FromValue(string value);

        protected IGameHookInstance Instance { get; }

        public GameHookMapperVariables MapperVariables { get; }

        public GlossaryList? Glossary
        {
            get
            {
                if (Instance.Mapper == null) throw new Exception("Instance.Mapper is NULL.");

                if (string.IsNullOrEmpty(MapperVariables.Reference) == false)
                {
                    return Instance.Mapper.Glossary[MapperVariables.Reference] ??
                           throw new MapperInitException($"Unable to load reference map '{MapperVariables.Reference}'. It was not found in the references section.");
                }

                return null;
            }
        }

        public HashSet<string> FieldsChanged { get; } = new();

        public string Path { get; }
        public string? Description { get; }
        public string Type { get; }
        public int? Length { get; }
        public int? Position { get; }
        public string? Reference { get; }

        public bool IsReadOnly => Address == null;

        public string? AddressExpression { get; private set; }

        public MemoryAddress? Address
        {
            get => _address;
            set
            {
                if (_address == value) return;

                FieldsChanged.Add("address");
                _address = value;
            }
        }

        public object? Value
        {
            get => _value;
            set
            {
                if (_value == value) return;

                FieldsChanged.Add("value");
                _value = value;
            }
        }

        public byte[]? Bytes
        {
            get => _bytes;
            private set
            {
                if (_bytes == value) return;

                FieldsChanged.Add("bytes");
                _bytes = value;
            }
        }

        public byte[]? BytesFrozen { get; private set; }
        public bool Frozen => BytesFrozen != null;

        public void ProcessLoop(IMemoryManager memoryContainer)
        {
            if (Instance.Mapper == null) { throw new Exception("Instance.Mapper is NULL."); }

            MemoryAddress? address = Address;
            byte[]? previousBytes = Bytes;
            byte[]? bytes = null;
            object? value;

            // Yaml Preprocessors
            if (Instance.Mapper.Format == MapperFormats.YAML && string.IsNullOrEmpty(MapperVariables.YamlPreprocessor) == false)
            {
                if (MapperVariables.YamlPreprocessor.Contains("data_block_a245dcac"))
                {
                    try
                    {
                        var baseAddress = address ?? throw new Exception($"Property {Path} does not have a base address.");

                        var structureIndex = MapperVariables.YamlPreprocessor.GetIntParameterFromFunction(0);
                        var offset = MapperVariables.YamlPreprocessor.GetIntParameterFromFunction(1);

                        var preprocessorResult = Preprocessor_a245dcac.Read(memoryContainer, baseAddress, structureIndex, offset, MapperVariables.Length ?? 0);
                        address = preprocessorResult.Address;
                        bytes = preprocessorResult.DecryptedData;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to process preprocessor {MapperVariables.YamlPreprocessor}.", ex);
                    }
                }
                else if (MapperVariables.YamlPreprocessor.Contains("dma_967d10cc"))
                {
                    try
                    {
                        var memoryAddress = MapperVariables.YamlPreprocessor.GetMemoryAddressFromFunction(0);
                        var offset = MapperVariables.YamlPreprocessor.GetIntParameterFromFunction(1);

                        var dmaAddress = Preprocessor_967d10cc.Read(memoryContainer, memoryAddress, size: 4, offset);
                        if (dmaAddress == null) { return; }

                        address = dmaAddress;
                        bytes = null;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to process preprocessor {MapperVariables.YamlPreprocessor}.", ex);
                    }
                }
            }

            if (MapperVariables.StaticValue != null)
            {
                Value = MapperVariables.StaticValue;

                return;
            }

            if (Length == null)
            {
                throw new Exception("Length is NULL.");
            }

            if (string.IsNullOrEmpty(AddressExpression) == false && IsAddressMathSolved == false)
            {
                if (AddressMath.TrySolve(AddressExpression, Instance.Variables, out var solvedAddress))
                {
                    address = solvedAddress;
                }
                else
                {
                    // TODO: Write a log entry here.
                }
            }

            if (address == null && bytes == null)
            {
                // There is nothing to do for this property, as it does not have an address or bytes.
                // Hopefully a postprocessor will pick it up and set it's value!
                return;
            }

            if (bytes == null)
            {
                if (string.IsNullOrEmpty(MapperVariables.MemoryContainer))
                {
                    bytes = memoryContainer.DefaultNamespace.GetBytes(address ?? 0x00, Length ?? 1).Data;
                }
                else
                {
                    bytes = memoryContainer.Namespaces[MapperVariables.MemoryContainer].GetBytes(address ?? 0x00, Length ?? 0).Data;
                }
            }

            if (previousBytes != null && bytes != null && previousBytes.SequenceEqual(bytes))
            {
                // Fast path - if the bytes match, then we can assume the property has not been
                // updated since last poll.

                // Do nothing, we don't need to calculate the new value as
                // the bytes are the same.

                return;
            }

            if (bytes == null)
            {
                throw new Exception(
                    $"Unable to retrieve bytes for property '{Path}' at address {Address?.ToHexdecimalString()}. Is the address within the drivers' memory address block ranges?");
            }

            value = ToValue(bytes);

            if (string.IsNullOrEmpty(MapperVariables.ReadFunction) == false)
            {
                value = Instance.Evalulate(MapperVariables.ReadFunction, value, null);
            }

            // Yaml Postprocessor
            if (Instance.Mapper.Format == MapperFormats.YAML && string.IsNullOrEmpty(MapperVariables.YamlPostprocessorReader) == false)
            {
                var postprocessorExpression = new Expression(MapperVariables.YamlPostprocessorReader);
                postprocessorExpression.Parameters["x"] = value;

                postprocessorExpression.EvaluateFunction += delegate (string name, FunctionArgs args)
                {
                    if (name == "BitRange")
                        args.Result = NCalcFunctions.ReadBitRange((int)args.Parameters[0].Evaluate(),
                            (int)args.Parameters[1].Evaluate(), (int)args.Parameters[2].Evaluate());
                };

                value = Convert.ToInt32(postprocessorExpression.Evaluate());
            }

            // Reference lookup
            if (ShouldRunReferenceTransformer)
            {
                if (Glossary == null)
                {
                    throw new Exception("Glossary is NULL.");
                }

                value = Glossary.GetSingleOrDefaultByKey(Convert.ToUInt64(value))?.Value;
            }

            Address = address;
            Bytes = bytes;
            Value = value;
        }

        public void SetAddress(string? addressExpression)
        {
            if (string.IsNullOrEmpty(addressExpression))
            {
                return;
            }

            AddressExpression = addressExpression;

            IsAddressMathSolved = AddressMath.TrySolve(addressExpression, new Dictionary<string, object?>(), out var address);

            if (IsAddressMathSolved)
            {
                Address = address;
            }
        }

        public async Task<byte[]> WriteValue(string value, bool? freeze)
        {
            byte[] bytes;

            if (ShouldRunReferenceTransformer)
            {
                if (Glossary == null)
                {
                    throw new Exception("Glossary is NULL.");
                }

                bytes = BitConverter.GetBytes(Glossary.GetSingleByValue(value).Key);
            }
            else
            {
                bytes = FromValue(value);
            }

            /*
            if (string.IsNullOrEmpty(MapperVariables.WriteFunction) == false)
            {
                var result = Instance.Evalulate(MapperVariables.WriteFunction, bytes, Bytes);
                bytes = result as byte[] ?? throw new Exception("Write expression did not return a byte array.");
            }
            */

            await WriteBytes(bytes, freeze);

            return bytes;
        }

        public async Task WriteBytes(byte[] bytes, bool? freeze)
        {
            if (Instance == null) throw new Exception("Instance is NULL.");
            if (Instance.Driver == null) throw new Exception("Driver is NULL.");
            if (Address == null) throw new Exception($"{Path} does not have an address. Cannot write data to an empty address.");

            await Instance.Driver.WriteBytes((MemoryAddress)Address, bytes);

            if (freeze == true) await FreezeProperty(bytes);
            else if (freeze == false) await UnfreezeProperty();
        }

        public async Task FreezeProperty(byte[] bytesFrozen)
        {
            FieldsChanged.Add("frozen");

            BytesFrozen = bytesFrozen;

            var propertyArray = new IGameHookProperty[] { this };
            foreach (var notifier in Instance.ClientNotifiers)
            {
                await notifier.SendPropertiesChanged(propertyArray);
            }
        }

        public async Task UnfreezeProperty()
        {
            FieldsChanged.Remove("frozen");

            BytesFrozen = null;

            var propertyArray = new IGameHookProperty[] { this };
            foreach (var notifier in Instance.ClientNotifiers)
            {
                await notifier.SendPropertiesChanged(propertyArray);
            }
        }
    }
}