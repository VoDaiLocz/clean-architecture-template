using Application.Features.Learner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = configuration;
        services.AddSingleton<DemoLearnerSession>();
        return services;
    }
}
