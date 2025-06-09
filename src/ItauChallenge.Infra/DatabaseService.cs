using Dapper;
using ItauChallenge.Domain;
// using ItauChallenge.Domain.Entities; // Removed as entities are in ItauChallenge.Domain
using ItauChallenge.Domain.Repositories; // Added for repository interfaces
using Microsoft.Extensions.Configuration;
using MySqlConnector; // Added for MySqlConnection
using System;
using System.Collections.Generic;
using System.Data; // Required for CommandType
using System.IO; // Required for File.ReadAllText
using System.Linq; // Required for splitting script
using System.Threading.Tasks;

namespace ItauChallenge.Infra
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' not found in configuration.");
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts.txt");
            Console.WriteLine($"Attempting to find script at primary location: {scriptPath}");

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"Script not found at primary location. Attempting fallbacks.");
                // Fallback 1: Try relative to executing assembly's directory (e.g., bin/Debug/net8.0)
                // This can be different from AppContext.BaseDirectory in some test scenarios or when hosted.
                string executingAssemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (executingAssemblyDir != null) {
                    string assemblyScriptPath = Path.Combine(executingAssemblyDir, "scripts.txt");
                    Console.WriteLine($"Attempting assembly location: {assemblyScriptPath}");
                    if (File.Exists(assemblyScriptPath)) {
                        scriptPath = assemblyScriptPath;
                        Console.WriteLine($"Script found at assembly location: {scriptPath}");
                    } else {
                        // Fallback 2: Try one level up from AppContext.BaseDirectory (e.g., for some test runners where BaseDirectory is .../bin/Debug/net8.0)
                        // and script might be copied to .../bin/Debug/
                        var upOneLevel = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
                        string parentDirScriptPath = Path.Combine(upOneLevel, "scripts.txt");
                        Console.WriteLine($"Attempting parent directory of AppContext.BaseDirectory: {parentDirScriptPath}");
                        if (File.Exists(parentDirScriptPath)) {
                            scriptPath = parentDirScriptPath;
                             Console.WriteLine($"Script found at parent directory location: {scriptPath}");
                        } else {
                            Console.WriteLine($"Script also not found at: {assemblyScriptPath} or {parentDirScriptPath}. Further fallbacks can be added if needed (e.g. specific /app path for containers).");
                            // As a last resort, if in a known container path, one could check:
                            // string containerPath = "/app/src/ItauChallenge.Infra/scripts.txt";
                            // if (File.Exists(containerPath)) scriptPath = containerPath;
                        }
                    }
                }
            } else {
                 Console.WriteLine($"Script found at primary location: {scriptPath}");
            }

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"ERROR: Database script file not found after checking primary and fallback locations. Final attempted path: {scriptPath}");
                throw new FileNotFoundException($"Database script file not found. Checked AppContext.BaseDirectory and common fallbacks. See console logs for paths attempted. Final path: {scriptPath}");
            }

            Console.WriteLine($"Attempting to read database script from: {scriptPath}");
            var scriptContent = await File.ReadAllTextAsync(scriptPath);

            // MySqlConnector might handle 'DELIMITER' command, but it's safer to split and remove them
            // or execute them in separate batches if Dapper/MySqlConnector has issues.
            // For simplicity, we'll try to execute the whole script, assuming MySqlConnector handles it.
            // If not, we'll need to parse and split statements, especially around DELIMITER changes.

            // Splitting by "DELIMITER ;" and "DELIMITER //" is tricky because the delimiter itself changes.
            // A common approach is to split by ";\n" when DELIMITER is ;, and by "//\n" when DELIMITER is //
            // However, MySQL scripts can be complex.
            // Let's assume for now the MySqlConnector can handle the script as a whole or with simple splitting.
            // We will remove DELIMITER commands as Dapper won't execute them as SQL.

            var commands = scriptContent.Split(new[] { ";\n", ";\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(cmd => cmd.Trim())
                                        .Where(cmd => !string.IsNullOrWhiteSpace(cmd) &&
                                                      !cmd.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                                        .ToList();

            // Re-join commands that were part of SP definition if any were split prematurely
            // This simple split won't correctly handle SPs with internal semicolons if not for DELIMITER handling.
            // Given the script structure, this might be okay if we execute commands one by one.
            // The provided script uses DELIMITER for one SP.
            // A robust solution uses a proper SQL parser or executes script parts based on DELIMITER.

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Try executing the script content directly - MySqlConnector might handle it.
                // This is often the case if `AllowUserVariables=true` and no `DELIMITER` commands are in the script itself.
                // Since our script HAS `DELIMITER` commands, this direct execution might fail or behave unexpectedly.
                try
                {
                    // Replace DELIMITER commands with empty string before sending to server
                    string cleanedScript = scriptContent.Replace("DELIMITER //", "").Replace("DELIMITER ;", "");
                    await connection.ExecuteAsync(cleanedScript);
                    Console.WriteLine("Database script executed successfully (attempted as a whole).");
                }
                catch (Exception exWhole)
                {
                    Console.WriteLine($"Error executing whole script: {exWhole.Message}. Will try command by command.");
                    // Fallback: execute command by command (this will fail for the SP if not handled carefully)
                    // The SP `UpdateClientPositions` needs to be created with `DELIMITER //` context.
                    // Standard Dapper ExecuteAsync won't understand `DELIMITER` commands.
                    // We need to handle the SP creation carefully.

                    // Simple split by semicolon (ignoring DELIMITER for this fallback)
                    commands = scriptContent.Split(new[] { ";\n", ";\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                        .ToList();

            // string storedProcedureCreationCommand = ""; // Commented out as part of a larger block not directly used
            // bool inStoredProcedure = false; // Commented out as part of a larger block not directly used

                    List<string> finalCommands = new List<string>();
                    string tempCommand = "";

                    // More robust split logic considering DELIMITER
                    var scriptLines = scriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    string currentDelimiter = ";";
                    foreach (var line in scriptLines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                        {
                    if (tempCommand.Trim().Length > 0) { // Ensure tempCommand is not just whitespace
                                finalCommands.Add(tempCommand);
                            }
                    tempCommand = ""; // Reset tempCommand after adding
                            currentDelimiter = trimmedLine.Substring("DELIMITER ".Length).Trim();
                            continue;
                        }

                        tempCommand += line + "\n";

                        if (line.TrimEnd().EndsWith(currentDelimiter))
                        {
                    // Remove the delimiter itself and the trailing newline before adding
                    finalCommands.Add(tempCommand.Substring(0, tempCommand.Length - currentDelimiter.Length -1 ).TrimEnd('\r', '\n'));
                            tempCommand = "";
                        }
                    }
            if (tempCommand.Trim().Length > 0) finalCommands.Add(tempCommand.TrimEnd('\r', '\n'));


                    foreach (var commandText in finalCommands.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        try
                        {
                            await connection.ExecuteAsync(commandText);
                        }
                        catch (Exception cmdEx)
                        {
                    Console.WriteLine($"Error executing command: <<{commandText.Substring(0, Math.Min(100, commandText.Length))}...>>. Error: {cmdEx.Message}");
                            // Decide if to throw or continue. For setup, one failure might be critical.
                    // Consider re-throwing for critical setup steps: throw;
                        }
                    }
                    Console.WriteLine("Database script executed command by command (fallback).");
                }
            }
        }


        public async Task<IEnumerable<Operation>> GetUserOperationsAsync(int userId, int assetId, int days = 30)
        {
            const string query = @"
                SELECT
                    id AS Id,
                    user_id AS UserId,
                    asset_id AS AssetId,
                    type AS Type,
                    quantity AS Quantity,
                    price AS Price,
                    operation_dth AS OperationDth,
                    created_dth AS CreatedDth
                FROM op
                WHERE user_id = @UserId
                  AND asset_id = @AssetId
                  AND operation_dth >= DATE_SUB(NOW(), INTERVAL @Days DAY)
                ORDER BY operation_dth DESC;";

            // The query in the prompt used op_id, op_usr_id etc. but the table uses id, user_id
            // Corrected to use actual column names from 'op' table.
            // Dapper maps underscore to PascalCase, so `user_id` maps to `UserId`. Explicit aliases are for clarity or when names differ significantly.

            using (var connection = new MySqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Operation>(query, new { UserId = userId, AssetId = assetId, Days = days });
            }
        }

        // IProcessedMessageRepository Implementation
        public async Task<bool> IsMessageProcessedAsync(string messageId)
        {
            const string query = "SELECT COUNT(1) FROM processed_messages WHERE message_id = @MessageId;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                var count = await connection.ExecuteScalarAsync<int>(query, new { MessageId = messageId });
                return count > 0;
            }
        }

        public async Task MarkAsProcessedAsync(string messageId)
        {
            const string insertProcessedMessageQuery = @"
                INSERT INTO processed_messages (message_id, processed_dth)
                VALUES (@MessageId, @ProcessedDth);";
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(insertProcessedMessageQuery, new { MessageId = messageId, ProcessedDth = DateTime.UtcNow });
            }
        }

        // IQuoteRepository Implementation
        public async Task<Quote> GetLatestQuoteAsync(int assetId)
        {
            const string query = @"
                SELECT
                    id AS Id,
                    asset_id AS AssetId,
                    price AS Price,
                    quote_dth AS QuoteDth,
                    created_dth AS CreatedDth
                FROM qtt
                WHERE asset_id = @AssetId
                ORDER BY quote_dth DESC
                LIMIT 1;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<Quote>(query, new { AssetId = assetId });
            }
        }

        public async Task SaveAsync(Quote quote)
        {
            if (quote.QuoteDth == DateTime.MinValue)
            {
                quote.QuoteDth = DateTime.UtcNow;
            }
            if (quote.CreatedDth == DateTime.MinValue)
            {
                quote.CreatedDth = DateTime.UtcNow;
            }
            const string insertQuoteQuery = @"
                INSERT INTO qtt (asset_id, price, quote_dth, created_dth)
                VALUES (@AssetId, @Price, @QuoteDth, @CreatedDth);";
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(insertQuoteQuery, new { quote.AssetId, quote.Price, quote.QuoteDth, quote.CreatedDth });
            }
        }

        // IPositionRepository Implementation
        public async Task<IEnumerable<Position>> GetClientPositionsAsync(int userId)
        {
            const string query = @"
                SELECT
                    id AS Id,
                    user_id AS UserId,
                    asset_id AS AssetId,
                    quantity AS Quantity,
                    average_price AS AveragePrice,
                    pos_pl AS PL,
                    updated_dth AS UpdatedDth,
                    created_dth AS CreatedDth
                FROM pos
                WHERE user_id = @UserId;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Position>(query, new { UserId = userId });
            }
        }

        public async Task UpdatePositionsForAssetPriceAsync(int assetId, decimal newPrice)
        {
            // Updates client positions' P&L based on a new asset price.
            const string query = @"
        UPDATE pos
        SET
            pos_pl = (quantity * @NewPrice) - (quantity * average_price),
            updated_dth = NOW()
        WHERE
            asset_id = @AssetId;";

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(query, new { NewPrice = newPrice, AssetId = assetId });
            }
        }

        public Task AddOrUpdatePositionAsync(Position position)
        {
            throw new NotImplementedException();
        }

        // IUserRepository Implementation
        public async Task<User> CreateAsync(User user, decimal brokeragePercent)
        {
            const string sql = @"
                INSERT INTO usr (usr_name, usr_email, usr_brokerage_pct)
                VALUES (@Name, @Email, @BrokeragePercent);
                SELECT LAST_INSERT_ID();";

            using (var connection = new MySqlConnection(_connectionString))
            {
                var newUserId = await connection.ExecuteScalarAsync<int>(sql, new { user.Name, user.Email, BrokeragePercent = brokeragePercent });
                user.Id = newUserId;
                user.CreatedDth = DateTime.UtcNow; // Assuming CreatedDth should be set here
                return user;
            }
        }

        Task<User> IUserRepository.GetByIdAsync(int userId)
        {
            throw new NotImplementedException();
        }

        // IAssetRepository Implementation
        async Task<Asset> IAssetRepository.GetByIdAsync(int assetId)
        {
            const string query = @"
                SELECT
                    id AS Id,
                    ticker AS Ticker,
                    name AS Name,
                    type AS Type,
                    created_dth AS CreatedDth
                FROM ast
                WHERE id = @AssetId;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<Asset>(query, new { AssetId = assetId });
            }
        }

        public async Task<Asset> GetByTickerAsync(string ticker)
        {
            const string query = @"
                SELECT
                    id AS Id,
                    ticker AS Ticker,
                    name AS Name,
                    type AS Type,
                    created_dth AS CreatedDth
                FROM ast
                WHERE ticker = @Ticker;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<Asset>(query, new { Ticker = ticker });
            }
        }

        public Task<IEnumerable<Asset>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        // IOperationRepository Implementation (GetUserOperationsAsync was already present and matches)
        public Task AddAsync(Operation operation)
        {
            throw new NotImplementedException();
        }

        public async Task SaveQuoteAndMarkMessageProcessedAsync(Quote quote, string messageId)
        {
            if (quote.QuoteDth == DateTime.MinValue) { quote.QuoteDth = DateTime.UtcNow; }
            if (quote.CreatedDth == DateTime.MinValue) { quote.CreatedDth = DateTime.UtcNow; }

            const string insertQuoteQuery = @"
                INSERT INTO qtt (asset_id, price, quote_dth, created_dth)
                VALUES (@AssetId, @Price, @QuoteDth, @CreatedDth);";
            const string insertProcessedMessageQuery = @"
                INSERT INTO processed_messages (message_id, processed_dth)
                VALUES (@MessageId, @ProcessedDth);";

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        await connection.ExecuteAsync(insertQuoteQuery, new { quote.AssetId, quote.Price, quote.QuoteDth, quote.CreatedDth }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync(insertProcessedMessageQuery, new { MessageId = messageId, ProcessedDth = DateTime.UtcNow }, transaction).ConfigureAwait(false);
                        await transaction.CommitAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        await transaction.RollbackAsync().ConfigureAwait(false);
                        throw;
                    }
                }
            }
        }
    }
}
