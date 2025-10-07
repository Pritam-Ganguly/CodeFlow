using CodeFlow.core.Data;
using CodeFlow.core.Models;
using Dapper;

namespace CodeFlow.core.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public TagRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task AddTagsToQuestionAsync(int quesitonId, IEnumerable<int> tagIds)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var questionTagEntries = tagIds.Select(tagId => new { QuestionId = quesitonId, TagId = tagId });
            var sql = "INSERT INTO QuestionTags (QuestionId, TagId) VALUES (@QuestionId, @TagId)";
            await connection.ExecuteAsync(sql, questionTagEntries);
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = "INSERT INTO Tags (Name, Description) VALUES (@Name, @Description) RETURNING Id";
            var newId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Name = tag.Name,
                Description = tag.Description,
            });
            tag.Id = newId;
            return tag;
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM Tags WHERE LOWER(Name) = LOWER(@Name)";
            return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { Name = name });
        }

        public async Task<IEnumerable<Tag>> GetTagsForQuestionAsync(int questionId)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            var sql = @"SELECT t.* FROM Tags t
                        INNER JOIN QuestionTags gt ON t.Id = qt.TagId
                        WHERE qt.QuestionId = @QuestionId";
            return await connection.QueryAsync<Tag>(sql, new
            {
                QuestionId = questionId,
            });
        }
    }
}
