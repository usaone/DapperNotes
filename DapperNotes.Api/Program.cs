using DapperNotes.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new SqliteConnectionFactory(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Notes")
        ?? throw new InvalidOperationException("Missing 'Notes' connection string")));

builder.Services.AddScoped<INotesRepository, NotesRepository>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<IDbConnectionFactory>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DapperNotes v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
