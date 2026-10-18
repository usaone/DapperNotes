using DapperNotes.Api.Security;
using Microsoft.Extensions.Configuration;

namespace DapperNotes.Tests;

// AppFactory replaces the connection string and the passwords, so a mistake in these
// settings in the committed appsettings.json would not fail any other test.
// These tests read the file as committed, with no overrides.
public class CommittedConfigurationTests
{
    private static IConfiguration LoadCommittedConfiguration()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.GetFiles("DapperNotes.slnx").Any())
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var settingsPath = Path.Combine(directory.FullName, "DapperNotes.Api", "appsettings.json");

        return new ConfigurationBuilder().AddJsonFile(settingsPath, optional: false).Build();
    }

    private static SwaggerAuthOptions LoadSwaggerAuth()
    {
        var options = LoadCommittedConfiguration()
            .GetSection(SwaggerAuthOptions.SectionName)
            .Get<SwaggerAuthOptions>();
        Assert.NotNull(options);
        return options;
    }

    [Fact]
    public void AppSettings_ProvidesTheNotesConnectionString()
    {
        // Program.cs throws at startup when this key is missing, which no other test would notice.
        var connectionString = LoadCommittedConfiguration().GetConnectionString("Notes");

        Assert.False(string.IsNullOrWhiteSpace(connectionString));
    }

    [Fact]
    public void AppSettings_DoesNotContainAnySwaggerPassword()
    {
        var withPassword = LoadSwaggerAuth().Users
            .Where(user => !string.IsNullOrEmpty(user.Password))
            .Select(user => user.Username);

        // Passwords belong in user-secrets or environment variables, never in source control.
        Assert.Empty(withPassword);
    }

    [Fact]
    public void AppSettings_ConfiguresExactlyTheExpectedSwaggerUsersAndRoles()
    {
        var users = LoadSwaggerAuth().Users
            .Select(user => $"{user.Username}:{string.Join(",", user.Roles)}");

        // Adding an account, or changing who holds the SwaggerUser role, must be a deliberate edit here too.
        Assert.Equal(["swagger-admin:SwaggerUser", "viewer:Reader"], users);
    }
}
