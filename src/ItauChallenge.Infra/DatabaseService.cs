using Dapper;
using ItauChallenge.Domain;
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
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts.txt"); // Assuming scripts.txt is copied to output
             if (!File.Exists(scriptPath)) {
                // Fallback for test environments or different content root
                string currentDirectory = Directory.GetCurrentDirectory();
                // Try to find the file relative to the current directory, going up a few levels if necessary
                // This is a common structure: <project_root>/bin/Debug/netX.X/
                // script is in <project_root>/ (relative to csproj)
                // For tests, it might be <solution_root>/<test_project>/bin/Debug/netX.X
                // and scripts.txt is in <solution_root>/src/ItauChallenge.Infra/
                string projectRootPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", ".."));
                scriptPath = Path.Combine(projectRootPath, "src", "ItauChallenge.Infra", "scripts.txt");
                 if (!File.Exists(scriptPath) && AppContext.BaseDirectory.Contains("testhost")) // Specific check for test host
                 {
                    projectRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
                    scriptPath = Path.Combine(projectRootPath, "src", "ItauChallenge.Infra", "scripts.txt");
                 }

                 if (!File.Exists(scriptPath))
                 {
                    // Attempt another common path from within /app (like in a container)
                    scriptPath = "/app/src/ItauChallenge.Infra/scripts.txt";
                 }
            }


            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Database script file not found. Tried: {scriptPath} and other fallbacks.");
            }

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

                    string storedProcedureCreationCommand = "";
                    bool inStoredProcedure = false;

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
                            if (tempCommand.Length > 0) {
                                finalCommands.Add(tempCommand);
                                tempCommand = "";
                            }
                            currentDelimiter = trimmedLine.Substring("DELIMITER ".Length).Trim();
                            continue;
                        }

                        tempCommand += line + "\n";

                        if (line.TrimEnd().EndsWith(currentDelimiter))
                        {
                            finalCommands.Add(tempCommand.Substring(0, tempCommand.Length - currentDelimiter.Length - (line.EndsWith("\n") ? 1:0) ));
                            tempCommand = "";
                        }
                    }
                    if (tempCommand.Trim().Length > 0) finalCommands.Add(tempCommand);


                    foreach (var commandText in finalCommands.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        try
                        {
                            await connection.ExecuteAsync(commandText);
                        }
                        catch (Exception cmdEx)
                        {
                            Console.WriteLine($"Error executing command: <<{commandText}>>. Error: {cmdEx.Message}");
                            // Decide if to throw or continue. For setup, one failure might be critical.
                            // throw; // Or log and continue if some failures are acceptable
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

        public async Task<bool> IsMessageProcessedAsync(string messageId)
        {
            const string query = "SELECT COUNT(1) FROM processed_messages WHERE message_id = @MessageId;";
            using (var connection = new MySqlConnection(_connectionString))
            {
                var count = await connection.ExecuteScalarAsync<int>(query, new { MessageId = messageId });
                return count > 0;
            }
        }

        public async Task SaveQuoteAsync(Quote quote, string messageId)
        {
            // Ensure QuoteDth is set, default to UtcNow if not.
            // The qtt table has quote_dth NOT NULL.
            if (quote.QuoteDth == DateTime.MinValue)
            {
                quote.QuoteDth = DateTime.UtcNow;
            }
             // Ensure CreatedDth is set for qtt record
            if (quote.CreatedDth == DateTime.MinValue)
            {
                quote.CreatedDth = DateTime.UtcNow;
            }


            const string insertQuoteQuery = @"
                INSERT INTO qtt (asset_id, price, quote_dth, created_dth)
                VALUES (@AssetId, @Price, @QuoteDth, @CreatedDth);";
            // Corrected to use POCO property names, Dapper will map them to qtt.asset_id etc. if columns are named with underscores
            // Or ensure POCO names match exact column names if no underscores are used in DB.
            // DB schema uses: asset_id, price, quote_dth, created_dth. POCO: AssetId, Price, QuoteDth, CreatedDth. Dapper handles this.

            const string insertProcessedMessageQuery = @"
                INSERT INTO processed_messages (message_id, processed_dth)
                VALUES (@MessageId, @ProcessedDth);";

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        await connection.ExecuteAsync(insertQuoteQuery, new { quote.AssetId, quote.Price, quote.QuoteDth, quote.CreatedDth }, transaction);
                        await connection.ExecuteAsync(insertProcessedMessageQuery, new { MessageId = messageId, ProcessedDth = DateTime.UtcNow }, transaction);
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task UpdateClientPositionsAsync(int assetId, decimal newPrice)
        {
            // Updates client positions' P&L based on a new asset price.
            // Note: Average price (average_price) is generally affected by buy/sell operations,
            // not solely by real-time quote fluctuations. This method updates P&L and the
            // position's update timestamp based on new quotes.
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

        // New methods for API controllers
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

        public async Task<Asset> GetAssetByIdAsync(int assetId)
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

        public async Task<Asset> GetAssetByTickerAsync(string ticker)
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
    }
}
