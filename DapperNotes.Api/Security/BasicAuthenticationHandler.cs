using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace DapperNotes.Api.Security
{
    public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SwaggerAuthOptions _swaggerAuth;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<SwaggerAuthOptions> swaggerAuth)
            : base(options, logger, encoder)
        {
            _swaggerAuth = swaggerAuth.Value;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // No Authorization header at all: nobody tried to authenticate. The challenge will ask for credentials.
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!AuthenticationHeaderValue.TryParse(headerValues, out var header)
                || !string.Equals(header.Scheme, SwaggerSecurity.Scheme, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(header.Parameter))
            {
                // A different scheme (for example Bearer) is not ours to judge.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!TryDecodeCredentials(header.Parameter, out var username, out var password))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
            }

            var user = _swaggerAuth.Users.FirstOrDefault(
                candidate => string.Equals(candidate.Username, username, StringComparison.Ordinal));

            // Compare even when the user is unknown so both failures cost the same time and look the same.
            var expectedPassword = user?.Password ?? string.Empty;
            var passwordMatches = ConstantTimeEquals(password, expectedPassword);

            // An unset password must never authenticate, even against an empty password.
            if (user is null || expectedPassword.Length == 0 || !passwordMatches)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
            }

            var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // This header is what makes the browser show its own sign-in dialog.
            Response.Headers.WWWAuthenticate = $"{SwaggerSecurity.Scheme} realm=\"DapperNotes Swagger\", charset=\"UTF-8\"";
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        private static bool TryDecodeCredentials(string parameter, out string username, out string password)
        {
            username = string.Empty;
            password = string.Empty;

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
                var separator = decoded.IndexOf(':');
                if (separator < 0)
                {
                    return false;
                }

                username = decoded[..separator];
                password = decoded[(separator + 1)..];
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool ConstantTimeEquals(string left, string right)
        {
            // Hashing first gives both sides the same length, so FixedTimeEquals does not reveal the password length.
            var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
            var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));
            return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
        }
    }
}
