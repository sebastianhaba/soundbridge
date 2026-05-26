using Microsoft.AspNetCore.Mvc;

namespace SoundBridge.Libraries.RadioOnline;

[ApiController]
[Route("api/radio-online")]
public class RadioOnlineController : ControllerBase
{
    private readonly IRadioOnlineStore _store;

    public RadioOnlineController(IRadioOnlineStore store)
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

    [HttpGet("stations")]
    public IActionResult GetAllStations()
    {
        var stations = _store.GetAllStations().ToList();
        return Ok(stations);
    }

    [HttpGet("stations/{name}")]
    public IActionResult GetStation(string name)
    {
        var station = _store.GetStationByName(name);
        if (station is null)
            return NotFound();

        return Ok(station);
    }

    [HttpPost("stations")]
    public IActionResult AddStation([FromBody] AddStationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Url is required");

        if (_store.GetStationByName(request.Name) is not null)
            return Conflict($"Station '{request.Name}' already exists");

        _store.AddStation(request.Name, request.Url,
            request.MimeType ?? "audio/mpeg");

        return CreatedAtAction(nameof(GetStation),
            new { name = request.Name },
            new { request.Name, request.Url, request.MimeType });
    }

    [HttpPut("stations/{name}")]
    public IActionResult UpdateStation(string name, [FromBody] UpdateStationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Url is required");

        if (_store.UpdateStation(name, request.Url,
            request.MimeType ?? "audio/mpeg"))
            return Ok(new { Name = name, request.Url, request.MimeType });

        return NotFound();
    }

    [HttpDelete("stations/{name}")]
    public IActionResult DeleteStation(string name)
    {
        if (_store.DeleteStation(name))
            return NoContent();

        return NotFound();
    }
}

public record UpdateRootRequest(string Name);
public record AddStationRequest(string Name, string Url, string? MimeType);
public record UpdateStationRequest(string Url, string? MimeType);
