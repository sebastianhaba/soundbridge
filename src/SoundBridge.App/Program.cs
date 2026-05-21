using Microsoft.Extensions.Options;
using OpenHome.Net.Core;
using OhNetLibrary = OpenHome.Net.Core.Library;
using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using Serilog;
using SoundBridge.App.Configuration;
using SoundBridge.App.Core;
using SoundBridge.App.Library;
using SoundBridge.App.Providers;
using SoundBridge.App.Services;

namespace SoundBridge.App;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json")
                           .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
                           .Build();

        Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

        try
        {
            var sbSection = configuration.GetSection("SoundBridge");
            var mediaHost = sbSection["MediaHost"] ?? "localhost";
            var mediaPort = int.Parse(sbSection["MediaPort"] ?? "5000");

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://{mediaHost}:{mediaPort}");
            builder.Host.UseSerilog();

            if (args.Contains("--service"))
                builder.Host.UseWindowsService();

            var services = builder.Services;

            services.Configure<SoundBridgeOptions>(sbSection);

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<SoundBridgeOptions>>().Value;
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UdnManager>>();
                return new UdnManager(opts.UdnFilePath, logger);
            });

            services.AddSingleton<OhNetLibrary>(_ =>
            {
                var initParams = new InitParams();
                var library = OhNetLibrary.Create(initParams);
                library.StartDv();
                return library;
            });

            services.AddSingleton<DvDevice>(sp =>
            {
                sp.GetRequiredService<OhNetLibrary>();
                var udnManager = sp.GetRequiredService<UdnManager>();
                var opts = sp.GetRequiredService<IOptions<SoundBridgeOptions>>().Value;
                var device = new DvDeviceStandard(udnManager.GetOrCreateUdn());
                device.SetAttribute("Upnp.Domain", "schemas-upnp-org");
                device.SetAttribute("Upnp.Type", "MediaServer");
                device.SetAttribute("Upnp.Version", "1");
                device.SetAttribute("Upnp.FriendlyName", opts.FriendlyName);
                device.SetAttribute("Upnp.Manufacturer", opts.Manufacturer);
                device.SetAttribute("Upnp.ModelName", "SoundBridge");
                device.SetAttribute("Upnp.ModelNumber", "1.0");
                return device;
            });

            services.AddSingleton<IContentResolver, LocalLibraryResolver>();

            services.AddSingleton<DvProviderUpnpOrgContentDirectory1>(sp =>
            {
                var device = sp.GetRequiredService<DvDevice>();
                var resolver = sp.GetRequiredService<IContentResolver>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SoundBridgeContentDirectory>>();
                return new SoundBridgeContentDirectory(device, resolver, logger);
            });

            services.AddSingleton<DvProviderUpnpOrgConnectionManager1>(sp =>
            {
                var device = sp.GetRequiredService<DvDevice>();
                return new SoundBridgeConnectionManager(device);
            });

            services.AddHostedService<UpnpDeviceService>();
            services.AddHostedService<ContentDirectoryService>();

            var app = builder.Build();

            app.MapGet("/media/{**path}", async (string? path, IContentResolver resolver, HttpContext http) =>
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    http.Response.StatusCode = 400;
                    await http.Response.WriteAsync("Missing path");
                    return;
                }

                try
                {
                    var (fullPath, _) = resolver.ResolveToPath(path);

                    if (!PathValidator.IsAudioExtension(fullPath))
                    {
                        http.Response.StatusCode = 403;
                        await http.Response.WriteAsync("Forbidden file type");
                        return;
                    }

                    if (!File.Exists(fullPath))
                    {
                        http.Response.StatusCode = 404;
                        await http.Response.WriteAsync("File not found");
                        return;
                    }

                    var mime = GetMimeType(fullPath);
                    await Results.File(fullPath, mime).ExecuteAsync(http);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Media request failed for path: {Path}", path);
                    http.Response.StatusCode = 404;
                    await http.Response.WriteAsync("Not found");
                }
            });

            Log.Information("SoundBridge starting");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            _ => "application/octet-stream"
        };
    }
}
