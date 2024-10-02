using GameHook.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GameHook.Domain.Infrastructure
{
#pragma warning disable IDE1006 // Naming Styles
    class LatestCommit
    {
        public string sha { get; set; } = string.Empty;
    }
#pragma warning restore IDE1006 // Naming Styles

    class MapperData
    {
        public DateTime? LastCheckedDate { get; set; }
        public string? LastLocalVersion { get; set; }
        public string? LastLocalCommit { get; set; }
    }

    public class MapperUpdateManager : IMapperUpdateManager
    {
        private ILogger<MapperUpdateManager> Logger { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private MapperData MapperData { get; }
        private bool AutomaticMapperUpdates { get; }
        private int CheckForMapperUpdatesMinutes { get; }

        private const string GithubMapperUrl = "https://github.com/gamehook-io/mappers";
        private const string LatestCommitUrl = "https://api.github.com/repos/gamehook-io/mappers/commits/main";
        private const string MapperLatestZipUrl = "https://github.com/gamehook-io/mappers/archive/refs/heads/main.zip";

        public MapperUpdateManager(ILogger<MapperUpdateManager> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            Logger = logger;
            HttpClientFactory = httpClientFactory;

            if (BuildEnvironment.IsDebug && configuration["ALTERNATIVE_MAPPER_DIRECTORY"] != null)
            {
                Logger.LogInformation("Using alternative mapper directory, not performing automatic updates.");
                AutomaticMapperUpdates = false;
            }
            else
            {
                AutomaticMapperUpdates = bool.Parse(configuration.GetRequiredValue("AUTOMATIC_MAPPER_UPDATES"));

            }

            CheckForMapperUpdatesMinutes = int.Parse(configuration.GetRequiredValue("CHECK_FOR_MAPPER_UPDATES_MINUTES"));

            if (Directory.Exists(BuildEnvironment.ConfigurationDirectory) == false)
            {
                Logger.LogInformation("Creating configuration directory for GameHook.");

                Directory.CreateDirectory(BuildEnvironment.ConfigurationDirectory);
            }

            if (File.Exists(MapperDataFilepath))
            {
                MapperData = JsonSerializer.Deserialize<MapperData>(File.ReadAllText(MapperDataFilepath)) ?? throw new Exception($"Could not deserialize mapper data from {MapperDataFilepath}");
            }
            else
            {
                MapperData = new MapperData();
            }
        }

        private async Task WriteMapperData() => await File.WriteAllTextAsync(MapperDataFilepath, JsonSerializer.Serialize(MapperData));

        private string MapperDataFilepath => Path.Combine(BuildEnvironment.ConfigurationDirectory, "mappers.json");
        private string MapperLocalDirectory => Path.Combine(BuildEnvironment.ConfigurationDirectory, "Mappers");

        private string MapperZipTemporaryFilepath => Path.Combine(BuildEnvironment.ConfigurationDirectory, $"gamehook_mappers_main_temp.zip");
        private string MapperTemporaryExtractionDirectory => Path.Combine(BuildEnvironment.ConfigurationDirectory, $"gamehook_mappers_temp\\");

        private void CleanupTemporaryFiles()
        {
            if (File.Exists(MapperZipTemporaryFilepath))
            {
                File.Delete(MapperZipTemporaryFilepath);
            }

            if (Directory.Exists(MapperTemporaryExtractionDirectory))
            {
                Directory.Delete(MapperTemporaryExtractionDirectory, true);
            }
        }

        private async Task DownloadLatestMappers(HttpClient httpClient)
        {
            try
            {
                CleanupTemporaryFiles();

                // Download the ZIP from Github.
                var bytes = await httpClient.GetByteArrayAsync(MapperLatestZipUrl);
                File.WriteAllBytes(MapperZipTemporaryFilepath, bytes);

                // Extract to the temporary directory.
                using var zout = ZipFile.OpenRead(MapperZipTemporaryFilepath);
                zout.ExtractToDirectory(MapperTemporaryExtractionDirectory);

                if (Directory.Exists(MapperLocalDirectory))
                {
                    Directory.Delete(MapperLocalDirectory, true);
                }

                // Move from inside of the temporary directory into the main mapper folder.
                Directory.Move(Path.Combine(MapperTemporaryExtractionDirectory, "mappers-main"), MapperLocalDirectory);
            }
            finally
            {
                CleanupTemporaryFiles();
            }
        }

        public async Task<bool> CheckForUpdates()
        {
            try
            {
                if (AutomaticMapperUpdates == false)
                {
                    Logger.LogInformation("Skipping update checks due to being disabled in configuration.");

                    return false;
                }
                else if (CheckForMapperUpdatesMinutes == 0 ||
                         (MapperData.LastCheckedDate.HasValue == false || MapperData.LastCheckedDate.HasValue && (DateTime.UtcNow - MapperData.LastCheckedDate.Value).Minutes > CheckForMapperUpdatesMinutes) ||
                         MapperData.LastLocalVersion != BuildEnvironment.AssemblyProductVersion)
                {
                    var httpClient = HttpClientFactory.CreateClient();
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GameHook", BuildEnvironment.AssemblyProductVersion));

                    var latestCommitJson = await httpClient.GetStringAsync(LatestCommitUrl);
                    var latestCommit = JsonSerializer.Deserialize<LatestCommit>(latestCommitJson)?.sha;
                    var latestVersion = BuildEnvironment.AssemblyProductVersion;

                    if (MapperData.LastLocalCommit != latestCommit)
                    {
                        Logger.LogInformation($"Downloading mappers updates from our source repository {GithubMapperUrl}.");

                        await DownloadLatestMappers(httpClient);

                        MapperData.LastLocalCommit = latestCommit;
                    }

                    Logger.LogInformation("GameHook will periodically check for mapper updates and download them.");
                    Logger.LogInformation("If you do not want this, please disable it in the configuration.");

                    MapperData.LastLocalVersion = latestVersion;
                    MapperData.LastCheckedDate = DateTime.UtcNow;

                    await WriteMapperData();

                    return true;
                }
                else
                {
                    Logger.LogDebug("Not enough time has passed before doing another update check -- skipping.");

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not perform update check for mappers.");

                return false;
            }
        }
    }
}
