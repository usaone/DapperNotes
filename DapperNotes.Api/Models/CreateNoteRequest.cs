namespace DapperNotes.Api.Models
{
    public class CreateNoteRequest
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }
}
