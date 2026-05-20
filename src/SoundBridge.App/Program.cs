using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenHome.Net.Core;
using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using Serilog;
using SoundBridge.App.Configuration;
using SoundBridge.App.Core;
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
            var builder = Host.CreateDefaultBuilder(args);
            builder.UseSerilog();

            if (args.Contains("--service"))
                builder.UseWindowsService();

            builder.ConfigureServices((ctx, services) =>
            {
                services.Configure<SoundBridgeOptions>(
                    ctx.Configuration.GetSection("SoundBridge"));

                services.AddSingleton(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<SoundBridgeOptions>>().Value;
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UdnManager>>();
                    return new UdnManager(opts.UdnFilePath, logger);
                });

                services.AddSingleton<Library>(_ =>
                {
                    var initParams = new InitParams();
                    var library = Library.Create(initParams);
                    library.StartDv();
                    return library;
                });

                services.AddSingleton<DvDevice>(sp =>
                {
                    sp.GetRequiredService<Library>();
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

                services.AddSingleton<DvProviderUpnpOrgConnectionManager1>(sp =>
                {
                    var device = sp.GetRequiredService<DvDevice>();
                    return new SoundBridgeConnectionManager(device);
                });

                services.AddSingleton<DvProviderUpnpOrgContentDirectory1>(sp =>
                {
                    var device = sp.GetRequiredService<DvDevice>();
                    return new SoundBridgeContentDirectory(device);
                });

                services.AddHostedService<UpnpDeviceService>();
                services.AddHostedService<ContentDirectoryService>();
            });

            Log.Information("SoundBridge starting");
            await builder.Build().RunAsync();
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