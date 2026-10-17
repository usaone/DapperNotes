using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using DapperNotes.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DapperNotes.Tests;

public class BasicAuthenticationHandlerTests
{
    private const string FailureMessage = "Invalid credentials.";

    private static readonly SwaggerUser Admin = new()
    {
        Username = "swagger-admin",
        Password = "admin-pw",
        Roles = ["SwaggerUser"]
    };

    private static string Basic(string credentials) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

    private static async Task<(BasicAuthenticationHandler Handler, DefaultHttpContext Context)> CreateHandlerAsync(
        string? authorizationHeader,
        params SwaggerUser[] users)
    {
        var context = new DefaultHttpContext();
        if (authorizationHeader is not null)
        {
            context.Request.Headers.Authorization = authorizationHeader;
        }

        var swaggerAuth = new SwaggerAuthOptions { Users = [.. users] };
        var handler = new BasicAuthenticationHandler(
            new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(swaggerAuth));

        var scheme = new AuthenticationScheme(SwaggerSecurity.Scheme, null, typeof(BasicAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);
        return (handler, context);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNoAuthorizationHeader_ReturnsNoResult()
    {
        var (handler, _) = await CreateHandlerAsync(null, Admin);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task AuthenticateAsync_WithBearerHeader_ReturnsNoResult()
    {
        var (handler, _) = await CreateHandlerAsync("Bearer some-token", Admin);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_SucceedsWithNameAndRoleClaims()
    {
        var (handler, _) = await CreateHandlerAsync(Basic("swagger-admin:admin-pw"), Admin);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("swagger-admin", result.Principal.Identity?.Name);
        Assert.True(result.Principal.IsInRole("SwaggerUser"));
    }

    [Fact]
    public async Task AuthenticateAsync_WithPasswordContainingColon_SplitsOnTheFirstColonOnly()
    {
        var user = new SwaggerUser { Username = "colon-user", Password = "pa:ss:word", Roles = ["SwaggerUser"] };
        var (handler, _) = await CreateHandlerAsync(Basic("colon-user:pa:ss:word"), user);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_Fails()
    {
        var (handler, _) = await CreateHandlerAsync(Basic("swagger-admin:wrong"), Admin);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AuthenticateAsync_WithUnknownUserAndWrongPassword_FailWithTheSameMessage()
    {
        var (unknownUserHandler, _) = await CreateHandlerAsync(Basic("ghost:admin-pw"), Admin);
        var (wrongPasswordHandler, _) = await CreateHandlerAsync(Basic("swagger-admin:wrong"), Admin);

        var unknownUser = await unknownUserHandler.AuthenticateAsync();
        var wrongPassword = await wrongPasswordHandler.AuthenticateAsync();

        // The response must not reveal which usernames exist.
        Assert.Equal(FailureMessage, unknownUser.Failure?.Message);
        Assert.Equal(FailureMessage, wrongPassword.Failure?.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WithMalformedBase64_Fails()
    {
        var (handler, _) = await CreateHandlerAsync("Basic !!!!", Admin);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.None);
    }

    [Fact]
    public async Task AuthenticateAsync_WithCredentialsMissingTheColon_Fails()
    {
        var (handler, _) = await CreateHandlerAsync(Basic("justausername"), Admin);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.None);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenConfiguredPasswordIsEmpty_NeverAuthenticates()
    {
        var noPassword = new SwaggerUser { Username = "unset", Password = "", Roles = ["SwaggerUser"] };
        var (handler, _) = await CreateHandlerAsync(Basic("unset:"), noPassword);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ChallengeAsync_Returns401WithBasicWwwAuthenticateHeader()
    {
        var (handler, context) = await CreateHandlerAsync(null, Admin);

        await handler.ChallengeAsync(new AuthenticationProperties());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var header = context.Response.Headers.WWWAuthenticate.ToString();
        Assert.StartsWith("Basic realm=", header);
    }

    // IOptionsMonitor<T> has no simple public implementation, and the handler only reads CurrentValue.
    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
