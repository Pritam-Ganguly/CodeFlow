using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    /// <summary>
    /// Repository that provides data access operations for <see cref="User"/> and <see cref="UserProfile"/>.
    /// Adds logging and exception handling around database operations.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<UserRepository> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="connectionFactory">Factory to create database connections.</param>
        /// <param name="logger">Logger instance.</param>
        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a user by email address.
        /// </summary>
        /// <param name="email">Email of the user to retrieve.</param>
        /// <returns>The matching <see cref="User"/> or <c>null</c> if not found.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                var sql = "SELECT * FROM Users WHERE Email = @Email";
                using var connection = await _connectionFactory.CreateConnectionAsync();
                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user by id.
        /// </summary>
        /// <param name="id">Id of the user to retrieve.</param>
        /// <returns>The matching <see cref="User"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user does not exist.</exception>
        public async Task<User> GetByIdAsync(int id)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = "SELECT * FROM Users WHERE Id = @Id";
                User? user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });

                if (user == null)
                {
                    _logger.LogWarning("User not found with Id {Id}", id);
                    throw new InvalidOperationException("User doesn't exist.");
                }

                return user;
            }
            catch (InvalidOperationException)
            {
                // Already logged above - rethrow to preserve original exception type.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by id: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the <see cref="UserProfile"/> for a given user id.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <returns>The <see cref="UserProfile"/> or <c>null</c> if not found.</returns>
        public async Task<UserProfile?> GetUserProfile(int id)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT up.*, u.* FROM UserProfiles up INNER JOIN Users u ON up.UserId = u.Id WHERE up.UserId = @Id";

                var userProfiles = await connection.QueryAsync<UserProfile, User, UserProfile>(
                    sql,
                    map: (userProfile, user) =>
                    {
                        userProfile.User = user;
                        return userProfile;
                    },
                    param: new
                    {
                        Id = id
                    },
                    splitOn: "id");

                return userProfiles.SingleOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user profile for user id: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Creates a <see cref="UserProfile"/> for the given user id.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <returns>The newly created profile id, or <c>null</c> on failure.</returns>
        public async Task<int?> CreateUserProfile(int id)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"INSERT INTO UserProfiles (UserId) VALUES (@UserId) RETURNING Id";

                var result = await connection.ExecuteScalarAsync<int?>(sql, new
                {
                    UserId = id
                });

                _logger.LogInformation("Created user profile for user id {Id}, new profile id {ProfileId}", id, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user profile for user id: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Updates the bio on a user's profile.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="bio">New bio text.</param>
        /// <returns><c>true</c> when the update affected at least one row.</returns>
        public async Task<bool> UpdateUserProfileBio(int id, string bio)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"UPDATE UserProfiles SET Bio = @Bio WHERE UserId = @Id";

                var result = await connection.ExecuteAsync(sql, new
                {
                    Bio = bio,
                    Id = id,
                });

                _logger.LogInformation("Updated bio for user id {Id}. RowsAffected: {RowsAffected}", id, result);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user profile bio for user id: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Updates the profile image for a user's profile.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="profilePictureMimeType">MIME type of the picture.</param>
        /// <param name="profilePictureFileName">Original file name of the picture.</param>
        /// <param name="profilePicture">Binary contents of the picture.</param>
        /// <returns><c>true</c> when the update affected at least one row.</returns>
        public async Task<bool> UpdateUserProfileImage(int id, string profilePictureMimeType, string profilePictureFileName, byte[] profilePicture)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                        UPDATE UserProfiles SET ProfilePicture = @ProfilePicture,
                        ProfilePictureMimeType = @ProfilePictureMimeType,
                        ProfilePictureFileName = @ProfilePictureFileName WHERE UserId = @UserId";

                var result = await connection.ExecuteAsync(sql, new
                {
                    ProfilePicture = profilePicture,
                    ProfilePictureMimeType = profilePictureMimeType,
                    ProfilePictureFileName = profilePictureFileName,
                    UserId = id
                });

                _logger.LogInformation("Updated profile image for user id {Id}. FileName: {FileName}, MimeType: {MimeType}, BytesLength: {BytesLength}, RowsAffected: {RowsAffected}",
                    id, profilePictureFileName, profilePictureMimeType, profilePicture?.Length ?? 0, result);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile image for user id: {Id}", id);
                throw;
            }
        }
    }
}