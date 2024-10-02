using GameHook.Domain;
using GameHook.Domain.Implementations;
using GameHook.Domain.Interfaces;
using Jint;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace GameHook.Application
{
    public class GameHookInstance : IGameHookInstance
    {
        private IConfiguration Configuration { get; }
        private ILogger<GameHookInstance> Logger { get; }
        private ScriptConsole ScriptConsoleAdapter { get; }
        private CancellationTokenSource? ReadLoopToken { get; set; }
        private IMapperFilesystemProvider MapperFilesystemProvider { get; }
        private IEnumerable<MemoryAddressBlock>? BlocksToRead { get; set; }
        public List<IClientNotifier> ClientNotifiers { get; }
        public bool Initalized { get; private set; }
        public IGameHookDriver? Driver { get; private set; }
        public IGameHookMapper? Mapper { get; private set; }
        public IPlatformOptions? PlatformOptions { get; private set; }
        public IMemoryManager MemoryContainerManager { get; private set; }
        public Dictionary<string, object?> State { get; private set; }
        public Dictionary<string, object?> Variables { get; private set; }
        private Engine? GlobalScriptEngine { get; set; }

#if DEBUG
        private bool DebugOutputMemoryLayoutToFilesystem { get; set; } = false;
#endif

        public GameHookInstance(IConfiguration configuration, ILogger<GameHookInstance> logger, ScriptConsole scriptConsoleAdapter, IMapperFilesystemProvider provider, IEnumerable<IClientNotifier> clientNotifiers)
        {
            Configuration = configuration;
            Logger = logger;
            ScriptConsoleAdapter = scriptConsoleAdapter;
            MapperFilesystemProvider = provider;
            ClientNotifiers = clientNotifiers.ToList();

            MemoryContainerManager = new MemoryManager();
            State = new Dictionary<string, object?>();
            Variables = new Dictionary<string, object?>();
        }

        private async Task ResetState()
        {
            if (ReadLoopToken != null && ReadLoopToken.Token.CanBeCanceled)
            {
                ReadLoopToken.Cancel();
            }

            Initalized = false;
            ReadLoopToken = null;

            Driver = null;
            Mapper = null;
            PlatformOptions = null;
            BlocksToRead = null;

            GlobalScriptEngine = null;
            State = new Dictionary<string, object?>();
            Variables = new Dictionary<string, object?>();

            await ClientNotifiers.ForEachAsync(async x => await x.SendInstanceReset());
        }

        private async Task ReadLoop()
        {
            if (Configuration.GetRequiredValue("SHOW_READ_LOOP_STATISTICS").ToLower() == "true")
            {
                while (ReadLoopToken != null && ReadLoopToken.IsCancellationRequested == false)
                {
                    try
                    {
                        Stopwatch stopwatch = new();
                        stopwatch.Start();

                        await Read();

                        stopwatch.Stop();

                        var stateJson = JsonSerializer.Serialize(State);
                        var variablesJson = JsonSerializer.Serialize(Variables);
                        Logger.LogInformation($"Stopwatch took {stopwatch.ElapsedMilliseconds} ms.\nGlobal State: {stateJson}\nGlobal Variables: {variablesJson}");

                        await Task.Delay(1);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "An error occured when read looping the mapper.");

                        await ResetState();
                    }
                }
            }
            else
            {
                while (ReadLoopToken != null && ReadLoopToken.IsCancellationRequested == false)
                {
                    try
                    {
                        await Read();
                        await Task.Delay(1);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "An error occured when read looping the mapper.");

                        await ResetState();
                    }
                }
            }
        }

        private async Task Read()
        {
            if (Driver == null) throw new Exception("Driver is null.");
            if (PlatformOptions == null) throw new Exception("Platform options are null.");
            if (Mapper == null) throw new Exception("Mapper is null.");
            if (BlocksToRead == null) throw new Exception("BlocksToRead is null.");

            var driverResult = await Driver.ReadBytes(BlocksToRead);

            foreach (var result in driverResult)
            {
                MemoryContainerManager.DefaultNamespace.Fill(result.Key, result.Value);
            }

#if DEBUG
            if (MemoryContainerManager is not IStaticMemoryDriver && DebugOutputMemoryLayoutToFilesystem)
            {
                var memoryContainerPath = Path.GetFullPath(Path.Combine(BuildEnvironment.BinaryDirectoryGameHookFilePath, "..", "..", "..", "..", "..", "..", "GameHook.IntegrationTests", "Data", $"{Mapper.Metadata.Id}-0.json"));

                File.WriteAllText(memoryContainerPath, JsonSerializer.Serialize(driverResult));
                DebugOutputMemoryLayoutToFilesystem = false;
            }
#endif

            // Setup at start of loop
            foreach (var property in Mapper.Properties.Values)
            {
                property.FieldsChanged.Clear();
            }

            // Preprocessor
            if (Mapper.HasGlobalPreprocessor)
            {
                if (GlobalScriptEngine == null) throw new Exception("GlobalScriptEngine is null.");

                if (GlobalScriptEngine.Invoke("preprocessor").ToObject() as bool? == false)
                {
                    // The preprocessor returned false, which means we do not want to run anything this loop.
                    return;
                }
            }

            // Processor
            foreach (var property in Mapper.Properties.Values)
            {
                try
                {
                    property.ProcessLoop(MemoryContainerManager);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Property {property.Path} failed to run processor.");
                    throw new PropertyProcessException($"Property {property.Path} failed to run processor.", ex);
                }
            }

            // Postprocessor
            if (Mapper.HasGlobalPostprocessor)
            {
                if (GlobalScriptEngine == null) throw new Exception("GlobalScriptEngine is null.");

                GlobalScriptEngine.Evaluate("postprocessor").ToObject();
            }

            // Fields Changed
            var propertiesChanged = Mapper.Properties.Values.Where(x => x.FieldsChanged.Any()).ToArray();
            if (propertiesChanged.Length > 0)
            {
                try
                {
                    foreach (var notifier in ClientNotifiers)
                    {
                        await notifier.SendPropertiesChanged(propertiesChanged);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Could not send {propertiesChanged.Length} property change events.");
                    throw new PropertyProcessException($"Could not send {propertiesChanged.Length} property change events.", ex);
                }
            }
        }

        public async Task Load(IGameHookDriver driver, string mapperId)
        {
            try
            {
                await ResetState();

                Logger.LogDebug("Creating GameHook mapper instance...");

                Driver = driver;

                await Driver.EstablishConnection();

                // Load the mapper file.
                if (string.IsNullOrEmpty(mapperId))
                {
                    throw new ArgumentException("ID was NULL or empty.", nameof(mapperId));
                }

                // Get the file path from the filesystem provider.
                var mapperFile = MapperFilesystemProvider.MapperFiles.SingleOrDefault(x => x.Id == mapperId) ??
                                 throw new Exception($"Unable to determine a mapper with the ID of {mapperId}.");

                if (File.Exists(mapperFile.AbsolutePath) == false)
                {
                    throw new FileNotFoundException($"File was not found in the {mapperFile.Type} mapper folder.", mapperFile.DisplayName);
                }

                var mapperContents = await File.ReadAllTextAsync(mapperFile.AbsolutePath);
                if (mapperFile.AbsolutePath.EndsWith(".yml"))
                {
                    Mapper = GameHookMapperYamlFactory.ReadMapper(this, MapperFilesystemProvider, mapperFile.Id);
                }
                else if (mapperFile.AbsolutePath.EndsWith(".xml"))
                {
                    var scriptContentsAbsolutePath = mapperFile.AbsolutePath.Replace(".xml", ".js");
                    string? scriptContents = null;

                    if (File.Exists(scriptContentsAbsolutePath))
                    {
                        scriptContents = await File.ReadAllTextAsync(scriptContentsAbsolutePath);
                    }

                    Mapper = GameHookMapperXmlFactory.LoadMapperFromFile(this, mapperContents, scriptContents);
                }
                else
                {
                    throw new Exception($"Invalid extension for mapper.");
                }

                PlatformOptions = Mapper.Metadata.GamePlatform switch
                {
                    "NES" => new NES_PlatformOptions(),
                    "SNES" => new SNES_PlatformOptions(),
                    "GB" => new GB_PlatformOptions(),
                    "GBC" => new GBC_PlatformOptions(),
                    "GBA" => new GBA_PlatformOptions(),
                    "PSX" => new PSX_PlatformOptions(),
                    "NDS" => new NDS_PlatformOptions(),
                    _ => throw new Exception($"Unknown game platform {Mapper.Metadata.GamePlatform}.")
                };

                GlobalScriptEngine = new Engine().Execute(Mapper.GlobalScript ?? string.Empty);
                GlobalScriptEngine.SetValue("console", ScriptConsoleAdapter);
                GlobalScriptEngine.SetValue("state", State);
                GlobalScriptEngine.SetValue("variables", Variables);
                GlobalScriptEngine.SetValue("mapper", Mapper);
                GlobalScriptEngine.SetValue("memory", MemoryContainerManager);
                GlobalScriptEngine.SetValue("driver", Driver);

                // Calculate the blocks to read from the mapper memory addresses.
                BlocksToRead = PlatformOptions.Ranges.ToList();

                await Read();

                Initalized = true;

                await ClientNotifiers.ForEachAsync(async x => await x.SendMapperLoaded(Mapper));

                // Start the read loop once successfully running once.
                ReadLoopToken = new CancellationTokenSource();
                _ = Task.Run(ReadLoop, ReadLoopToken.Token);

                Logger.LogInformation($"Loaded mapper for {Mapper.Metadata.GameName} ({Mapper.Metadata.Id}).");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occured when loading the mapper.");

                await ResetState();

                throw;
            }
        }

        public object? Evalulate(string function, object? x, object? y)
        {
            if (GlobalScriptEngine == null) throw new Exception("GlobalScriptEngine is null.");

            try
            {
                return GlobalScriptEngine.SetValue("x", x).SetValue("y", y).Evaluate(function).ToObject();
            }
            catch (Exception ex)
            {
                Logger.LogError("Javascript evalulate engine exception.", ex);

                throw;
            }
        }
    }
}
