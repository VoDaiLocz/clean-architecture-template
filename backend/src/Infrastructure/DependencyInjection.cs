using Application.Common.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("ToeicDb") ?? "Data Source=toeic-normalization.db";

        services.AddSingleton<IKnowledgeRepository>(_ =>
        {
            var repository = SqliteKnowledgeRepository.FromConnectionString(connectionString);
            repository.Initialize();
            return repository;
        });

        return services;
    }
}
