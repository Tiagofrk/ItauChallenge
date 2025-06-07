using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace ItauChallenge.Application;

public interface IResilientQuoteService
{
    Task<string> GetLatestQuoteAsync(string assetId);
}

public class ResilientQuoteService : IResilientQuoteService
{
    private readonly IExternalQuotationService _externalQuotationService;
    private readonly ILogger<ResilientQuoteService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public ResilientQuoteService(
        IExternalQuotationService externalQuotationService,
        ILogger<ResilientQuoteService> logger)
    {
        _externalQuotationService = externalQuotationService;
        _logger = logger;

        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>() // Specify exceptions that should trip the circuit
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2, // Allow 2 failures before opening circuit
                durationOfBreak: TimeSpan.FromSeconds(30), // Keep circuit open for 30 seconds
                onBreak: (exception, timespan, context) =>
                {
                    _logger.LogWarning(exception, "CircuitBreaker: Breaking circuit for {TimeSpan} due to {ExceptionType}", timespan, exception.GetType().Name);
                },
                onReset: (context) =>
                {
                    _logger.LogInformation("CircuitBreaker: Resetting circuit.");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("CircuitBreaker: Circuit is now half-open. Next call is a trial.");
                }
            );
    }

    public async Task<string> GetLatestQuoteAsync(string assetId)
    {
        try
        {
            return await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("ResilientQuoteService: Attempting to fetch quote for {AssetId} through circuit breaker.", assetId);
                return await _externalQuotationService.GetLatestQuoteAsync(assetId);
            });
        }
        catch (BrokenCircuitException bce)
        {
            _logger.LogWarning(bce, "ResilientQuoteService: Circuit is open for {AssetId}. Returning fallback.", assetId);
            return $"Fallback: Quotation for {assetId} is currently unavailable (circuit open). Try again later.";
        }
        catch (HttpRequestException hre) // Catch if policy rethrows or if it's an unhandled attempt
        {
            _logger.LogError(hre, "ResilientQuoteService: HttpRequestException for {AssetId} after circuit breaker policy. Returning fallback.", assetId);
            return $"Fallback: Quotation for {assetId} is currently unavailable due to a request error. Try again later.";
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "ResilientQuoteService: Unexpected error fetching quote for {AssetId}. Returning fallback.", assetId);
             return $"Fallback: An unexpected error occurred while fetching quotation for {assetId}.";
        }
    }
}
