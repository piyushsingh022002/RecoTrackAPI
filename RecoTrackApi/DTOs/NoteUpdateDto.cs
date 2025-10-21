public class NoteUpdateDto
{
    // Removed Id and UserId as per requirements
    public required string Title { get; set; }
    public required string Content { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}