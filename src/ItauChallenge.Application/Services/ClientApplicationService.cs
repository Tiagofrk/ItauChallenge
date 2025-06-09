using ItauChallenge.Contracts.Dtos;
using ItauChallenge.Application.Services;
using ItauChallenge.Domain; // For OperationType, Purchase, FinancialCalculations
using ItauChallenge.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public class ClientApplicationService : IClientApplicationService
    {
        private readonly IPositionRepository _positionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IQuoteRepository _quoteRepository;
        private readonly ILogger<ClientApplicationService> _logger;

        public ClientApplicationService(
            IPositionRepository positionRepository,
            IAssetRepository assetRepository,
            IOperationRepository operationRepository,
            IQuoteRepository quoteRepository,
            ILogger<ClientApplicationService> logger)
        {
            _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
            _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
            _operationRepository = operationRepository ?? throw new ArgumentNullException(nameof(operationRepository));
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ClientPositionDto>> GetClientPositionsAsync(int userId)
        {
            _logger.LogInformation("Fetching client positions for UserId: {UserId}", userId);
            var positions = await _positionRepository.GetClientPositionsAsync(userId).ConfigureAwait(false);

            if (positions == null || !positions.Any())
            {
                _logger.LogInformation("No positions found for UserId: {UserId}", userId);
                return Enumerable.Empty<ClientPositionDto>();
            }

            var clientPositions = new List<ClientPositionDto>();
            // Assuming one ClientPositionDto per client, aggregating all asset positions.
            // The current DTO structure seems to imply one ClientPositionDto holds all assets for that client.

            var assetPositionsDto = new List<AssetPositionDto>();
            decimal totalPortfolioValue = 0;

            foreach (var pos in positions)
            {
                var asset = await _assetRepository.GetByIdAsync(pos.AssetId).ConfigureAwait(false);
                string assetTicker = asset?.Ticker ?? pos.AssetId.ToString();

                var latestQuote = await _quoteRepository.GetLatestQuoteAsync(pos.AssetId).ConfigureAwait(false);
                decimal currentMarketPrice = latestQuote?.Price ?? 0m; // Use 0 if no quote available

                decimal totalValue = pos.Quantity * currentMarketPrice;
                // P&L might need to be recalculated here if `pos.PL` is not live or if definition changes.
                // For now, using pos.PL as stored, assuming it's updated by other processes (like price updates).

                assetPositionsDto.Add(new AssetPositionDto
                {
                    AssetId = assetTicker,
                    Quantity = pos.Quantity,
                    AverageAcquisitionPrice = pos.AveragePrice,
                    CurrentMarketPrice = currentMarketPrice,
                    TotalValue = totalValue,
                    ProfitOrLoss = totalValue - (pos.Quantity * pos.AveragePrice) // Recalculate P/L based on current price
                });
                totalPortfolioValue += totalValue;
            }

            // The DTO seems to be designed for one client, so we create one main DTO.
            // If the request was for *multiple* clients, this structure would need adjustment.
            var clientPositionDto = new ClientPositionDto
            {
                ClientId = userId.ToString(), // DTO uses string for ClientId
                Assets = assetPositionsDto,
                TotalPortfolioValue = totalPortfolioValue,
                AsOfDate = DateTime.UtcNow
            };

            // Since the interface is IEnumerable<ClientPositionDto>, we return a list containing the single client's DTO.
            // This could be re-evaluated if the intent is different.
            // If GetClientPositionsAsync is always for *one* client, perhaps the return type should be ClientPositionDto directly.
            // Sticking to interface for now.
            clientPositions.Add(clientPositionDto);

            return clientPositions;
        }

        public async Task<AveragePriceDto> GetAveragePurchasePriceAsync(int userId, string assetTicker)
        {
            _logger.LogInformation("Calculating average purchase price for UserId: {UserId}, AssetTicker: {AssetTicker}", userId, assetTicker);

            var asset = await _assetRepository.GetByTickerAsync(assetTicker).ConfigureAwait(false);
            if (asset == null)
            {
                _logger.LogWarning("Asset with ticker {AssetTicker} not found.", assetTicker);
                // Or throw NotFoundException / return error DTO
                return null;
            }

            // Fetch all operations to ensure all purchases are included. A large number for 'days' signifies this.
            // Consider if IOperationRepository needs a method to get all operations for an asset without a day limit,
            // or if passing int.MaxValue for days is acceptable. For now, using a very large number like 10 years.
            var operations = await _operationRepository.GetUserOperationsAsync(userId, asset.Id, days: 365 * 10).ConfigureAwait(false);

            if (operations == null || !operations.Any())
            {
                _logger.LogWarning("No operations found for UserId {UserId}, AssetId {AssetId}", userId, asset.Id);
                return new AveragePriceDto { UserId = userId.ToString(), AssetId = assetTicker, AveragePrice = 0, TotalQuantity = 0, CalculationDate = DateTime.UtcNow };
            }

            var buyOperations = operations.Where(op => op.Type == OperationType.Compra).ToList();
            if (!buyOperations.Any())
            {
                _logger.LogWarning("No 'Compra' (Buy) operations found for UserId {UserId}, AssetId {AssetId}", userId, asset.Id);
                return new AveragePriceDto { UserId = userId.ToString(), AssetId = assetTicker, AveragePrice = 0, TotalQuantity = 0, CalculationDate = DateTime.UtcNow };
            }

            var purchases = buyOperations.Select(op => new Purchase(op.Quantity, op.Price)).ToList();
            var averagePrice = FinancialCalculations.CalculateWeightedAveragePrice(purchases);
            var totalQuantity = buyOperations.Sum(op => op.Quantity);

            return new AveragePriceDto
            {
                UserId = userId.ToString(),
                AssetId = assetTicker,
                AveragePrice = averagePrice,
                TotalQuantity = totalQuantity,
                CalculationDate = DateTime.UtcNow
            };
        }
    }
}
