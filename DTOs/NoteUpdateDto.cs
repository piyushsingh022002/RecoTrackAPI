public class NoteUpdateDto
{
    // Removed Id and UserId as per requirements
    public string Title { get; set; }
    public string Content { get; set; }
    public List<string> Tags { get; set; }
    public List<string> MediaUrls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}