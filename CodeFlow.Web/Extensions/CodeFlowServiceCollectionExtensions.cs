using CodeFlow.core.Data;
using CodeFlow.core.Repositories;
using CodeFlow.core.Repositories.AuthServices;
using CodeFlow.core.Repositories.Seed;
using CodeFlow.core.Servies;

namespace CodeFlow.Web.Extensions
{
    public static class CodeFlowServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeFlowServices(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IAnswerRepository, AnswerRepository>();
            services.AddScoped<IVoteRepository, VoteRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IReputationRepository, ReputationRepository>();
            services.AddScoped<IBadgeRepository, BadgeRepository>();
            services.AddScoped<IUserActivityRepository, UserActivityRepository>();
            services.AddScoped<IAuthServices, AuthServices>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<BadgeDataSeed>();
            services.AddScoped<IMarkdownService, MarkdownService>();

            return services;
        }
    }
}
