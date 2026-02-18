using AngleSharp.Css;
using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeFlow.core.Identity
{
    public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
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
                    _logger.LogInformation("Successfully found user with Id: {UserId}: {UserName}", id, user.UserName);
                }
                else
                {
                    _logger.LogInformation("No user found with Id: {UserId}", id);
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
                    _logger.LogInformation("Successfully found user with name: {UserName}: {UserName}", normalizedUserName, user.UserName);
                }
                else
                {
                    _logger.LogInformation("No user found with name: {UserName}", normalizedUserName);
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

        /// <summary>
        /// Adds the specified user to the named role. Ensures role exists and avoids duplicate user-role rows.
        /// Uses a transaction and Postgres-safe INSERT ... ON CONFLICT pattern to avoid races.
        /// </summary>
        public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("roleName cannot be null or whitespace.", nameof(roleName));

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Adding user {UserId} to role {RoleName}", user.Id, roleName);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            IDbTransaction? transaction = null;
            try
            {
                transaction = connection.BeginTransaction();

                // Try fetch existing role id
                const string sqlFetchRoleId = @"SELECT Id FROM Roles WHERE Name = @RoleName";
                var roleId = await connection.ExecuteScalarAsync<int?>(sqlFetchRoleId, new { RoleName = roleName }, transaction);

                if (roleId == null)
                {
                    // Try insert role - if a concurrent insert happens, ON CONFLICT DO NOTHING prevents error.
                    const string sqlInsertRoleReturning = @"INSERT INTO Roles (Name) VALUES (@RoleName) ON CONFLICT (Name) DO NOTHING RETURNING Id";
                    roleId = await connection.ExecuteScalarAsync<int?>(sqlInsertRoleReturning, new { RoleName = roleName }, transaction);

                    if (roleId == null)
                    {
                        // Another process likely inserted the role concurrently; re-fetch id.
                        roleId = await connection.ExecuteScalarAsync<int?>(sqlFetchRoleId, new { RoleName = roleName }, transaction);
                    }
                }

                if (roleId == null)
                {
                    throw new InvalidOperationException($"Unable to determine role id for role '{roleName}'.");
                }

                // Avoid duplicate user-role entries
                const string sqlCheckRelation = @"SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
                var existing = await connection.ExecuteScalarAsync<int>(sqlCheckRelation, new { UserId = user.Id, RoleId = roleId }, transaction);

                if (existing == 0)
                {
                    const string sqlInsertRelation = @"INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
                    await connection.ExecuteAsync(sqlInsertRelation, new { UserId = user.Id, RoleId = roleId }, transaction);
                    _logger.LogInformation("Added user {UserId} to role {RoleName} (RoleId={RoleId})", user.Id, roleName, roleId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} is already in role {RoleName} (RoleId={RoleId}) - skipping insert", user.Id, roleName, roleId);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user {UserId} to role {RoleName}", user.Id, roleName);
                try { transaction?.Rollback(); } catch { /* swallow rollback errors */ }
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        /// <summary>
        /// Checks if the specified user is in the given role.
        /// Returns true if a matching user-role relation exists.
        /// </summary>
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("roleName cannot be null or whitespace.", nameof(roleName));

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Checking role membership for user {UserId} in role {RoleName}", user.Id, roleName);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // Use COUNT to avoid type-mapping issues and to keep SQL simple/portable
                const string sql = @"
                    SELECT COUNT(1)
                    FROM UserRoles ur
                    JOIN Roles r ON ur.RoleId = r.Id
                    WHERE ur.UserId = @UserId AND r.Name = @RoleName";

                var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = user.Id, RoleName = roleName });
                var isInRole = count > 0;

                _logger.LogInformation("IsInRole result for user {UserId} role {RoleName}: {IsInRole}", user.Id, roleName, isInRole);
                return isInRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role membership for user {UserId} in role {RoleName}", user.Id, roleName);
                throw;
            }
        }

        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets all roles for the specified user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Checking all roles for {Name}", user.UserName);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // Use COUNT to avoid type-mapping issues and to keep SQL simple/portable
                const string sql = @"
                    SELECT r.Name
                    FROM UserRoles ur
                    JOIN Roles r ON ur.RoleId = r.Id
                    WHERE ur.UserId = @UserId";

                var result = await connection.QueryAsync<string>(sql, new { UserId = user.Id});

                _logger.LogInformation("Found all {count} roles for user", result.Count());

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking roles for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets all users in the specified role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("roleName cannot be null or whitespace.", nameof(roleName));

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Checking all users in role {RoleName}", roleName);

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // Use COUNT to avoid type-mapping issues and to keep SQL simple/portable
                const string sql = @"
                    SELECT u.*
                    FROM Users u 
                    LEFT JOIN UserRoles ur ON u.Id = ur.UserId
                    LEFT JOIN Roles r ON ur.RoleId = r.Id
                    WHERE UPPER(r.Name) = UPPER(@RoleName)";

                var users = await connection.QueryAsync<ApplicationUser>(sql,
                    param: new
                    {
                        RoleName = roleName,
                    }
                 );

                _logger.LogInformation("Fount {Count} user for {RoleName}", users.Count(), roleName);
                return users.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for user in role {RoleName}", roleName);
                throw;
            }
            ;
        }

        public void Dispose() { }
    }
}
