using DapperNotes.Api.Data;
using DapperNotes.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DapperNotes.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly INotesRepository _notesRepository;

        public NotesController(INotesRepository notesRepository)
        {
            _notesRepository = notesRepository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NoteDto>> Get(int id)
        {
            var note = await _notesRepository.GetByIdAsync(id);
            if (note == null)
            {
                return NotFound();
            }
            return Ok(note);
        }

        [HttpPost]
        public async Task<ActionResult<NoteDto>> Create(CreateNoteRequest request)
        {
            var note = await _notesRepository.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
        }
    }
}
