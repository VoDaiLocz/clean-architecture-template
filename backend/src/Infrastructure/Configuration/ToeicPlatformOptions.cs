using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

public sealed record ToeicPlatformOptions(DatabaseOptions Database)
{
    private const string LocalDatabaseConnectionString = "Data Source=toeic-normalization.db";

    public static ToeicPlatformOptions FromConfiguration(
        IConfiguration configuration,
        string environmentName
    )
    {
        var connectionString = configuration.GetConnectionString("ToeicDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (IsProduction(environmentName))
            {
                throw new InvalidOperationException(
                    "Production requires ConnectionStrings:ToeicDb."
                );
            }

            connectionString = LocalDatabaseConnectionString;
        }

        return new ToeicPlatformOptions(new DatabaseOptions(connectionString));
    }

    private static bool IsProduction(string environmentName) =>
        string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
}

public sealed record DatabaseOptions(string ConnectionString);
