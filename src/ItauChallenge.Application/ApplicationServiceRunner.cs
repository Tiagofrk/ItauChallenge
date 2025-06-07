using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ItauChallenge.Application;

public class ApplicationServiceRunner
{
    private readonly ILogger<ApplicationServiceRunner> _logger;
    private readonly IResilientQuoteService _resilientQuoteService;

    public ApplicationServiceRunner(ILogger<ApplicationServiceRunner> logger, IResilientQuoteService resilientQuoteService)
    {
        _logger = logger;
        _resilientQuoteService = resilientQuoteService;
    }

    public async Task RunQuoteFetchingDemoAsync()
    {
        _logger.LogInformation("Starting RunQuoteFetchingDemoAsync...");

        for (int i = 0; i < 10; i++)
        {
            _logger.LogInformation("Demo Iteration {Iteration}: Attempting to get quote for 'ITUB4'.", i + 1);
            var quote = await _resilientQuoteService.GetLatestQuoteAsync("ITUB4");
            _logger.LogInformation("Demo Iteration {Iteration}: Received: {Quote}", i + 1, quote);

            if (i == 2 || i == 5) // Wait a bit to see circuit breaker states
            {
                 _logger.LogInformation("Demo Iteration {Iteration}: Waiting for 5 seconds...", i+1);
                 await Task.Delay(5000);
            }
            else
            {
                 await Task.Delay(500); // Short delay between attempts
            }
        }
        _logger.LogInformation("RunQuoteFetchingDemoAsync finished.");
    }

    // Static method to allow running this example if needed (e.g. from a console app host)
    // This won't be directly executable in this subtask but sets up for future integration.
    public static async Task ExecuteDemo(IServiceProvider services)
    {
        var runner = services.GetRequiredService<ApplicationServiceRunner>();
        await runner.RunQuoteFetchingDemoAsync();
    }
}
