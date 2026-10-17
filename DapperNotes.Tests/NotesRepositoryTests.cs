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
    public async Task CreateAsync_CalledTwice_ReturnsIncreasingIds()
    {
        var first = await _repository.CreateAsync(new CreateNoteRequest { Title = "One", Content = "1" });
        var second = await _repository.CreateAsync(new CreateNoteRequest { Title = "Two", Content = "2" });

        Assert.True(second.Id > first.Id);
    }
}
