using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenHome.Net.Device.Providers;

namespace SoundBridge.App.Services;

public class ContentDirectoryService : BackgroundService
{
    private readonly DvProviderUpnpOrgContentDirectory1 _contentDirectory;
    private readonly ILogger<ContentDirectoryService> _logger;

    public ContentDirectoryService(
        DvProviderUpnpOrgContentDirectory1 contentDirectory,
        ILogger<ContentDirectoryService> logger)
    {
        _contentDirectory = contentDirectory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContentDirectory service ready");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
