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

        if (_store.GetStationByName(request.Name) is not null)
            return Conflict($"Station '{request.Name}' already exists");

        _store.AddStation(request.Name);

        return CreatedAtAction(nameof(GetStation),
            new { name = request.Name },
            new { request.Name });
    }

    [HttpPut("stations/{name}")]
    public IActionResult UpdateStation(string name, [FromBody] UpdateStationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        if (name != request.Name && _store.GetStationByName(request.Name) is not null)
            return Conflict($"Station '{request.Name}' already exists");

        if (_store.UpdateStation(name, request.Name))
            return Ok(new { Name = request.Name });

        return NotFound();
    }

    [HttpDelete("stations/{name}")]
    public IActionResult DeleteStation(string name)
    {
        if (_store.DeleteStation(name))
            return NoContent();

        return NotFound();
    }

    [HttpGet("stations/{stationName}/streams")]
    public IActionResult GetStreams(string stationName)
    {
        var streams = _store.GetStreamsByStation(stationName).ToList();
        return Ok(streams);
    }

    [HttpGet("stations/{stationName}/streams/{streamName}")]
    public IActionResult GetStream(string stationName, string streamName)
    {
        var stream = _store.GetStream(stationName, streamName);
        if (stream is null)
            return NotFound();

        return Ok(stream);
    }

    [HttpPost("stations/{stationName}/streams")]
    public IActionResult AddStream(string stationName, [FromBody] AddStreamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Url is required");

        if (_store.GetStationByName(stationName) is null)
            return NotFound($"Station '{stationName}' not found");

        if (_store.GetStream(stationName, request.Name) is not null)
            return Conflict($"Stream '{request.Name}' already exists in station '{stationName}'");

        _store.AddStream(stationName, request.Name, request.Url,
            request.MimeType ?? "audio/mpeg");

        return CreatedAtAction(nameof(GetStream),
            new { stationName, streamName = request.Name },
            new { request.Name, request.Url, request.MimeType });
    }

    [HttpPut("stations/{stationName}/streams/{streamName}")]
    public IActionResult UpdateStream(string stationName, string streamName, [FromBody] UpdateStreamRequest request)
    {
        if (_store.GetStationByName(stationName) is null)
            return NotFound($"Station '{stationName}' not found");

        var existing = _store.GetStream(stationName, streamName);
        if (existing is null)
            return NotFound();

        var newName = request.Name ?? streamName;
        if (newName != streamName && _store.GetStream(stationName, newName) is not null)
            return Conflict($"Stream '{newName}' already exists in station '{stationName}'");

        if (_store.UpdateStream(stationName, streamName, request.Name, request.Url, request.MimeType))
            return Ok(new { Name = newName, Url = request.Url ?? existing.Url, MimeType = request.MimeType ?? existing.MimeType });

        return NotFound();
    }

    [HttpDelete("stations/{stationName}/streams/{streamName}")]
    public IActionResult DeleteStream(string stationName, string streamName)
    {
        if (_store.DeleteStream(stationName, streamName))
            return NoContent();

        return NotFound();
    }
}

public record UpdateRootRequest(string Name);
public record AddStationRequest(string Name);
public record UpdateStationRequest(string Name);
public record AddStreamRequest(string Name, string Url, string? MimeType);
public record UpdateStreamRequest(string? Name, string? Url, string? MimeType);
