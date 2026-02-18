using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CodeFlow.core.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(IDbConnectionFactory dbConnectionFactory, ILogger<NotificationRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int?> CreateNotification(Notification notification)
        {
            if (notification is null)
            {
                _logger.LogWarning("CreateNotification called with null notification.");
                return null;
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                var sql = @"
                       INSERT INTO Notifications (Type, Message, UserId, QuestionId, IsRead, CreatedAt) 
                       VALUES (@Type, @Message, @UserId, @QuestionId, @IsRead, @CreatedAt)
                       RETURNING Id";

                _logger.LogInformation("Executing CreateNotification SQL for UserId={UserId}, QuestionId={QuestionId}", notification.UserId, notification.QuestionId);

                var id = await connection.ExecuteScalarAsync<int?>(sql, notification);

                _logger.LogInformation("Created notification with Id={NotificationId} for UserId={UserId}", id, notification.UserId);

                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for UserId={UserId}, QuestionId={QuestionId}", notification.UserId, notification.QuestionId);
                return null;
            }
        }

        public async Task<bool> UpdateNotification(int notificationId, bool isRead)
        {
            if (notificationId <= 0)
            {
                _logger.LogWarning("UpdateNotification called with invalid notificationId={NotificationId}", notificationId);
                return false;
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                var sql = @"UPDATE Notifications SET IsRead = @IsRead WHERE Id = @Id";

                _logger.LogInformation("Executing UpdateNotification SQL for Id={NotificationId}, IsRead={IsRead}", notificationId, isRead);

                var count = await connection.ExecuteAsync(sql, new
                {
                    IsRead = isRead,
                    Id = notificationId
                });

                var success = count > 0;
                _logger.LogInformation("UpdateNotification result for Id={NotificationId}: {Success} (rowsAffected={Count})", notificationId, success, count);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification Id={NotificationId}", notificationId);
                return false;
            }
        }

        public async Task<IEnumerable<Notification>?> GetAllNotificationForUserId(int userId, int limit = 50)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("GetAllNotificationForUserId called with invalid userId={UserId}", userId);
                return null;
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                var sql = @"SELECT n.*, u.*, q.* FROM Notifications n 
                        LEFT JOIN Users u ON n.UserId = u.Id
                        LEFT JOIN Questions q ON n.QuestionId = q.Id
                        WHERE n.UserId = @UserId
                        ORDER BY n.CreatedAt DESC
                        LIMIT @Limit";

                _logger.LogInformation("Executing GetAllNotificationForUserId SQL for UserId={UserId}, Limit={Limit}", userId, limit);

                var notifications = await connection.QueryAsync<Notification, User, Question, Notification>(sql,
                    map: (notification, user, question) =>
                    {
                        notification.User = user;
                        notification.Question = question;
                        return notification;
                    },
                    param: new
                    {
                        UserId = userId,
                        Limit = limit
                    },
                    splitOn: "Id,Id");

                _logger.LogInformation("Retrieved {Count} notifications for UserId={UserId}", notifications?.AsList().Count ?? 0, userId);

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for UserId={UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<Notification>?> GetNotification(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("GetNotification called with invalid id={Id}", id);
                return null;
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                var sql = @"SELECT n.*, u.*, q.* FROM Notifications n 
                        LEFT JOIN Users u ON n.UserId = u.Id
                        LEFT JOIN Questions q ON n.QuestionId = q.Id
                        WHERE n.Id = @Id";

                _logger.LogInformation("Executing GetNotification SQL for Id={Id}", id);

                var notification = await connection.QueryAsync<Notification, User, Question, Notification>(sql,
                    map: (notification, user, question) =>
                    {
                        notification.User = user;
                        notification.Question = question;
                        return notification;
                    },
                    param: new
                    {
                        Id = id
                    },
                    splitOn: "Id,Id");

                _logger.LogInformation("Retrieved {Count} records for notification Id={Id}", notification?.AsList().Count ?? 0, id);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification Id={Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<Notification>?> GetAllUnreadNotification(int userId, int limit = 10)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("GetAllUnreadNotification called with invalid userId={UserId}", userId);
                return null;
            }

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync();

                var sql = @"SELECT n.*, u.*, q.* FROM Notifications n 
                        LEFT JOIN Users u ON n.UserId = u.Id
                        LEFT JOIN Questions q ON n.QuestionId = q.Id
                        WHERE n.UserId = @UserId AND n.IsRead = false
                        ORDER BY n.CreatedAt DESC
                        LIMIT @Limit";

                _logger.LogInformation("Executing GetAllUnreadNotification SQL for UserId={UserId}, Limit={Limit}", userId, limit);

                var notifications = await connection.QueryAsync<Notification, User, Question, Notification>(sql,
                    map: (notification, user, question) =>
                    {
                        notification.User = user;
                        notification.Question = question;
                        return notification;
                    },
                    param: new
                    {
                        UserId = userId,
                        Limit = limit,
                    },
                    splitOn: "Id,Id");

                _logger.LogInformation("Retrieved {Count} unread notifications for UserId={UserId}", notifications?.AsList().Count ?? 0, userId);

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications for UserId={UserId}", userId);
                return null;
            }
        }

    }
}
