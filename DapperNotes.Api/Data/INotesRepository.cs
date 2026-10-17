using DapperNotes.Api.Models;

namespace DapperNotes.Api.Data
{
    public interface INotesRepository
    {
        Task<IReadOnlyList<NoteDto>> GetAllAsync();
        Task<NoteDto?> GetByIdAsync(int id);
        Task<NoteDto> CreateAsync(CreateNoteRequest request);
        Task<NoteDto?> UpdateAsync(int id, UpdateNoteRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
