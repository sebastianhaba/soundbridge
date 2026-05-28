namespace SoundBridge.Libraries.PrThreeArchive;

public class ApiListResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int Total { get; set; }
    public object? Exception { get; set; }
}

public class PrShowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
}

public class PrEpisodeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? CategoryName { get; set; }
    public DateTime DatePublic { get; set; }
    public string Duration { get; set; } = "";
    public string AudioFile { get; set; } = "";
}
