using System.Reflection;

public static class BuildEnvironment
{
    // 0.0.0.0
    public static string AssemblyVersion
    {
        get
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var attributes = entryAssembly?.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return ((AssemblyFileVersionAttribute)attributes[0]).Version;
            }

            throw new Exception("Cannot determine application AssemblyVersion.");
        }
    }

    // 0.0.0.0+hash
    public static string AssemblyProductVersion
    {
        get
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var attributes = entryAssembly?.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            }

            throw new Exception("Cannot determine application AssemblyProductVersion.");
        }
    }

    private static string BinaryDirectory =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        throw new Exception("Could not determine the binary directory.");

    public static string ConfigurationDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameHook");

    // TODO: DEPRECATED FEATURE - Remove this code later. 5/19/2023
    public static string MapperUserSettingsDirectory => Path.Combine(ConfigurationDirectory, "MapperUserSettings");

    public static string ConfigurationDirectoryAppsettingsFilePath =>
        Path.Combine(ConfigurationDirectory, "appsettings.json");

    public static string ConfigurationDirectoryWpfConfigFilePath =>
        Path.Combine(ConfigurationDirectory, "gamehook.wpf.config");

    public static string BinaryDirectoryGameHookFilePath => Path.Combine(BinaryDirectory, "GameHook.json");

    // TODO: DEPRECATED FEATURE - Remove this code later. 5/19/2023
    public static string ConfigurationDirectoryUiBuilderScreenDirectory =>
        Path.Combine(ConfigurationDirectory, "UiBuilderScreens");


#if DEBUG
    public static bool IsDebug => true;
    public static bool IsTestingBuild => true;
#else
    public static bool IsDebug = false;
    public static bool IsTestingBuild => AssemblyVersion == "0.0.0.0";
#endif
}