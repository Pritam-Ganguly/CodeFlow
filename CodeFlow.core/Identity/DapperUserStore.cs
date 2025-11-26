using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CodeFlow.core.Identity
{
    public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<DapperUserStore> _logger;

        public DapperUserStore(IDbConnectionFactory connectionFactory, ILogger<DapperUserStore> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new application user in the database asynchronously.
        /// </summary>
        /// <param name="user">The ApplicationRole instance to create. Must not be null.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
        /// <returns>
        /// An IdentityResult indicating the outcome of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the role parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellation token.</exception>
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if(user == null)
            {
                const string errorMessage = "User parameter cannot be null.";
                _logger.LogError(errorMessage);
                throw new ArgumentNullException(nameof(user), errorMessage);
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Attempting to create new user: {UserName}", user.UserName);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var sql = @"
                INSERT INTO Users (DisplayName, Email, PasswordHash, CreatedAt, Reputation)
                VALUES (@UserName, @Email, @PasswordHash, @CreatedAt, 1)
                RETURNING id";

                var newRoleId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    user.UserName,
                    user.Email,
                    user.PasswordHash,
                    CreatedAt = DateTime.UtcNow
                });
                user.Id = newRoleId;
                _logger.LogInformation("User created successfully with id {UserId} for user {Username}", newRoleId, user.UserName);
                return IdentityResult.Success;
            }
            catch (OperationCanceledException)
            {
                var errorMessage = $"Operation to create a new user for {user.UserName} was cancelled.";
                _logger.LogInformation(errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "OperationCancelled",
                    Description = errorMessage
                });
            }
            catch (NpgsqlException dbException) when (dbException.SqlState == "23505")
            {
                var(errorCode, errorMessage) = ParseDuplicateKeyError(dbException, user);
                _logger.LogWarning(errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = errorCode,
                    Description = errorMessage
                });
            }
            catch(Exception ex)
            {
                var errorMessage = $"An error occured when creating a user {user.UserName}: {ex.Message}";
                _logger.LogWarning(errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserCreationFailed",
                    Description = errorMessage
                });
            }
        }

        /// <summary>
        /// Finds a user by its id.
        /// </summary>
        /// <param name="userId">Id value of user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
        /// <returns>
        /// The ApplicationUser details if found, otherwise null.
        /// </returns>
        public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(!int.TryParse(userId, out int id))
            {
                _logger.LogWarning("Failed to parse User Id '{userId}' as Integer", userId);
                return null;
            }
            return await FindByIdAsync(id);
        }

        /// <summary>
        /// Finds a user by its id.
        /// </summary>
        /// <param name="id">Id value of user.</param>
        /// <returns>
        /// The ApplicationUser details if found, otherwise null.
        /// </returns>
        private async Task<ApplicationUser?> FindByIdAsync(int id)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT id, DisplayName as UserName, Email, PasswordHash FROM Users WHERE Id=@Id";
                var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new
                {
                    Id = id
                });
                if (user != null)
                {
                    _logger.LogDebug("Successfully found user with Id: {UserId}: {UserName}", id, user.UserName);
                }
                else
                {
                    _logger.LogDebug("No user found with Id: {UserId}", id);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An unexpected error occurred while finding user ID {UserId}. Error: {ErrorMessage}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Finds a user by its normalized user name.
        /// </summary>
        /// <param name="normalizedUserName"> value of user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
        /// <returns>
        /// The ApplicationUser details if found, otherwise null.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellation token.</exception>
        public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = "SELECT Id, DisplayName as UserName, PasswordHash FROM Users WHERE UPPER(DisplayName) = UPPER(@NormalizedUserName)";
                var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new
                {
                    NormalizedUserName = normalizedUserName
                });
                if (user != null)
                {
                    _logger.LogDebug("Successfully found user with name: {UserName}: {UserName}", normalizedUserName, user.UserName);
                }
                else
                {
                    _logger.LogDebug("No user found with name: {UserName}", normalizedUserName);
                }
                return user;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation to find user with name {Username} was cancelled", normalizedUserName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An unexpected error occurred while finding user name {UserName}. Error: {ErrorMessage}", normalizedUserName, ex.Message);
                throw;
            }
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

        public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id +"");

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public void Dispose() { }

        public static (string ErrorCode, string ErrorMessage) ParseDuplicateKeyError(NpgsqlException exception, ApplicationUser user)
        {
            var message = exception.Message.ToLowerInvariant();

            // Parse the error message to determine which field caused the violation
            if (message.Contains("users_displayname") || message.Contains("displayname") ||
                message.Contains("username") || (message.Contains("unique") && message.Contains("display_name")))
            {
                return ("DuplicateUserName",
                    $"The display name '{user.UserName}' is already taken. Please choose another.");
            }

            if (message.Contains("users_email") || message.Contains("email") ||
                (message.Contains("unique") && message.Contains("email")))
            {
                return ("DuplicateEmail",
                    $"The email address '{user.Email}' is already registered.");
            }

            return ("DuplicateUser",
                    "A user with these details already exists. Please try a different display name or email address.");
        }
    }
}
