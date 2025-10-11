using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public QuestionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(Question question)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"
            INSERT INTO Questions (Title, Body, UserId)
            VALUES (@Title, @Body, @UserId)
            RETURNING Id;";

            var newId = await connection.ExecuteScalarAsync<int>(sql, question);
            return newId;
        }

        public async Task<Question?> GetByIdAsync(int id)
        {
            var sql = @"
            SELECT q.*, u.* 
            FROM Questions q 
            INNER JOIN Users u ON q.UserID = u.Id
            WHERE q.Id = @Id";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var questions = await connection.QueryAsync<Question, User, Question>(sql, map: (question, user) =>
            {
                question.User = user;
                return question;
            },
            param: new { Id = id },
             splitOn: "Id"
            );

            return questions.SingleOrDefault();
        }

        public async Task<Question?> GetByIdWithTagsAsync(int id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"SELECT q.*, u.*, t.*
                        FROM Questions q
                        INNER JOIN Users u ON q.UserId = u.Id
                        LEFT JOIN QuestionTags qt ON q.Id = qt.QuestionId
                        LEFT JOIN Tags t ON qt.TagId = t.Id
                        WHERE q.Id = @Id";
            Question? question = null;
            var results = await connection.QueryAsync<Question, User, Tag, Question>(
                sql,
                map: (q, u, t) =>
                {
                    if(question == null)
                    {
                        question = q;
                        question.User = u;
                    }
                    if(t != null && t.Id != 0)
                    {
                        question.Tags.Add(t);
                    }

                    return question;
                },
                param: new {Id = id},
                splitOn: "Id, Id"
            );

            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Question>> GetRecentAsync(int limit = 20)
        {
            var sql = @"
            SELECT q.*, u.*
            FROM Questions q
            INNER JOIN Users u ON q.UserId = u.Id
            ORDER BY q.CreatedAt DESC
            LIMIT @Limit";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryAsync<Question, User, Question>(sql, map: (question, user) =>
            {
                question.User = user;
                return question;
            },
            param: new { Limit = limit },
            splitOn: "Id"
            );
        }

        public async Task<IEnumerable<Question>> GetRecentWithTagsAsync(int limit = 20)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @" SELECT q.*, u.* FROM Questions q INNER JOIN Users u ON q.UserId = u.Id ORDER BY q.CreatedAt DESC LIMIT @Limit";

            var questions = await connection.QueryAsync<Question, User, Question>(
                sql, 
                map: (question, user) =>
                {
                    question.User = user;
                    return question;
                },
                param: new { Limit = limit },
                splitOn: "Id,Id"
            );

            var questionIds = questions.Select(q => q.Id).ToList();
            if (questionIds.Count != 0)
            {
                var tagsSql = @"SELECT qt.QuestionId, t.* FROM QuestionTags qt INNER JOIN Tags t ON qt.TagId = t.Id WHERE qt.QuestionId = ANY(@QuestionIds)";

                var tagsByQuestionId = await connection.QueryAsync<int, Tag, (int QuestionId, Tag Tag)>(
                    tagsSql,
                    map: (questionId, tag) => (questionId, tag),
                    param: new { QuestionIds = questionIds.ToArray() },
                    splitOn: "Id"
                );

                var tagsLookup = tagsByQuestionId.ToLookup(x => x.QuestionId, x => x.Tag);
                foreach(var question in questions)
                {
                    if (tagsLookup.Contains(question.Id))
                    {
                        question.Tags.AddRange(tagsLookup[question.Id]);
                    }
                }
            }

            return questions;

        }

        public async Task<IEnumerable<Question>> SearchAsync(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return await GetRecentAsync();
            }

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"SELECT q.*, u.* FROM Questions q INNER JOIN Users u ON q.UserId = u.Id WHERE q.SearchVector @@ plainto_tsquery('english', @SearchQuery)
                        ORDER BY ts_rank(q.SearchVector, plainto_tsquery('english', @SearchQuery)) DESC, q.CreatedAt DESC";

            return await connection.QueryAsync<Question, User, Question>(
                sql,
                map: (question, user) =>
                {
                    question.User = user;
                    return question;
                },
                param: new { SearchQuery = searchQuery },
                splitOn: "Id");
        }

        public async Task<int?> CurrentVoteAsync(int userId, int quesitonId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"SELECT VoteType FROM Votes WHERE UserId = @UserId AND QuestionId = @QuestionId";
            var existingVote = await connection.QueryFirstOrDefaultAsync<int?>(sql, new
            {
                UserId = userId,
                QuestionId = quesitonId
            });
            return existingVote;
        }

        public async Task<int?> CurrentVoteForAnswerItemAsync(int userId, int answerId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = @"SELECT VoteType FROM Votes WHERE UserId = @UserId AND AnswerId = @AnswerId";
            var existingVote = await connection.QueryFirstOrDefaultAsync<int?>(sql, new
            {
                UserId = userId,
                AnswerId = answerId
            });
            return existingVote;
        }
    }
}