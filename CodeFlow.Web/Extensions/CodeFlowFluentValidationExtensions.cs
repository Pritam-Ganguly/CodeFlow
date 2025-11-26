using CodeFlow.Web.Models;
using FluentValidation;

namespace CodeFlow.Web.Extensions
{
    public static class CodeFlowFluentValidationExtensions
    {
        public static IServiceCollection AddCustomValidationServices(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<CreateRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<EditRequestModelValidator>();
            return services;
        }
    }
}
