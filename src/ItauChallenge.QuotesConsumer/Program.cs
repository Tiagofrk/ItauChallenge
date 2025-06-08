using ItauChallenge.QuotesConsumer;

using ItauChallenge.Infra; // Required for IDatabaseService, DatabaseService

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) => // Added hostContext for IConfiguration access if needed directly here
    {
        // IConfiguration is registered by default with CreateDefaultBuilder.
        // It's available via hostContext.Configuration or can be injected directly into services.

        // Register IDatabaseService and its implementation
        // DatabaseService requires IConfiguration, which will be automatically injected by DI container.
        services.AddScoped<IDatabaseService, DatabaseService>();

        // Worker is already registered to be created via DI and receive its dependencies.
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
