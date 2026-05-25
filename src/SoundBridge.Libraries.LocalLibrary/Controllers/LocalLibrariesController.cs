using Microsoft.AspNetCore.Mvc;

namespace SoundBridge.Libraries.LocalLibrary;

[ApiController]
[Route("api/local-libraries")]
public class LocalLibrariesController : ControllerBase
{
    private readonly ILocalLibraryStore _store;

    public LocalLibrariesController(ILocalLibraryStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var roots = _store.GetAll().ToList();
        return Ok(roots);
    }

    [HttpGet("{name}")]
    public IActionResult GetByName(string name)
    {
        var root = _store.GetByName(name);
        if (root is null)
            return NotFound();
        return Ok(root);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateLocalLibraryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest("Path is required");

        if (_store.GetByName(request.Name) is not null)
            return Conflict($"Library root '{request.Name}' already exists");

        _store.Add(request.Name, request.Path);
        return CreatedAtAction(nameof(GetByName), new { name = request.Name }, new { request.Name, request.Path });
    }

    [HttpDelete("{name}")]
    public IActionResult Delete(string name)
    {
        if (_store.Delete(name))
            return NoContent();
        return NotFound();
    }
}

public record CreateLocalLibraryRequest(string Name, string Path);
