using GameHook.Domain.DTOs;
using GameHook.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameHook.Domain.Infrastructure
{
    static class MapperFilesystemHelper
    {
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null) throw new ArgumentNullException("extensions");

            IEnumerable<FileInfo> files = dir.EnumerateFiles("*", SearchOption.AllDirectories);
            return files.Where(f => extensions.Contains(f.Extension));
        }
    }

    public class MapperFilesystemProvider : IMapperFilesystemProvider
    {
        private ILogger<MapperFilesystemProvider> Logger { get; }

        public string MapperFolder { get; }
        public string? BinaryMapperFolder { get; }

        public IEnumerable<MapperFilesystemDTO> MapperFiles { get; private set; } = new List<MapperFilesystemDTO>();

        public MapperFilesystemProvider(ILogger<MapperFilesystemProvider> logger, IConfiguration configuration)
        {
            Logger = logger;

            MapperFolder = Path.Combine(BuildEnvironment.ConfigurationDirectory, "Mappers");
            BinaryMapperFolder = GetCustomMappersFolder();

            if (BuildEnvironment.IsDebug)
            {
                var alternativeMapperDirectory = configuration["ALTERNATIVE_MAPPER_DIRECTORY"];
                if (string.IsNullOrEmpty(alternativeMapperDirectory) == false)
                {
                    MapperFolder = alternativeMapperDirectory;
                }
            }

            RefreshMapperFiles();
        }

        public void RefreshMapperFiles()
        {
            MapperFiles = GetAllMapperFiles();
        }

        private string? GetCustomMappersFolder()
        {
            var processPath = Path.GetDirectoryName(Environment.ProcessPath);
            if (processPath == null)
            {
                Logger.LogWarning("Unable to determine the process path for executable. Cannot determine the custom mapper folder.");
                return null;
            }

            var folder = Path.Combine(processPath, "Mappers");
            if (Directory.Exists(folder) == false)
            {
                return null;
            }

            return folder;
        }

        private string GetId(MapperFilesystemTypes type, string directory, string filePath)
        {
            if (directory.Contains(".."))
            {
                throw new Exception("Invalid characters in file path.");
            }

            return $"{type}_{filePath.Replace(directory, string.Empty)[1..].Replace(".", "_").Replace("\\", "_")}".ToLower();
        }

        private string GetDisplayName(string directory, string filePath)
        {
            return filePath.Replace(directory, string.Empty)[1..].Replace("\\", " - ");
        }

        /// <summary>
        /// We replace the base path with an empty string
        /// as to not expose the absolute path of the filesystem.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<MapperFilesystemDTO> GetAllMapperFiles()
        {
            if (MapperFolder.Contains(".."))
            {
                throw new Exception("Invalid characters in mapper folder path.");
            }

            var mappers = new DirectoryInfo(MapperFolder)
                .GetFilesByExtensions(".xml", ".yml")
                .Select(x => new MapperFilesystemDTO()
                {
                    Id = GetId(MapperFilesystemTypes.Official, MapperFolder, x.FullName),
                    Type = MapperFilesystemTypes.Official,
                    AbsolutePath = x.FullName,
                    DisplayName = $"{GetDisplayName(MapperFolder, x.FullName)}"
                })
                .ToList();

            if (BinaryMapperFolder != null)
            {
                if (BinaryMapperFolder.Contains('.') || BinaryMapperFolder.Contains(".."))
                {
                    throw new Exception("Invalid characters in mapper folder path.");
                }

                var localMappers = new DirectoryInfo(BinaryMapperFolder)
                    .GetFilesByExtensions(".xml", ".yml")
                    .Select(x => new MapperFilesystemDTO()
                    {
                        Id = GetId(MapperFilesystemTypes.Local, BinaryMapperFolder, x.FullName),
                        Type = MapperFilesystemTypes.Local,
                        AbsolutePath = x.FullName,
                        DisplayName = $"(Local) {GetDisplayName(BinaryMapperFolder, x.FullName)}"
                    })
                    .ToList();

                mappers.AddRange(localMappers);
            }

            return mappers;
        }
    }
}
