using GameHook.Domain.DTOs;
using GameHook.Domain.Interfaces;

namespace GameHook.WebAPI.ClientNotifiers
{
    public class OutputPropertiesToFilesystem : IClientNotifier
    {
        private static string OutputPropertiesDirectory => Path.Combine(BuildEnvironment.ConfigurationDirectory, "OutputProperties");

        private static string FormatFilename(string path, string extension)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception($"Filename output filename is NULL.");
            }

            if (path.Contains("..") || path.Contains(":") || path.Contains("\\"))
            {
                throw new Exception($"File name {path} contains invalid characters.");
            }

            return path.Replace(".", "_") + extension;
        }

        private async Task OutputPropertyToFilesystem(IGameHookProperty property)
        {
            var filename = FormatFilename(property.Path, ".txt");
            var value = $"{property.Value?.ToString()}";

            // Convert special types.
            if (property.Value != null && (property.Type == "bit" || property.Type == "bool"))
            {
                value = (bool)property.Value == true ? "1" : "0";
            }

            await File.WriteAllTextAsync(Path.Combine(OutputPropertiesDirectory, filename), value);
        }

        public Task SendGameHookError(ProblemDetailsForClientDTO _) => Task.CompletedTask;

        public async Task SendInstanceReset()
        {
            if (Directory.Exists(OutputPropertiesDirectory))
            {
                Directory.Delete(OutputPropertiesDirectory, true);
            }

            Directory.CreateDirectory(OutputPropertiesDirectory);

            await Task.CompletedTask;
        }

        public async Task SendMapperLoaded(IGameHookMapper mapper)
        {
            Directory.CreateDirectory(OutputPropertiesDirectory);

            foreach (var property in mapper.Properties.Values)
            {
                await OutputPropertyToFilesystem(property);
            }
        }

        public Task SendDriverError(ProblemDetailsForClientDTO _) => Task.CompletedTask;

        public async Task SendPropertiesChanged(IEnumerable<IGameHookProperty> properties)
        {
            foreach (var property in properties)
            {
                if (property.FieldsChanged.Contains("value"))
                {
                    await OutputPropertyToFilesystem(property);
                }
            }
        }

        public Task SendUiBuilderScreenSaved(Guid id) => Task.CompletedTask;
    }
}
