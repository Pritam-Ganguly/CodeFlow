using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Identity
{
    public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DapperUserStore(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO Users (DisplayName, Email, PasswordHash, CreatedAt, Reputation)
                VALUES (@UserName, @Email, @PasswordHash, @CreatedAt, 1)
                RETURNING id";

            var newId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                UserName = user.UserName,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                CreatedAt = DateTime.UtcNow
            });

            user.Id = newId;
            return IdentityResult.Success;

        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public void Dispose(){}

        public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(int.TryParse(userId, out int id))
            {
                return await FindByIdAsync(id);
            }
            return null;
        }

        private async Task<ApplicationUser?> FindByIdAsync(int id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"SELECT id, DisplayName as UserName, Email, PasswordHash FROM Users WHERE Id=@id";
            var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new
            {
                Id = id
            });
            return user;
        }

        public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT Id, DisplayName as UserName, PasswordHash FROM Users WHERE UPPER(DisplayName) = UPPER(@NormalizedUserName)";
            var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new
            {
                NormalizedUserName = normalizedUserName
            });
            return user;
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
    }
}
