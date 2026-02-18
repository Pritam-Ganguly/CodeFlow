using CodeFlow.core.Data;
using CodeFlow.core.Models.Services;
using CodeFlow.core.Repositories;
using CodeFlow.core.Repositories.AuthServices;
using CodeFlow.core.Repositories.Decorators;
using CodeFlow.core.Repositories.Seed;
using CodeFlow.core.Servies;
using CodeFlow.Web.Hubs;
using CodeFlow.Web.Hubs.Services;
using Microsoft.AspNetCore.SignalR;

namespace CodeFlow.Web.Extensions
{
    public static class CodeFlowServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeFlowServices(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IQuestionRepository>(providers =>
            {
                var questionInnerRepository = new QuestionRepository(
                    providers.GetRequiredService<IDbConnectionFactory>(),
                    providers.GetRequiredService<IReputationRepository>(),
                    providers.GetRequiredService<IUserActivityRepository>(),
                    providers.GetRequiredService<IMarkdownService>(),
                    providers.GetRequiredService<ILogger<QuestionRepository>>()
                    );

                return new CachedQuestionRepository(questionInnerRepository,
                    providers.GetRequiredService<IRedisCacheService>(),
                    providers.GetRequiredService<IRedisCacheInvalidationService>(),
                    providers.GetRequiredService<ILogger<CachedQuestionRepository>>());

            });

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
            services.AddScoped<IRedisCacheService, RedisCacheService>();
            services.AddScoped<IHotQuestionCacheService, HotQuestionCacheService>();
            services.AddScoped<IRedisQuestionPopularityService, RedisQuestionPopularityService>();
            services.AddScoped<IRedisCacheInvalidationService, RedisCacheInvalidationService>();
            services.AddScoped<IFlagRepository, FlagRepository>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            return services;
        }
    }
}
