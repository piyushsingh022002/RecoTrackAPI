namespace RecoTrackApi.DTOs
{
 public class ShareNoteRequestDto
 {
 // Either provide SharedWithUserId or SharedWithEmails (comma separated)
 public string? SharedWithUserId { get; set; }
 public string? SharedWithEmails { get; set; }
 public string Permission { get; set; } = "VIEW"; // VIEW | EDIT
 }
}
