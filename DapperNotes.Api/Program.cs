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

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<IDbConnectionFactory>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();   // before

    await next(context);                                 // pass it down the pipeline

    sw.Stop();                                           // after: the response is ready
    Console.WriteLine($"{context.Request.Method} {context.Request.Path} took {sw.ElapsedMilliseconds} ms");
});


app.Run();
