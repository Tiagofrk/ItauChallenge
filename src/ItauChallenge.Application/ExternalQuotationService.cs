using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ItauChallenge.Application;

// Simulates a service that might be unreliable
public interface IExternalQuotationService
{
    Task<string> GetLatestQuoteAsync(string assetId);
}

public class ExternalQuotationService : IExternalQuotationService
{
    private readonly ILogger<ExternalQuotationService> _logger;
    private int _requestCount = 0;

    public ExternalQuotationService(ILogger<ExternalQuotationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetLatestQuoteAsync(string assetId)
    {
        _requestCount++;
        _logger.LogInformation("ExternalQuotationService: Attempting to fetch quote for {AssetId}. Request #{RequestCount}", assetId, _requestCount);

        // Simulate unreliability: Fail 4 out of 5 times
        if (_requestCount % 5 != 1) // Succeeds on 1st, 6th, 11th try etc.
        {
            _logger.LogError("ExternalQuotationService: Simulated failure for {AssetId}.", assetId);
            await Task.Delay(100); // Simulate network delay
            throw new HttpRequestException($"Simulated network error fetching quote for {assetId}.");
        }

        var quote = $"Asset: {assetId}, Price: {new Random().Next(10, 500)} USD (from external)";
        _logger.LogInformation("ExternalQuotationService: Successfully fetched quote: {Quote}", quote);
        await Task.Delay(50); // Simulate network delay
        return quote;
    }
}
