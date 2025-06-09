using ItauChallenge.Application.Services;
using ItauChallenge.Domain.Repositories;
using ItauChallenge.Infra;
using ItauChallenge.Application; // For IExternalQuotationService, ResilientQuoteService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // To use controllers
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services needed by controllers
builder.Services.AddSingleton<IExternalQuotationService, ExternalQuotationService>(); // From ItauChallenge.Application
builder.Services.AddSingleton<IResilientQuoteService, ResilientQuoteService>();   // From ItauChallenge.Application

// Register Infrastructure services (DatabaseService provides implementations for all repository interfaces)
builder.Services.AddScoped<IDatabaseService, DatabaseService>(); // Keep for IDbInitializer if needed by startup
builder.Services.AddScoped<IAssetRepository, DatabaseService>();
builder.Services.AddScoped<IOperationRepository, DatabaseService>();
builder.Services.AddScoped<IUserRepository, DatabaseService>();
builder.Services.AddScoped<IPositionRepository, DatabaseService>();
builder.Services.AddScoped<IQuoteRepository, DatabaseService>();
builder.Services.AddScoped<IProcessedMessageRepository, DatabaseService>();
builder.Services.AddScoped<IDbInitializer, DatabaseService>(); // DatabaseService itself is the initializer

// Register new Application Services
builder.Services.AddScoped<IAssetApplicationService, AssetApplicationService>();
builder.Services.AddScoped<IBrokerageApplicationService, BrokerageApplicationService>();
builder.Services.AddScoped<IClientApplicationService, ClientApplicationService>();
builder.Services.AddScoped<IUserApplicationService, UserApplicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controller routes
app.MapControllers();

// Initialize the database schema if needed
// This is a good place for a one-time setup during startup in Development.
// For production, migrations are usually handled out-of-band or with more sophisticated tools.
if (app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            await dbService.InitializeDatabaseAsync();
            Console.WriteLine("Database initialization attempted.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during database initialization: {ex.Message}");
        // Optionally, rethrow or handle as critical failure depending on app requirements
    }
}

// Keep the weather forecast example for now, or remove if not needed

// Keep the weather forecast example for now, or remove if not needed
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
