using System.Net;
using Microsoft.Extensions.Options;
using OpenHome.Net.Core;
using OhNetLibrary = OpenHome.Net.Core.Library;
using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using Serilog;
using SoundBridge.Abstractions;
using SoundBridge.Libraries.LocalLibrary;
using SoundBridge.App.Core;
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

            services.AddSingleton<OhNetLibrary>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<SoundBridgeOptions>>().Value;
                var initParams = new InitParams();
                var library = OhNetLibrary.Create(initParams);
                library.StartDv();
                SelectBestSubnet(library, opts.WebServerHost);
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
                device.SetAttribute("Upnp.Manufacturer", "Sebastian Haba");
                device.SetAttribute("Upnp.ManufacturerUrl", "https://github.com/sebastianhaba");
                device.SetAttribute("Upnp.ModelName", "SoundBridge");
                device.SetAttribute("Upnp.ModelNumber", "0.1.1");
                device.SetAttribute("Upnp.ModelUrl", "https://github.com/sebastianhaba/soundbridge");

                var presentationUrl = BuildPresentationUrl(opts.WebServerHost, opts.WebServerPort);
                if (presentationUrl is not null)
                    device.SetAttribute("Upnp.PresentationUrl", presentationUrl);

                var iconBaseUrl = BuildIconBaseUrl(opts.WebServerHost, opts.WebServerPort);
                if (iconBaseUrl is not null)
                {
                    device.SetAttribute("Upnp.IconList", $@"
<icon>
    <mimetype>image/png</mimetype>
    <width>256</width>
    <height>256</height>
    <depth>32</depth>
    <url>{iconBaseUrl}/icon_256.png</url>
</icon>
<icon>
    <mimetype>image/png</mimetype>
    <width>48</width>
    <height>48</height>
    <depth>32</depth>
    <url>{iconBaseUrl}/icon_48.png</url>
</icon>");
                }

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

            services.AddControllers()
                .AddApplicationPart(typeof(LocalLibrariesController).Assembly);
            services.AddOpenApi();

            var app = builder.Build();

            app.UseStaticFiles();
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

    private static string? BuildPresentationUrl(string host, int port)
    {
        if (host is "0.0.0.0" or "+" or "::" or "")
            return null;

        return $"http://{host}:{port}/scalar/v1";
    }

    private static string? BuildIconBaseUrl(string host, int port)
    {
        if (host is "0.0.0.0" or "+" or "::" or "")
            return null;

        return $"http://{host}:{port}/icons";
    }

    private static void SelectBestSubnet(OhNetLibrary library, string host)
    {
        if (host is "0.0.0.0" or "+" or "::" or "")
            return;

        using var subnetList = new SubnetList();
        for (uint i = 0; i < subnetList.Size(); i++)
        {
            var adapter = subnetList.SubnetAt(i);
            var fullName = adapter.FullName();
            if (fullName.StartsWith(host))
            {
                Log.Information("Selected UPnP subnet: {Subnet}", fullName);
                library.SetCurrentSubnet(adapter);
                return;
            }
        }

        Log.Warning("No subnet found matching {Host}, using all subnets", host);
    }
}
