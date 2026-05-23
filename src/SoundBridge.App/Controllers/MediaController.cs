using Microsoft.AspNetCore.Mvc;
using SoundBridge.App.Core;
using SoundBridge.App.Library;

namespace SoundBridge.App.Controllers;

[Route("media")]
public class MediaController : ControllerBase
{
    private readonly IContentResolver _resolver;

    public MediaController(IContentResolver resolver)
    {
        _resolver = resolver;
    }

    [HttpGet("{**path}")]
    public async Task Get(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Missing path");
            return;
        }

        try
        {
            var (fullPath, _) = _resolver.ResolveToPath(path);

            if (!PathValidator.IsAudioExtension(fullPath))
            {
                Response.StatusCode = 403;
                await Response.WriteAsync("Forbidden file type");
                return;
            }

            if (!System.IO.File.Exists(fullPath))
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("File not found");
                return;
            }

            var mime = GetMimeType(fullPath);
            await Results.File(fullPath, mime, enableRangeProcessing: true).ExecuteAsync(HttpContext);
        }
        catch
        {
            Response.StatusCode = 404;
            await Response.WriteAsync("Not found");
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
