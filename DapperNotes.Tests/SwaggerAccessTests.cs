using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace DapperNotes.Tests;

public class SwaggerAccessTests : IClassFixture<SwaggerAccessTests.AppFactory>
{
    private readonly AppFactory _factory;

    public SwaggerAccessTests(AppFactory factory) => _factory = factory;

    private HttpClient CreateClient(string? credentials = null)
    {
        var client = _factory.CreateClient();
        if (credentials is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }

        return client;
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task Swagger_WithoutCredentials_Returns401WithChallenge(string path)
    {
        var response = await CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Basic");
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task Swagger_WithWrongPassword_Returns401(string path)
    {
        var response = await CreateClient("swagger-admin:wrong").GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task Swagger_AsAuthenticatedUserWithoutTheRole_Returns403(string path)
    {
        var response = await CreateClient("viewer:viewer-pw").GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task Swagger_AsSwaggerUser_Returns200(string path)
    {
        var response = await CreateClient("swagger-admin:admin-pw").GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NotesApi_WithoutCredentials_StaysPublic()
    {
        var response = await CreateClient().GetAsync("/notes/999");

        // 404 (not 401) proves the auth gate does not leak onto the Notes endpoints.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public sealed class AppFactory : WebApplicationFactory<Program>
    {
        // A shared-cache in-memory database vanishes when its last connection closes,
        // so this one stays open for the life of the factory.
        private readonly string _connectionString = $"Data Source=file:swagger-{Guid.NewGuid():N}?mode=memory&cache=shared";
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
}
