using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoundBridge.Libraries.LocalLibrary;

namespace SoundBridge.App.Services;

public class ContentDirectoryService : BackgroundService
{
    private readonly ILocalLibraryStore _store;
    private readonly ILogger<ContentDirectoryService> _logger;

    public ContentDirectoryService(
        ILocalLibraryStore store,
        ILogger<ContentDirectoryService> logger)
    {
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var roots = _store.GetAll().ToList();
        if (roots.Count == 0)
        {
            _logger.LogWarning("No local libraries configured — library will appear empty");
        }
        else
        {
            foreach (var root in roots)
            {
                if (!Directory.Exists(root.Path))
                {
                    _logger.LogWarning("Local library '{Name}' path does not exist: {Path}", root.Name, root.Path);
                    continue;
                }

                _logger.LogInformation("Local library '{Name}' → {Path}", root.Name, root.Path);
            }
        }

        _logger.LogInformation("ContentDirectory service ready");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
