/*
 This file is for a factory class that creates and opens Npgsql database connections asynchronously.
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace CodeFlow.core.Data
{
    /// <summary>
    /// Factory that creates and opens <see cref="NpgsqlConnection"/> instances.
    /// </summary>
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NpgsqlConnectionFactory> _logger;

        public NpgsqlConnectionFactory(IConfiguration configuration, ILogger<NpgsqlConnectionFactory> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates and opens an <see cref="IDbConnection"/> asynchronously.
        /// </summary>
        /// <returns>An opened <see cref="IDbConnection"/> ready for use.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the "DefaultConnection" is not configured.</exception>
        /// <exception cref="Exception">Any exception thrown while opening the connection is logged and rethrown.</exception>
        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("DefaultConnection string is not configured.");
                throw new InvalidOperationException("DefaultConnection string is not configured.");
            }

            var connection = new NpgsqlConnection(connectionString);

            try
            {
                _logger.LogDebug("Opening Npgsql connection.");
                await connection.OpenAsync();
                _logger.LogDebug("Npgsql connection opened successfully.");
                return connection;
            }
            catch (Exception ex)
            {
                // Ensure connection is disposed in case of failure.
                try { connection.Dispose(); } catch {}

                _logger.LogError(ex, "Failed to open Npgsql connection.");
                throw;
            }
        }
    }
}