using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace DapperNotes.Tests;

public sealed class AppFactory : WebApplicationFactory<Program>
{
    // A shared-cache in-memory database vanishes when its last connection closes,
    // so this one stays open for the life of the factory.
    private readonly string _connectionString = $"Data Source=file:app-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public AppFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Swagger is only mapped in Development.
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Notes"] = _connectionString,
            ["SwaggerAuth:Users:0:Password"] = "admin-pw",
            ["SwaggerAuth:Users:1:Password"] = "viewer-pw"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
