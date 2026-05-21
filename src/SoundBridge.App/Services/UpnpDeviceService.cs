using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenHome.Net.Core;
using OpenHome.Net.Device;
using OpenHome.Net.Device.Providers;
using OhNetLibrary = OpenHome.Net.Core.Library;

namespace SoundBridge.App.Services;

public class UpnpDeviceService : BackgroundService
{
    private readonly OhNetLibrary _library;
    private readonly DvDevice _device;
    private readonly ILogger<UpnpDeviceService> _logger;

    public UpnpDeviceService(
        OhNetLibrary library,
        DvDevice device,
        ILogger<UpnpDeviceService> logger,
        DvProviderUpnpOrgContentDirectory1 _,
        DvProviderUpnpOrgConnectionManager1 __)
    {
        _library = library;
        _device = device;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enabling UPnP device");

        _device.SetDisabled(() => { });
        _device.SetEnabled();

        _logger.LogInformation("UPnP device enabled: {Udn}", _device.Udn());

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            _device.Dispose();
            _library.Dispose();
        }
    }
}
