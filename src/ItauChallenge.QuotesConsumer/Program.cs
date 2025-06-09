using ItauChallenge.QuotesConsumer;
using ItauChallenge.Infra; // Required for IDatabaseService, DatabaseService
using ItauChallenge.Application.Services; // For IKafkaMessageProcessorService & implementation
using ItauChallenge.Domain.Repositories;  // For Repository interfaces

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // IConfiguration is registered by default.

        // Register Infrastructure services (DatabaseService provides implementations for all repository interfaces)
        // IDatabaseService is used by KafkaMessageProcessorService and for DB initialization.
        services.AddScoped<IDatabaseService, DatabaseService>();

        // Specific repositories needed by Worker and KafkaMessageProcessorService
        services.AddScoped<IAssetRepository, DatabaseService>();
        services.AddScoped<IPositionRepository, DatabaseService>();
        services.AddScoped<IProcessedMessageRepository, DatabaseService>();
        // IQuoteRepository is not directly injected into Worker or KafkaMessageProcessorService with the new design,
        // as SaveQuoteAndMarkMessageProcessedAsync on IDatabaseService handles the quote saving.

        // Register Application Services
        services.AddScoped<IKafkaMessageProcessorService, KafkaMessageProcessorService>();

        services.AddHostedService<Worker>();
    })
    .Build();

// Initialize the database schema if needed, similar to API project
// This is a good place for a one-time setup during startup in Development.
// For production, migrations are usually handled out-of-band or with more sophisticated tools.
// Ensure this runs *before* host.Run() but after .Build()
if (host.Services.GetService<IHostEnvironment>()?.IsDevelopment() ?? false)
{
    try
    {
        using (var scope = host.Services.CreateScope())
        {
            var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            // It's important that scripts.txt is accessible by this consumer application as well.
            // Ensure its "Copy to Output Directory" property is set to "Copy if newer" or "Copy always".
            await dbService.InitializeDatabaseAsync();
            Console.WriteLine("Database initialization attempted by QuotesConsumer.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during database initialization in QuotesConsumer: {ex.Message}");
        // Optionally, rethrow or handle as critical failure depending on app requirements
        // Environment.Exit(1); // Example: exit if DB init fails
    }
}

host.Run();
