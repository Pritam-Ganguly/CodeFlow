using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    public class FlagRepository : IFlagRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<FlagRepository> _logger;       

        public FlagRepository(IDbConnectionFactory connectionFactory, ILogger<FlagRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a <see cref="Flag"/> by its identifier including its <see cref="FlagType"/>,
        /// reporting user and resolving user (if any).
        /// </summary>
        /// <param name="id">The flag identifier.</param>
        /// <returns>The <see cref="Flag"/> if found; otherwise <c>null</c>.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<Flag?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("GetByIdAsync starting for FlagId={FlagId}", id);

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"
                        SELECT f.*, ft.*, ru.*, rbu.* 
                        FROM Flags f
                        LEFT JOIN FlagTypes ft ON f.FlagTypeId = ft.Id
                        LEFT JOIN Users ru ON f.ReportingUserId = ru.Id
                        LEFT JOIN Users rbu ON f.ResolvedByUserId = rbu.Id
                        WHERE f.Id = @Id";

                var flag = await connection.QueryAsync<Flag, FlagType, User, User, Flag>(
                        sql,
                        map: (flag, flagType, reportingUser, resolvedByUser) =>
                        {
                            flag.FlagType = flagType;
                            flag.ReportingUser = reportingUser;
                            flag.ResolvedByUser = resolvedByUser;
                            return flag;
                        },
                        param: new { Id = id },
                        splitOn: "Id,Id,Id"
                    );

                var result = flag.FirstOrDefault();
                _logger.LogInformation("GetByIdAsync completed for FlagId={FlagId}, Found={Found}", id, result != null);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByIdAsync for FlagId={FlagId}", id);
                throw;
            }
        }

        /// <summary>
        /// Returns pending flags ordered by severity and creation time.
        /// </summary>
        /// <param name="limit">Maximum number of flags to return (default 20).</param>
        /// <returns>Collection of pending <see cref="Flag"/> objects.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<IEnumerable<Flag>> GetPendingFlagsAsync(int limit = 20)
        {
            try
            {
                _logger.LogInformation("GetPendingFlagsAsync starting with limit={Limit}", limit);

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @" SELECT f.*, ft.*, ru.*
                            FROM Flags f
                            LEFT JOIN FlagTypes ft ON f.FlagTypeId = ft.Id
                            LEFT JOIN Users ru ON f.ReportingUserId = ru.Id
                            WHERE f.Status = 'Pending'
                            ORDER BY 
                                    CASE WHEN ft.SeverityLevel = 3 THEN 1
                                         WHEN ft.SeverityLevel = 2 THEN 2
                                         ELSE 3 END,
                                    f.CreatedAt DESC
                            LIMIT @Limit;";

                var results = await connection.QueryAsync<Flag, FlagType, User, Flag>(
                    sql,
                    map: (flag, flagType, user) =>
                    {
                        flag.ReportingUser = user;
                        flag.FlagType = flagType;
                        return flag;
                    },
                    param: new { Limit = limit },
                    splitOn: "Id,Id");

                _logger.LogInformation("GetPendingFlagsAsync completed, returned {Count} records", results?.AsList().Count ?? 0);
                return results ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPendingFlagsAsync with limit={Limit}", limit);
                throw;
            }
        }               

        /// <summary>
        /// Inserts a new flag and returns the generated Id.
        /// </summary>
        /// <param name="flag">The flag to create. The <see cref="Flag.PostType"/> is stored as its string representation.</param>
        /// <returns>Generated flag Id if insert succeeds; otherwise <c>null</c>.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<int?> CreateFlagAsync(Flag flag)
        {
            try
            {
                _logger.LogInformation("CreateFlagAsync starting for PostType={PostType}, PostId={PostId}, ReportingUserId={ReportingUserId}", flag.PostType, flag.PostId, flag.ReportingUserId);

                using var connection = await _connectionFactory.CreateConnectionAsync();

                var sql = @"
                        INSERT INTO Flags (FlagTypeId, PostType, PostId, ReportingUserId, Reason)
                        VALUES (@FlagTypeId, @PostType, @PostId, @ReportingUserId, @Reason)
                        RETURNING Id";

                var id = await connection.ExecuteScalarAsync<int?>(sql, new
                {
                    flag.FlagTypeId,
                    PostType = flag.PostType.ToString(),
                    flag.PostId,
                    flag.ReportingUserId,
                    flag.Reason,
                });

                _logger.LogInformation("CreateFlagAsync created FlagId={FlagId} for PostId={PostId}", id, flag.PostId);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFlagAsync for PostId={PostId}, ReportingUserId={ReportingUserId}", flag?.PostId, flag?.ReportingUserId);
                throw;
            }
        }

        /// <summary>
        /// Updates the status and resolution information for a flag.
        /// </summary>
        /// <param name="flagId">Identifier of the flag to update.</param>
        /// <param name="status">New status value.</param>
        /// <param name="resolvedByUserId">User id who resolved or dismissed the flag.</param>
        /// <param name="resolutionNotes">Optional resolution notes.</param>
        /// <returns><c>true</c> when a row was updated; otherwise <c>false</c>.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<bool> UpdateStatusAsync(int flagId, FlagStatusType status, int resolvedByUserId, string? resolutionNotes)
        {
            try
            {
                _logger.LogInformation("UpdateStatusAsync starting for FlagId={FlagId}, Status={Status}, ResolvedByUserId={ResolvedByUserId}", flagId, status, resolvedByUserId);

                using var connection = await _connectionFactory.CreateConnectionAsync();

                var sql = @"
                            UPDATE Flags
                            SET Status = @Status,
                                ResolutionNotes = @ResolutionNotes,
                                ResolvedByUserId = @ResolvedByUserId,
                                UpdatedAt = NOW()
                            WHERE Id = @Id";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    Id = flagId,
                    Status = status.ToString(),
                    ResolutionNotes = resolutionNotes,
                    ResolvedByUserId = resolvedByUserId
                });

                var success = rowsAffected > 0;
                _logger.LogInformation("UpdateStatusAsync completed for FlagId={FlagId}, Success={Success}", flagId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusAsync for FlagId={FlagId}", flagId);
                throw;
            }
        }

        /// <summary>
        /// Checks whether a specific user already reported a given post.
        /// </summary>
        /// <param name="postType">Type of the post (Question, Answer, Comment).</param>
        /// <param name="postId">Identifier of the post.</param>
        /// <param name="reportingUserId">Identifier of the reporting user.</param>
        /// <returns><c>true</c> if a report by the user exists for the post; otherwise <c>false</c>.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<bool> GetIfUserHaveAlreadyReported(FlagPostType postType, int postId, int reportingUserId)
        {
            try
            {
                _logger.LogInformation("GetIfUserHaveAlreadyReported starting for PostType={PostType}, PostId={PostId}, ReportingUserId={ReportingUserId}", postType, postId, reportingUserId);

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT * FROM Flags WHERE PostType = @PostType AND PostId = @PostId AND ReportingUserId = @ReportingUserId";

                var reports = await connection.QueryAsync<Flag>(sql, new
                {
                    PostType = postType.ToString(),
                    PostId = postId,
                    ReportingUserId = reportingUserId
                });

                var exists = reports.Any();
                _logger.LogInformation("GetIfUserHaveAlreadyReported result for PostId={PostId}: {Exists}", postId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetIfUserHaveAlreadyReported for PostId={PostId}, ReportingUserId={ReportingUserId}", postId, reportingUserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all configured flag types.
        /// </summary>
        /// <returns>Collection of <see cref="FlagType"/> entries.</returns>
        /// <exception cref="System.Exception">Exceptions thrown by the DB layer are logged and rethrown.</exception>
        public async Task<IEnumerable<FlagType>> GetAllFlagTypes()
        {
            try
            {
                _logger.LogInformation("GetAllFlagTypes starting");

                using var connection = await _connectionFactory.CreateConnectionAsync();
                var sql = @"SELECT * FROM FlagTypes";

                var res = await connection.QueryAsync<FlagType>(sql);
                _logger.LogInformation("GetAllFlagTypes completed, returned {Count} types", res?.AsList().Count ?? 0);
                return res ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllFlagTypes");
                throw;
            }
        }

    }
}
