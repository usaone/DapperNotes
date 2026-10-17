using DapperNotes.Api.Data;
using DapperNotes.Api.Middleware;
using DapperNotes.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new SqliteConnectionFactory(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Notes")
        ?? throw new InvalidOperationException("Missing 'Notes' connection string")));

builder.Services.AddScoped<INotesRepository, NotesRepository>();

builder.Services.AddOptions<RequestTimingOptions>()
    .Bind(builder.Configuration.GetSection(RequestTimingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<SwaggerAuthOptions>(builder.Configuration.GetSection(SwaggerAuthOptions.SectionName));
builder.Services.AddAuthentication(SwaggerSecurity.Scheme)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(SwaggerSecurity.Scheme, null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SwaggerSecurity.Policy, policy => policy.RequireRole(SwaggerSecurity.Role));

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<IDbConnectionFactory>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // The spec is a normal endpoint, so it can require the policy directly.
    app.MapOpenApi().RequireAuthorization(SwaggerSecurity.Policy);

    // The UI is served as static files by middleware, which [Authorize] never sees,
    // so this gate must run before UseSwaggerUI.
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments(SwaggerSecurity.PathPrefix),
        gate => gate.Use(async (context, next) =>
        {
            var authentication = await context.AuthenticateAsync(SwaggerSecurity.Scheme);
            if (!authentication.Succeeded)
            {
                await context.ChallengeAsync(SwaggerSecurity.Scheme);
                return;
            }

            var authorization = await context.RequestServices
                .GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(authentication.Principal!, SwaggerSecurity.Policy);
            if (!authorization.Succeeded)
            {
                await context.ForbidAsync(SwaggerSecurity.Scheme);
                return;
            }

            await next();
        }));

    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DapperNotes v1"));
}

app.UseMiddleware<RequestTimingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
