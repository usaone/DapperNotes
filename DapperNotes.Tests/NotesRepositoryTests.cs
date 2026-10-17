using DapperNotes.Api.Data;
using DapperNotes.Api.Models;
using Microsoft.Data.Sqlite;

namespace DapperNotes.Tests;

public class NotesRepositoryTests : IAsyncLifetime
{
    // A named, shared-cache in-memory database lives only while at least one connection is open.
    // The repository opens and disposes a connection per call, so this keep-alive connection
    // stops the database (and its table) from vanishing between calls.
    private SqliteConnection _keepAlive = null!;
    private NotesRepository _repository = null!;

    public async Task InitializeAsync()
    {
        // A unique name per test instance means tests can't see each other's data.
        var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";

        _keepAlive = new SqliteConnection(connectionString);
        await _keepAlive.OpenAsync();

        var factory = new SqliteConnectionFactory(connectionString);
        await DatabaseInitializer.InitializeAsync(factory);
        _repository = new NotesRepository(factory);
    }

    public async Task DisposeAsync()
    {
        await _keepAlive.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsNoteWithGeneratedId()
    {
        var request = new CreateNoteRequest { Title = "First note", Content = "Hello Dapper" };

        var created = await _repository.CreateAsync(request);

        Assert.True(created.Id > 0);
        Assert.Equal("First note", created.Title);
        Assert.Equal("Hello Dapper", created.Content);
        Assert.InRange(created.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task GetByIdAsync_AfterCreate_ReturnsTheSameNote()
    {
        var created = await _repository.CreateAsync(
            new CreateNoteRequest { Title = "Round trip", Content = "Stored and read back" });

        var fetched = await _repository.GetByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Content, fetched.Content);
        Assert.Equal(created.CreatedAt, fetched.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoteMissing_ReturnsNull()
    {
        var fetched = await _repository.GetByIdAsync(9999);

        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var notes = await _repository.GetAllAsync();

        Assert.Empty(notes);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotesOrderedById()
    {
        var first = await _repository.CreateAsync(new CreateNoteRequest { Title = "A", Content = "a" });
        var second = await _repository.CreateAsync(new CreateNoteRequest { Title = "B", Content = "b" });

        var notes = await _repository.GetAllAsync();

        Assert.Equal([first.Id, second.Id], notes.Select(n => n.Id));
    }

    [Fact]
    public async Task UpdateAsync_ChangesOnlyTheTargetNote()
    {
        var target = await _repository.CreateAsync(new CreateNoteRequest { Title = "Target", Content = "t" });
        var other = await _repository.CreateAsync(new CreateNoteRequest { Title = "Other", Content = "o" });

        var updated = await _repository.UpdateAsync(target.Id, new UpdateNoteRequest { Title = "Changed", Content = "c" });

        Assert.NotNull(updated);
        Assert.Equal("Changed", updated.Title);
        Assert.Equal(target.CreatedAt, updated.CreatedAt);
        var untouched = await _repository.GetByIdAsync(other.Id);
        Assert.Equal("Other", untouched?.Title);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteMissing_ReturnsNull()
    {
        var updated = await _repository.UpdateAsync(9999, new UpdateNoteRequest { Title = "x", Content = "y" });

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheTargetNote()
    {
        var target = await _repository.CreateAsync(new CreateNoteRequest { Title = "Gone", Content = "g" });
        var other = await _repository.CreateAsync(new CreateNoteRequest { Title = "Stays", Content = "s" });

        var deleted = await _repository.DeleteAsync(target.Id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(target.Id));
        Assert.NotNull(await _repository.GetByIdAsync(other.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenNoteMissing_ReturnsFalse()
    {
        Assert.False(await _repository.DeleteAsync(9999));
    }

    [Fact]
    public async Task CreateAsync_CalledTwice_ReturnsIncreasingIds()
    {
        var first = await _repository.CreateAsync(new CreateNoteRequest { Title = "One", Content = "1" });
        var second = await _repository.CreateAsync(new CreateNoteRequest { Title = "Two", Content = "2" });

        Assert.True(second.Id > first.Id);
    }
}
