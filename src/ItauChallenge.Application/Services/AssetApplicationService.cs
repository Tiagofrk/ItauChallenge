using ItauChallenge.Contracts.Dtos;
using ItauChallenge.Application.Services;
using ItauChallenge.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public class AssetApplicationService : IAssetApplicationService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IQuoteRepository _quoteRepository;
        private readonly ILogger<AssetApplicationService> _logger;

        public AssetApplicationService(
            IAssetRepository assetRepository,
            IQuoteRepository quoteRepository,
            ILogger<AssetApplicationService> logger)
        {
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LatestQuoteDto> GetLatestQuoteAsync(string assetId)
        {
            _logger.LogInformation("Attempting to get latest quote for assetId: {AssetId}", assetId);

            if (!int.TryParse(assetId, out var parsedAssetId))
            {
                _logger.LogWarning("Invalid Asset ID format: {AssetId}", assetId);
                // Consider throwing a specific ArgumentFormatException or returning a result object
                // For now, returning null or let it throw if that's desired upstream.
                // Throwing an exception might be better for API layer to catch and return 400.
                throw new FormatException($"Invalid Asset ID format: {assetId}");
            }

            var asset = await _assetRepository.GetByIdAsync(parsedAssetId).ConfigureAwait(false);
            if (asset == null)
            {
                _logger.LogWarning("Asset with ID {ParsedAssetId} not found.", parsedAssetId);
                return null; // Or throw a NotFoundException
            }

            var quote = await _quoteRepository.GetLatestQuoteAsync(parsedAssetId).ConfigureAwait(false);
            if (quote == null)
            {
                _logger.LogWarning("No quote found for Asset ID {ParsedAssetId}", parsedAssetId);
                return null; // Or throw a NotFoundException
            }

            var dto = new LatestQuoteDto
            {
                AssetId = asset.Ticker, // Use ticker for display
                Price = quote.Price,
                Timestamp = quote.QuoteDth,
                Source = "Database" // Assuming this service primarily deals with DB quotes
            };

            _logger.LogInformation("Successfully fetched latest quote for Asset ID {ParsedAssetId} (Ticker: {Ticker})", parsedAssetId, asset.Ticker);
            return dto;
        }
    }
}
