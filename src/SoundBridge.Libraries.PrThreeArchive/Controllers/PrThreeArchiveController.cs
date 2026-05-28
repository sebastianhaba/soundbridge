using Microsoft.AspNetCore.Mvc;

namespace SoundBridge.Libraries.PrThreeArchive;

[ApiController]
[Route("api/pr-three-archive")]
public class PrThreeArchiveController : ControllerBase
{
    private readonly IPrThreeArchiveStore _store;

    public PrThreeArchiveController(IPrThreeArchiveStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult GetRoot()
    {
        return Ok(new { Name = _store.GetRootName() });
    }

    [HttpPut]
    public IActionResult UpdateRoot([FromBody] UpdateRootRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        _store.SetRootName(request.Name);
        return Ok(new { Name = request.Name });
    }
}

public record UpdateRootRequest(string Name);
