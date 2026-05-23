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
using Scalar.AspNetCore;
using LiteDB;

namespace SoundBridge.App;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var serilogConfig = new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json")
                           .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
                           .AddEnvironmentVariables()
                           .Build();

        Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(serilogConfig)
                    .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            if (args.Contains("--service"))
                builder.Host.UseWindowsService();

            var sbSection = builder.Configuration.GetSection("SoundBridge");
            var webServerHost = sbSection["WebServerHost"] ?? "0.0.0.0";
            var webServerPort = int.Parse(sbSection["WebServerPort"] ?? "5000");
            builder.WebHost.UseUrls($"http://{webServerHost}:{webServerPort}");

            var services = builder.Services;

            services.Configure<SoundBridgeOptions>(sbSection);

            services.AddSingleton<LiteDatabase>(_ => new LiteDatabase("data/soundbridge.db"));
            services.AddSingleton<ILocalLibraryStore, LocalLibraryStore>();

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<SoundBridgeOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<UdnManager>>();
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
                var logger = sp.GetRequiredService<ILogger<SoundBridgeContentDirectory>>();
                return new SoundBridgeContentDirectory(device, resolver, logger);
            });

            services.AddSingleton<DvProviderUpnpOrgConnectionManager1>(sp =>
            {
                var device = sp.GetRequiredService<DvDevice>();
                return new SoundBridgeConnectionManager(device);
            });

            services.AddHostedService<UpnpDeviceService>();
            services.AddHostedService<ContentDirectoryService>();

            services.AddControllers();
            services.AddOpenApi();

            var app = builder.Build();

            app.MapOpenApi();
            app.MapScalarApiReference();
            app.MapControllers();

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
}
