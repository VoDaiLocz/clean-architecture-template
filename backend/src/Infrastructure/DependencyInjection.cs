using Application.Common.Health;
using Application.Common.Interfaces.Jobs;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Storage;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Health;
using Infrastructure.Jobs;
using Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Features.SourceExtraction;
using Infrastructure.Extraction;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName = "Development"
    )
    {
        services.AddHealthChecks();
        var options = ToeicPlatformOptions.FromConfiguration(configuration, environmentName);

        services.AddSingleton<IKnowledgeRepository>(_ =>
        {
            var repository = SqliteKnowledgeRepository.FromConnectionString(options.Database.ConnectionString);
            repository.Initialize();
            return repository;
        });
        services.AddSingleton<IAuthRepository>(_ =>
        {
            var repository = SqliteAuthRepository.FromConnectionString(options.Database.ConnectionString);
            repository.Initialize();
            return repository;
        });
        
        services.AddOptions<Infrastructure.Security.JwtSettings>()
            .BindConfiguration("JwtSettings")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<Application.Common.Interfaces.Security.IPasswordHasher, Infrastructure.Security.PasswordHasherService>();
        services.AddSingleton<Application.Common.Interfaces.Security.IJwtTokenGenerator, Infrastructure.Security.JwtTokenGenerator>();

        services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        services.AddSingleton(new BackgroundJobRetryPolicy(maxAttempts: 3));
        services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();
        services.AddSingleton<IPlatformHealthService, PlatformHealthService>();
        services.AddSingleton<IPdfTextBlockExtractor, PdfPigTextBlockExtractor>();
        services.AddSingleton<IAudioMetadataProbe, TagLibAudioMetadataProbe>();
        services.AddSingleton<IAnswerKeyParser, CsvAnswerKeyParser>();
        services.AddSingleton<ITranscriptParser, CsvTranscriptParser>();
        services.AddSingleton<IReadingDraftParser, RegexReadingDraftParser>();
        services.AddSingleton<IListeningDraftParser, CsvListeningDraftParser>();

        return services;
    }
}
