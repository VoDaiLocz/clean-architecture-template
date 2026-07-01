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
#pragma warning disable CS0618
        services.AddSingleton<DemoLearnerSession>();
#pragma warning restore CS0618
        return services;
    }
}
