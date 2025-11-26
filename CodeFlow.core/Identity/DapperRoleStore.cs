using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CodeFlow.core.Identity
{
    public class DapperRoleStore : IRoleStore<ApplicationRole>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<DapperRoleStore> _logger;

        public DapperRoleStore(IDbConnectionFactory dbConnectionFactory, ILogger<DapperRoleStore> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new application role in the database asynchronously.
        /// </summary>
        /// <param name="role">The ApplicationRole instance to create. Must not be null.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>
        /// An IdentityResult indicating the outcome of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the role parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellation token.</exception>
        public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                const string errorMessage = "Role parameter cannot be null.";
                _logger.LogError(errorMessage);
                throw new ArgumentNullException(nameof(role), errorMessage);
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Attempting to create new role: {RoleName}", role.Name);
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = "INSERT INTO Roles (Name) VALUES (@Name) RETURNING Id";
                var newRoleId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    role.Name
                });
                role.Id = newRoleId;
                _logger.LogInformation("Successfully created role: {RoleName} with ID: {RoleId}", role.Name, newRoleId);
                return IdentityResult.Success;
            }
            catch (OperationCanceledException)
            {
                var errorMessage = $"Operation to create a new role for {role.Name} was cancelled.";
                _logger.LogInformation(errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "OperationCancelled",
                    Description = errorMessage
                });
            }
            catch (NpgsqlException dbException) when (dbException.SqlState == "23505")
            {
                var errorMessage = $"Role with name '{role.Name}' already exists.";
                _logger.LogWarning(errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateRoleName",
                    Description = errorMessage
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while creating role '{role.Name}': {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleCreationFailed",
                    Description = errorMessage
                });
            }
        }

        /// <summary>
        /// Finds and role by it's Id.
        /// </summary>
        /// <param name="roleId">Valid Id value of role.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>
        /// The ApplicationRole details if found, otherwise null.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellation token.</exception>
        public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                _logger.LogDebug("FindByIdAsync called with null or empty value");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Attempting to find role by ID: {RoleID}", roleId);
            
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                if(!int.TryParse(roleId, out int parsedRoleId))
                {
                    _logger.LogWarning("Failed to parse Role Id '{roleId}' as Integer", roleId);
                    return null;
                }

                var sql = "SELECT * FROM Roles WHERE Id = @Id";
                var result = await connection.QuerySingleOrDefaultAsync<ApplicationRole>(sql, new
                {
                    Id = parsedRoleId
                });
                if(result != null)
                {
                    _logger.LogDebug("Successfully found role with Id: {RoleId}: {RoleName}", roleId, result.Name);
                }
                else
                {
                    _logger.LogDebug("No role found with Id: {RoleId}", roleId);
                }
                return result;

            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation to find role by Id for {RoleId} was cancelled", roleId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An unexpected error occurred while finding role ID {RoleId}. Error: {ErrorMessage}", roleId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Finds and role by it's normalized name.
        /// </summary>
        /// <param name="normalizedRoleName">Valid normalized naem value of role.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>
        /// The ApplicationRole details if found, otherwise null.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via cancellation token.</exception>
        public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                _logger.LogWarning("FindByNameAsync is called with invalid normalizedRoleName '{nomralizedRoleName}' value.", normalizedRoleName);
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Attempting to find role by normalized name: {NormalizedRoleName}", normalizedRoleName);

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();
                var sql = "SELECT * FROM Roles WHERE UPPER(Name) = UPPER(@NormalizedName)";
                var result = await connection.QuerySingleOrDefaultAsync<ApplicationRole>(sql, new
                {
                    NormalizedName = normalizedRoleName,
                });

                if (result != null)
                {
                    _logger.LogDebug("Successfully found role with normalized name: {NormalizedRoleName}: {RoleName}", normalizedRoleName, result.Name);
                }
                else
                {
                    _logger.LogDebug("No role found with normalized name: {NormalizedRoleName}", normalizedRoleName);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation to find role by normalized name for {NormalizedRoleName} was cancelled", normalizedRoleName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An unexpected error occurred while finding role by normalized name {NormalizedRoleName}. Error: {ErrorMessage}", normalizedRoleName, ex.Message);
                throw;
            }
        }

        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name?.ToUpperInvariant());
        }

        public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id.ToString());
        }

        public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }
        public void Dispose()
        {

        }
    }
}
