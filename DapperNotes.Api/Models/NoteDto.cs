namespace DapperNotes.Api.Models
{
    public class NoteDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
