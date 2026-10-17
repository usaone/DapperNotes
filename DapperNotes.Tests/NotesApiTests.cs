using System.Net;
using System.Net.Http.Json;
using DapperNotes.Api.Models;

namespace DapperNotes.Tests;

public class NotesApiTests : IClassFixture<AppFactory>
{
    private readonly HttpClient _client;

    public NotesApiTests(AppFactory factory) => _client = factory.CreateClient();

    private async Task<NoteDto> CreateNoteAsync(string title = "Title", string content = "Content")
    {
        var response = await _client.PostAsJsonAsync("/notes", new CreateNoteRequest { Title = title, Content = content });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteDto>();
        Assert.NotNull(note);
        return note;
    }

    [Fact]
    public async Task GetAll_ReturnsCreatedNotesInIdOrder()
    {
        var first = await CreateNoteAsync("List one");
        var second = await CreateNoteAsync("List two");

        var notes = await _client.GetFromJsonAsync<List<NoteDto>>("/notes");

        Assert.NotNull(notes);
        var ids = notes.Select(n => n.Id).ToList();
        Assert.Contains(first.Id, ids);
        Assert.Contains(second.Id, ids);
        Assert.True(ids.IndexOf(first.Id) < ids.IndexOf(second.Id));
    }

    [Fact]
    public async Task Put_WithExistingId_ReplacesTitleAndContentAndKeepsCreatedAt()
    {
        var created = await CreateNoteAsync("Before", "Old content");

        var response = await _client.PutAsJsonAsync(
            $"/notes/{created.Id}", new UpdateNoteRequest { Title = "After", Content = "New content" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NoteDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("After", updated.Title);
        Assert.Equal("New content", updated.Content);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);

        var fetched = await _client.GetFromJsonAsync<NoteDto>($"/notes/{created.Id}");
        Assert.Equal("After", fetched?.Title);
    }

    [Fact]
    public async Task Put_WithMissingId_Returns404AndCreatesNothing()
    {
        var response = await _client.PutAsJsonAsync(
            "/notes/999999", new UpdateNoteRequest { Title = "Ghost", Content = "None" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/notes/999999")).StatusCode);
    }

    [Fact]
    public async Task Put_WithMissingTitle_Returns400()
    {
        var created = await CreateNoteAsync();

        var response = await _client.PutAsJsonAsync($"/notes/{created.Id}", new { Content = "No title" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingId_Returns204ThenTheNoteIsGone()
    {
        var created = await CreateNoteAsync();

        var response = await _client.DeleteAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/notes/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_CalledTwice_Returns404TheSecondTime()
    {
        var created = await CreateNoteAsync();
        await _client.DeleteAsync($"/notes/{created.Id}");

        var second = await _client.DeleteAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}
