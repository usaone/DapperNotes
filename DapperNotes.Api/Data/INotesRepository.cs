using DapperNotes.Api.Models;

namespace DapperNotes.Api.Data
{
    public interface INotesRepository
    {
        Task<NoteDto?> GetByIdAsync(int id);
        Task<NoteDto> CreateAsync(CreateNoteRequest request);
    }
}
