using System;

namespace RecoTrackApi.DTOs
{
 public class SharedNoteDto
 {
 public string? NoteId { get; set; }
 public string? Title { get; set; }
 public string? Content { get; set; }
 public string? OwnerId { get; set; }
 public string? Permission { get; set; }
 public DateTime SharedAt { get; set; }
 }
}
