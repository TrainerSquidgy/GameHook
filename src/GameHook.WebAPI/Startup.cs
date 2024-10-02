using GameHook.Application;
using GameHook.Domain;
using GameHook.Domain.Drivers;
using GameHook.Domain.Infrastructure;
using GameHook.Domain.Interfaces;
using GameHook.WebAPI.ClientNotifiers;
using GameHook.WebAPI.Hubs;
using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;

namespace GameHook.WebAPI
{
    class AppSettings
    {
        public string Urls { get; init; } = string.Empty;
        public bool OUTPUT_ALL_PROPERTIES_TO_FILESYSTEM { get; init; }
    }

    public class Startup
    {
        private AppSettings AppSettings { get; }

        public Startup(IConfiguration configuration)
        {
            AppSettings = configuration.Get<AppSettings>() ?? throw new Exception("Unable to bind application settings to AppSettings.");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddCors();

            // Add Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(x =>
            {
                x.DocumentFilter<DefaultSwashbuckleFilter>();

                x.EnableAnnotations();

                // Use method name as operationId
                x.CustomOperationIds(apiDesc =>
                {
                    return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null;
                });

                x.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "GameHook API",
                    Contact = new OpenApiContact
                    {
                        Name = "GameHook",
                        Url = new Uri("https://gamehook.io/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "GNU Affero General Public License v3.0",
                        Url = new Uri("https://github.com/gamehook-io/gamehook/blob/main/LICENSE.txt")
                    }
                });
            });

            // Add Web API
            services
                .AddControllers()
                .AddApplicationPart(typeof(Program).Assembly)
                .AddControllersAsServices()
                .AddProblemDetailsConventions()
                .AddJsonOptions(x => x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

            services.AddSignalR();

            services.AddProblemDetails((options) =>
            {
                options.ShouldLogUnhandledException = (ctx, e, d) => true;

                options.IncludeExceptionDetails = (ctx, ex) =>
                {
                    var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
                    return env.IsDevelopment();
                };
            });

            // Register application classes.
            services.AddSingleton<DriverOptions>();
            services.AddSingleton<IMapperFilesystemProvider, MapperFilesystemProvider>();
            services.AddSingleton<IMapperUpdateManager, MapperUpdateManager>();
            services.AddSingleton<IBizhawkMemoryMapDriver, BizhawkMemoryMapDriver>();
            services.AddSingleton<IRetroArchUdpPollingDriver, RetroArchUdpPollingDriver>();
            services.AddSingleton<IStaticMemoryDriver, StaticMemoryDriver>();
            services.AddSingleton<GameHookInstance>();
            services.AddSingleton<ScriptConsole>();
            services.AddSingleton<IClientNotifier, WebSocketClientNotifier>();

            if (AppSettings.OUTPUT_ALL_PROPERTIES_TO_FILESYSTEM)
            {
                services.AddSingleton<IClientNotifier, OutputPropertiesToFilesystem>();
            }
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> logger, IConfiguration configuration, IMapperUpdateManager updateManager)
        {
            if (BuildEnvironment.IsTestingBuild)
            {
                logger.LogWarning("WARNING: This is a debug build for testing!");
                logger.LogWarning("Please upgrade to the latest stable release.");
            }
            else
            {
                logger.LogInformation($"Starting GameHook version {BuildEnvironment.AssemblyProductVersion}.");
            }

            Directory.CreateDirectory(BuildEnvironment.ConfigurationDirectory);

            // TODO: DEPRECATED FEATURE - Remove this code later. 5/19/2023
            if (Directory.Exists(BuildEnvironment.MapperUserSettingsDirectory))
            {
                Directory.Delete(BuildEnvironment.MapperUserSettingsDirectory, true);
            }

            // TODO: DEPRECATED FEATURE - Remove this code later. 5/19/2023
            if (Directory.Exists(BuildEnvironment.ConfigurationDirectoryUiBuilderScreenDirectory))
            {
                Directory.Delete(BuildEnvironment.ConfigurationDirectoryUiBuilderScreenDirectory, true);
            }

            updateManager.CheckForUpdates().GetAwaiter().GetResult();

            app.UseCors(x =>
            {
                x.SetIsOriginAllowed(x => true);
                x.AllowAnyMethod();
                x.AllowAnyHeader();
                x.AllowCredentials();
            });

            // Use Swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseSerilogRequestLogging();
            app.UseProblemDetails();

            app.UseRouting();

            if (BuildEnvironment.IsDebug)
            {
                app.UseStaticFiles();
            }

            app.UseEndpoints(x =>
            {
                if (BuildEnvironment.IsDebug)
                {
                    x.MapGet("/", () => Results.Redirect("index.html", false));
                }
                else
                {
                    x.MapGet("/", () => Results.File(EmbededResources.index_html, contentType: "text/html"));
                    x.MapGet("/favicon.ico", () => Results.File(EmbededResources.favicon_ico, contentType: "image/x-icon"));
                    x.MapGet("/site.css", () => Results.File(EmbededResources.site_css, contentType: "text/css"));
                    x.MapGet("/dist/gameHookMapperClient.js", () => Results.File(EmbededResources.dist_gameHookMapperClient_js, contentType: "application/javascript"));
                }

                x.MapControllers();

                x.MapHub<UpdateHub>("/updates");
            });

            logger.LogInformation("GameHook is now online.");
            logger.LogInformation($"UI accessible via {string.Join(", ", AppSettings.Urls)}");

            if (AppSettings.OUTPUT_ALL_PROPERTIES_TO_FILESYSTEM)
            {
                logger.LogInformation("Outputting all properties to the filesystem.");
            }
        }
    }
}