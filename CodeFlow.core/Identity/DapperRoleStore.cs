using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Identity
{
    public class DapperRoleStore : IRoleStore<ApplicationRole>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public DapperRoleStore(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = "INSERT INTO Roles (Name) VALUES (@Name) RETURNING Id";
            var result = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Name = role.Name
            });
            role.Id = result;
            return IdentityResult.Success;

        }

        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public void Dispose()
        {

        }

        public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM Roles WHERE Id = @Id";
            if(int.TryParse(roleId, out int id))
            {
                var result = await connection.QuerySingleOrDefaultAsync<ApplicationRole>(sql, new
                {
                    Id = id
                });
                return result;
            }

            return null;

        }

        public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM Roles WHERE Name = @Name";
            var result = await connection.QuerySingleOrDefaultAsync<ApplicationRole>(sql, new
            {
                Name = normalizedRoleName
            });
            return null;
        }

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
    }
}
