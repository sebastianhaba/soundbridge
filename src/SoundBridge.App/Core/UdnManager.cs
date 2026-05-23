using Microsoft.Extensions.Logging;

namespace SoundBridge.App.Core;

public class UdnManager
{
    private readonly string _filePath;
    private readonly ILogger<UdnManager> _logger;

    public UdnManager(string filePath, ILogger<UdnManager> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public string GetOrCreateUdn()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(_filePath))
        {
            var stored = File.ReadAllText(_filePath).Trim();
            if (!string.IsNullOrEmpty(stored))
            {
                _logger.LogInformation("UDN loaded from {Path}: {Udn}", _filePath, stored);
                return stored;
            }
        }

        var udn = Guid.NewGuid().ToString("D");
        File.WriteAllText(_filePath, udn);
        _logger.LogInformation("UDN generated and saved to {Path}: {Udn}", _filePath, udn);
        return udn;
    }
}
