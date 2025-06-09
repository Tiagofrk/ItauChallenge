using Microsoft.Extensions.Configuration;
using ItauChallenge.Infra;
using Dapper;
using MySqlConnector;
using ItauChallenge.Domain; // Added for Position and other domain models
using ItauChallenge.Domain.Repositories; // Added for repository interfaces

namespace ItauChallenge.Infra.Tests
{
    [TestClass]
    public class DatabaseServiceTests
    {
        private static IConfiguration _configuration;
        private static string _connectionString;
        private static IDatabaseService _databaseService;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Ensures it finds appsettings in test output
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                .Build();

            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Test database connection string 'DefaultConnection' not found in appsettings.Test.json.");
            }

            // Ensure the database itself exists, but it might be empty or non-existent initially.
            // The InitializeDatabaseAsync should handle schema creation.
            // For tests, it's common to drop and recreate the database or ensure it's clean.
            // For now, we assume the DB server is running and the specified DB can be created/managed by the script.

            // Clean up database before tests (optional, good for idempotency)
            // This is a simplified cleanup. A more robust version would drop all known tables.
            try
            {
                using (var connection = new MySqlConnection(_connectionString.Replace("Database=itau_challenge_test_db", "Database=mysql"))) // Connect to default db
                {
                    await connection.ExecuteAsync("DROP DATABASE IF EXISTS itau_challenge_test_db;");
                    await connection.ExecuteAsync("CREATE DATABASE itau_challenge_test_db;");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to drop/create test database. This might be okay if DB doesn't exist yet. Error: {ex.Message}");
                // Depending on CI/local setup, you might not have permissions to drop/create DBs.
                // In such cases, ensure the DB is clean manually or via other scripts.
            }


            _databaseService = new DatabaseService(_configuration);
            // Initialize the database once for all tests in this class
            await _databaseService.InitializeDatabaseAsync();
        }

        [TestMethod]
        public async Task Test_InitializeDatabase_ShouldCreateSchema()
        {
            // The InitializeDatabaseAsync is called in ClassInitialize.
            // This test verifies if it successfully created the schema by querying a table.
            bool schemaCreated = false;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    // Check if a known table (e.g., 'usr') exists by trying to query it.
                    // A more specific check might query INFORMATION_SCHEMA.TABLES.
                    var result = await connection.QueryAsync("SELECT COUNT(*) FROM usr;");
                    schemaCreated = true; // If query does not throw, table exists.
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed, but schemaCreated will remain false.
                Console.WriteLine($"Schema verification query failed: {ex.Message}");
                schemaCreated = false;
            }
            Assert.IsTrue(schemaCreated, "Database schema was not created successfully, or 'usr' table is missing.");
        }

        [TestMethod]
        public async Task Test_GetUserOperations_ShouldReturnData()
        {
            // Arrange: Ensure DB is initialized (done in ClassInitialize)
            // Insert test data
            int testUserId;
            int testAssetId;
            DateTime operationTime = DateTime.UtcNow.AddDays(-5); // Ensure it's within default 30 day window

            using (var connection = new MySqlConnection(_connectionString))
            {
                // Create User
                testUserId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO usr (name, email, created_dth) VALUES (@Name, @Email, @CreatedDth); SELECT LAST_INSERT_ID();",
                    new { Name = "Test User Ops", Email = "testops@example.com", CreatedDth = DateTime.UtcNow });

                // Create Asset
                testAssetId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO ast (ticker, name, type, created_dth) VALUES (@Ticker, @Name, @Type, @CreatedDth); SELECT LAST_INSERT_ID();",
                    new { Ticker = "TSTOPS", Name = "Test Ops Asset", Type = "Stock", CreatedDth = DateTime.UtcNow });

                // Create Operations
                var op1 = new ItauChallenge.Domain.Operation // Use domain object for clarity, though direct values are fine for insert
                {
                    UserId = testUserId,
                    AssetId = testAssetId,
                    Type = ItauChallenge.Domain.OperationType.Compra,
                    Quantity = 100,
                    Price = 10.50m,
                    OperationDth = operationTime,
                    CreatedDth = DateTime.UtcNow
                };
                var op2 = new ItauChallenge.Domain.Operation
                {
                    UserId = testUserId,
                    AssetId = testAssetId,
                    Type = ItauChallenge.Domain.OperationType.Compra,
                    Quantity = 50,
                    Price = 11.00m,
                    OperationDth = operationTime.AddHours(1),
                    CreatedDth = DateTime.UtcNow
                };

                await connection.ExecuteAsync(
                    @"INSERT INTO op (user_id, asset_id, type, quantity, price, operation_dth, created_dth)
                      VALUES (@UserId, @AssetId, @Type, @Quantity, @Price, @OperationDth, @CreatedDth);",
                    new[] { op1, op2 });
            }

            // Act
            var operations = await ((IOperationRepository)_databaseService).GetUserOperationsAsync(testUserId, testAssetId, 30);

            // Assert
            Assert.IsNotNull(operations, "Operations list should not be null.");
            Assert.AreEqual(2, operations.Count(), "Should retrieve 2 operations.");

            var firstOp = operations.OrderBy(o => o.OperationDth).First();
            Assert.AreEqual(100, firstOp.Quantity);
            Assert.AreEqual(10.50m, firstOp.Price);
            Assert.AreEqual(ItauChallenge.Domain.OperationType.Compra, firstOp.Type);

            // Optional: Clean up specific test data if not relying on full DB drop/recreate per run/class
            // For this setup, ClassInitialize with DB drop should handle cleanup for subsequent class runs.
        }

        [TestMethod]
        public async Task Test_SaveQuote_And_CheckMessageProcessed_ShouldWork()
        {
            // Arrange: DB initialized in ClassInitialize
            int testAssetId;
            string messageId = $"test-message-{Guid.NewGuid()}";
            DateTime quoteTime = DateTime.UtcNow.AddMinutes(-10);

            using (var connection = new MySqlConnection(_connectionString))
            {
                // Create Asset (or use one if already created, ensure it's clean for this test if needed)
                var existingAsset = await connection.QueryFirstOrDefaultAsync<ItauChallenge.Domain.Asset>("SELECT * FROM ast WHERE ticker = @Ticker", new { Ticker = "TSTQT" });
                if (existingAsset == null)
                {
                    testAssetId = await connection.ExecuteScalarAsync<int>(
                        "INSERT INTO ast (ticker, name, type, created_dth) VALUES (@Ticker, @Name, @Type, @CreatedDth); SELECT LAST_INSERT_ID();",
                        new { Ticker = "TSTQT", Name = "Test Quote Asset", Type = "Stock", CreatedDth = DateTime.UtcNow });
                }
                else
                {
                    testAssetId = existingAsset.Id;
                }
            }

            var testQuote = new ItauChallenge.Domain.Quote
            {
                AssetId = testAssetId,
                Price = 150.75m,
                QuoteDth = quoteTime, // Specific time for the quote
                CreatedDth = DateTime.UtcNow // System time for DB record creation
            };

            // Act & Assert - Part 1: Save Quote and check IsMessageProcessedAsync
            await ((IQuoteRepository)_databaseService).SaveAsync(testQuote);
            await ((IProcessedMessageRepository)_databaseService).MarkAsProcessedAsync(messageId);

            bool isProcessed = await ((IProcessedMessageRepository)_databaseService).IsMessageProcessedAsync(messageId);
            Assert.IsTrue(isProcessed, "IsMessageProcessedAsync should return true for the saved messageId.");

            bool isNotProcessed = await ((IProcessedMessageRepository)_databaseService).IsMessageProcessedAsync($"non-existent-{Guid.NewGuid()}");
            Assert.IsFalse(isNotProcessed, "IsMessageProcessedAsync should return false for a non-existent messageId.");

            // Act & Assert - Part 2: Verify quote was saved correctly
            using (var connection = new MySqlConnection(_connectionString))
            {
                var savedQuote = await connection.QueryFirstOrDefaultAsync<ItauChallenge.Domain.Quote>(
                    "SELECT asset_id as AssetId, price as Price, quote_dth as QuoteDth FROM qtt WHERE asset_id = @AssetId ORDER BY created_dth DESC LIMIT 1",
                    new { AssetId = testAssetId });

                Assert.IsNotNull(savedQuote, "Saved quote should not be null.");
                Assert.AreEqual(testAssetId, savedQuote.AssetId);
                Assert.AreEqual(150.75m, savedQuote.Price);
                // MySQL DATETIME(6) precision can lead to minor differences if not careful with rounding or exact storage.
                // Compare with a tolerance or ensure DateTimeKind is consistent.
                Assert.IsTrue(Math.Abs((savedQuote.QuoteDth - quoteTime).TotalSeconds) < 1, "QuoteDth should match the saved quote's timestamp closely.");
            }
        }

        [TestMethod]
        public async Task UpdateClientPositionsAsync_ShouldCalculatePositivePL()
        {
            // Arrange
            int testUserId;
            int testAssetId;
            decimal initialAveragePrice = 100m;
            int quantity = 10;
            decimal initialPL = 0m; // Assuming P&L starts at 0 for a new position record
            DateTime initialUpdatedDth;

            using (var connection = new MySqlConnection(_connectionString))
            {
                testUserId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO usr (name, email, created_dth) VALUES ('Test User PL', 'testpl@example.com', NOW()); SELECT LAST_INSERT_ID();");
                testAssetId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO ast (ticker, name, type, created_dth) VALUES ('TSTPL', 'Test PL Asset', 'Stock', NOW()); SELECT LAST_INSERT_ID();");

                initialUpdatedDth = DateTime.UtcNow.AddDays(-1); // Ensure it's in the past

                await connection.ExecuteAsync(
                    @"INSERT INTO pos (user_id, asset_id, quantity, average_price, pos_pl, updated_dth, created_dth)
                      VALUES (@UserId, @AssetId, @Quantity, @AveragePrice, @PL, @UpdatedDth, NOW());",
                    new { UserId = testUserId, AssetId = testAssetId, Quantity = quantity, AveragePrice = initialAveragePrice, PL = initialPL, UpdatedDth = initialUpdatedDth });
            }

            decimal newMarketPrice = 120m; // New price is higher
            decimal expectedPL = (quantity * newMarketPrice) - (quantity * initialAveragePrice); // (10 * 120) - (10 * 100) = 1200 - 1000 = 200

            // Act
            await ((IPositionRepository)_databaseService).UpdatePositionsForAssetPriceAsync(testAssetId, newMarketPrice);

            // Assert
            using (var connection = new MySqlConnection(_connectionString))
            {
                var updatedPosition = await connection.QuerySingleOrDefaultAsync<Position>(
                    "SELECT user_id UserId, asset_id AssetId, quantity Quantity, average_price AveragePrice, pos_pl PL, updated_dth UpdatedDth FROM pos WHERE user_id = @UserId AND asset_id = @AssetId",
                    new { UserId = testUserId, AssetId = testAssetId });

                Assert.IsNotNull(updatedPosition, "Position should exist.");
                Assert.AreEqual(expectedPL, updatedPosition.PL, "P&L was not calculated correctly for positive scenario.");
                Assert.IsTrue(updatedPosition.UpdatedDth > initialUpdatedDth, "UpdatedDth should have been updated to a more recent time.");
            }
        }

        [TestMethod]
        public async Task UpdateClientPositionsAsync_ShouldCalculateNegativePL()
        {
            // Arrange
            int testUserId;
            int testAssetId;
            decimal initialAveragePrice = 100m;
            int quantity = 10;
            decimal initialPL = 0m;
            DateTime initialUpdatedDth;

            using (var connection = new MySqlConnection(_connectionString))
            {
                testUserId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO usr (name, email, created_dth) VALUES ('Test User NegPL', 'testnegpl@example.com', NOW()); SELECT LAST_INSERT_ID();");
                testAssetId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO ast (ticker, name, type, created_dth) VALUES ('TSTNPL', 'Test Neg PL Asset', 'Stock', NOW()); SELECT LAST_INSERT_ID();");
                initialUpdatedDth = DateTime.UtcNow.AddDays(-1);

                await connection.ExecuteAsync(
                    @"INSERT INTO pos (user_id, asset_id, quantity, average_price, pos_pl, updated_dth, created_dth)
                      VALUES (@UserId, @AssetId, @Quantity, @AveragePrice, @PL, @UpdatedDth, NOW());",
                    new { UserId = testUserId, AssetId = testAssetId, Quantity = quantity, AveragePrice = initialAveragePrice, PL = initialPL, UpdatedDth = initialUpdatedDth });
            }

            decimal newMarketPrice = 80m; // New price is lower
            decimal expectedPL = (quantity * newMarketPrice) - (quantity * initialAveragePrice); // (10 * 80) - (10 * 100) = 800 - 1000 = -200

            // Act
            await ((IPositionRepository)_databaseService).UpdatePositionsForAssetPriceAsync(testAssetId, newMarketPrice);

            // Assert
            using (var connection = new MySqlConnection(_connectionString))
            {
                var updatedPosition = await connection.QuerySingleOrDefaultAsync<Position>(
                     "SELECT user_id UserId, asset_id AssetId, quantity Quantity, average_price AveragePrice, pos_pl PL, updated_dth UpdatedDth FROM pos WHERE user_id = @UserId AND asset_id = @AssetId",
                    new { UserId = testUserId, AssetId = testAssetId });

                Assert.IsNotNull(updatedPosition, "Position should exist.");
                Assert.AreEqual(expectedPL, updatedPosition.PL, "P&L was not calculated correctly for negative scenario.");
                Assert.IsTrue(updatedPosition.UpdatedDth > initialUpdatedDth, "UpdatedDth should have been updated.");
            }
        }

        [TestMethod]
        public async Task UpdateClientPositionsAsync_ShouldCalculateZeroPL()
        {
            // Arrange
            int testUserId;
            int testAssetId;
            decimal initialAveragePrice = 100m;
            int quantity = 10;
            decimal initialPL = 50m; // Start with some non-zero P&L
            DateTime initialUpdatedDth;

            using (var connection = new MySqlConnection(_connectionString))
            {
                testUserId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO usr (name, email, created_dth) VALUES ('Test User ZeroPL', 'testzeropl@example.com', NOW()); SELECT LAST_INSERT_ID();");
                testAssetId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO ast (ticker, name, type, created_dth) VALUES ('TSTZPL', 'Test Zero PL Asset', 'Stock', NOW()); SELECT LAST_INSERT_ID();");
                initialUpdatedDth = DateTime.UtcNow.AddDays(-1);

                await connection.ExecuteAsync(
                    @"INSERT INTO pos (user_id, asset_id, quantity, average_price, pos_pl, updated_dth, created_dth)
                      VALUES (@UserId, @AssetId, @Quantity, @AveragePrice, @PL, @UpdatedDth, NOW());",
                    new { UserId = testUserId, AssetId = testAssetId, Quantity = quantity, AveragePrice = initialAveragePrice, PL = initialPL, UpdatedDth = initialUpdatedDth });
            }

            decimal newMarketPrice = 100m; // New price is same as average
            decimal expectedPL = (quantity * newMarketPrice) - (quantity * initialAveragePrice); // (10 * 100) - (10 * 100) = 0

            // Act
            await ((IPositionRepository)_databaseService).UpdatePositionsForAssetPriceAsync(testAssetId, newMarketPrice);

            // Assert
            using (var connection = new MySqlConnection(_connectionString))
            {
                var updatedPosition = await connection.QuerySingleOrDefaultAsync<Position>(
                    "SELECT user_id UserId, asset_id AssetId, quantity Quantity, average_price AveragePrice, pos_pl PL, updated_dth UpdatedDth FROM pos WHERE user_id = @UserId AND asset_id = @AssetId",
                    new { UserId = testUserId, AssetId = testAssetId });

                Assert.IsNotNull(updatedPosition, "Position should exist.");
                Assert.AreEqual(expectedPL, updatedPosition.PL, "P&L was not calculated correctly for zero P&L scenario.");
                Assert.IsTrue(updatedPosition.UpdatedDth > initialUpdatedDth, "UpdatedDth should have been updated.");
            }
        }
    }
}
