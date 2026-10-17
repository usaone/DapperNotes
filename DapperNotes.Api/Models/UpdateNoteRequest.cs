namespace DapperNotes.Api.Models
{
    public class UpdateNoteRequest
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }
}
