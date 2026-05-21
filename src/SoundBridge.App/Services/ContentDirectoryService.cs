using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundBridge.App.Configuration;
using SoundBridge.App.Core;

namespace SoundBridge.App.Services;

public class ContentDirectoryService : BackgroundService
{
    private readonly SoundBridgeOptions _options;
    private readonly ILogger<ContentDirectoryService> _logger;

    public ContentDirectoryService(
        IOptions<SoundBridgeOptions> options,
        ILogger<ContentDirectoryService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ValidateLibraryRoots();

        _logger.LogInformation("ContentDirectory service ready");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ValidateLibraryRoots()
    {
        if (_options.LibraryRoots.Length == 0)
        {
            _logger.LogWarning("No LibraryRoots configured — library will appear empty");
            return;
        }

        foreach (var root in _options.LibraryRoots)
        {
            if (string.IsNullOrWhiteSpace(root.Name))
            {
                _logger.LogWarning("Library root has empty name, skipping");
                continue;
            }

            if (string.IsNullOrWhiteSpace(root.Path))
            {
                _logger.LogWarning("Library root '{Name}' has empty path", root.Name);
                continue;
            }

            if (!Directory.Exists(root.Path))
            {
                _logger.LogWarning("Library root '{Name}' path does not exist: {Path}", root.Name, root.Path);
                continue;
            }

            _logger.LogInformation("Library root '{Name}' → {Path}", root.Name, root.Path);
        }
    }
}
