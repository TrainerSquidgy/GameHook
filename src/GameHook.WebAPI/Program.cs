using GameHook.WebAPI;
using Serilog;

public class Program
{
    public static void Main()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        try
        {
            var defaultLogPath = "GameHook.log";

            // Clear existing default log file if it exists.
            if (File.Exists(defaultLogPath))
            {
                File.WriteAllText(defaultLogPath, string.Empty);
            }

            Log.Logger = new LoggerConfiguration()
                                .WriteTo.Console()
                                .WriteTo.File(defaultLogPath)
                                .CreateBootstrapLogger();

            Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(x => x.UseStartup<Startup>())
                    .ConfigureAppConfiguration(x =>
                    {
                        // Add a custom appsettings.user.json file if
                        // the user wants to override their settings.

                        x.AddJsonStream(EmbededResources.appsettings_json);
                        x.AddJsonFile(BuildEnvironment.ConfigurationDirectoryAppsettingsFilePath, true, false);
                        x.AddJsonFile(BuildEnvironment.BinaryDirectoryGameHookFilePath, true, false);

                        if (BuildEnvironment.IsTestingBuild)
                        {
                            x.AddJsonFile("appsettings.Development.json", true);
                        }
                    })
                    .UseSerilog((context, services, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
                    .Build()
                    .Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "GameHook startup failed!");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}