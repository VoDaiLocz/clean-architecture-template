using Application.Common.Interfaces.Jobs;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Storage;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Jobs;
using Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName = "Development"
    )
    {
        var options = ToeicPlatformOptions.FromConfiguration(configuration, environmentName);

        services.AddSingleton<IKnowledgeRepository>(_ =>
        {
            var repository = SqliteKnowledgeRepository.FromConnectionString(options.Database.ConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        services.AddSingleton(new BackgroundJobRetryPolicy(maxAttempts: 3));
        services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();

        return services;
    }
}
