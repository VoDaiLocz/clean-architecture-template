using System;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Identity;
using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;

public sealed class SqliteAuthRepository : IAuthRepository, IDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteAuthRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public static SqliteAuthRepository InMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new SqliteAuthRepository(connection);
    }

    public static SqliteAuthRepository FromConnectionString(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return new SqliteAuthRepository(connection);
    }

    public void Initialize()
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS auth_users (
                user_id TEXT PRIMARY KEY,
                email_normalized TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                display_name TEXT NOT NULL,
                role TEXT NOT NULL,
                status TEXT NOT NULL,
                failed_login_attempts INTEGER NOT NULL,
                locked_until_utc TEXT,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS auth_refresh_tokens (
                refresh_token_id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                token_hash TEXT NOT NULL,
                expires_at_utc TEXT NOT NULL,
                revoked_at_utc TEXT,
                replaced_by_token_id TEXT,
                created_at_utc TEXT NOT NULL,
                FOREIGN KEY (user_id) REFERENCES auth_users(user_id)
            );
            
            CREATE INDEX IF NOT EXISTS idx_auth_refresh_tokens_user
                ON auth_refresh_tokens(user_id);
            """;
        command.ExecuteNonQuery();
    }

    public async Task<AuthUser?> GetUserByIdAsync(string userId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = 
            """
            SELECT user_id, email_normalized, password_hash, display_name, role, status, 
                   failed_login_attempts, locked_until_utc, created_at_utc, updated_at_utc
            FROM auth_users
            WHERE user_id = $user_id
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapAuthUser(reader);
        }
        return null;
    }

    public async Task<AuthUser?> GetUserByEmailAsync(string emailNormalized)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = 
            """
            SELECT user_id, email_normalized, password_hash, display_name, role, status, 
                   failed_login_attempts, locked_until_utc, created_at_utc, updated_at_utc
            FROM auth_users
            WHERE email_normalized = $email
            """;
        command.Parameters.AddWithValue("$email", emailNormalized);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapAuthUser(reader);
        }
        return null;
    }

    public async Task CreateUserAsync(AuthUser user)
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO auth_users (
                user_id, email_normalized, password_hash, display_name, role, status,
                failed_login_attempts, locked_until_utc, created_at_utc, updated_at_utc
            )
            VALUES (
                $user_id, $email_normalized, $password_hash, $display_name, $role, $status,
                $failed_login_attempts, $locked_until_utc, $created_at_utc, $updated_at_utc
            )
            """;
        
        command.Parameters.AddWithValue("$user_id", user.UserId);
        command.Parameters.AddWithValue("$email_normalized", user.EmailNormalized);
        command.Parameters.AddWithValue("$password_hash", user.PasswordHash);
        command.Parameters.AddWithValue("$display_name", user.DisplayName);
        command.Parameters.AddWithValue("$role", user.Role);
        command.Parameters.AddWithValue("$status", user.Status);
        command.Parameters.AddWithValue("$failed_login_attempts", user.FailedLoginAttempts);
        command.Parameters.AddWithValue("$locked_until_utc", user.LockedUntilUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$created_at_utc", user.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updated_at_utc", user.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserAsync(AuthUser user)
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            UPDATE auth_users SET
                email_normalized = $email_normalized,
                password_hash = $password_hash,
                display_name = $display_name,
                role = $role,
                status = $status,
                failed_login_attempts = $failed_login_attempts,
                locked_until_utc = $locked_until_utc,
                updated_at_utc = $updated_at_utc
            WHERE user_id = $user_id
            """;

        command.Parameters.AddWithValue("$user_id", user.UserId);
        command.Parameters.AddWithValue("$email_normalized", user.EmailNormalized);
        command.Parameters.AddWithValue("$password_hash", user.PasswordHash);
        command.Parameters.AddWithValue("$display_name", user.DisplayName);
        command.Parameters.AddWithValue("$role", user.Role);
        command.Parameters.AddWithValue("$status", user.Status);
        command.Parameters.AddWithValue("$failed_login_attempts", user.FailedLoginAttempts);
        command.Parameters.AddWithValue("$locked_until_utc", user.LockedUntilUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$updated_at_utc", user.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<AuthRefreshToken?> GetRefreshTokenAsync(string tokenId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = 
            """
            SELECT refresh_token_id, user_id, token_hash, expires_at_utc, revoked_at_utc, 
                   replaced_by_token_id, created_at_utc
            FROM auth_refresh_tokens
            WHERE refresh_token_id = $id
            """;
        command.Parameters.AddWithValue("$id", tokenId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapAuthRefreshToken(reader);
        }
        return null;
    }

    public async Task AddRefreshTokenAsync(AuthRefreshToken token)
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO auth_refresh_tokens (
                refresh_token_id, user_id, token_hash, expires_at_utc, revoked_at_utc,
                replaced_by_token_id, created_at_utc
            )
            VALUES (
                $refresh_token_id, $user_id, $token_hash, $expires_at_utc, $revoked_at_utc,
                $replaced_by_token_id, $created_at_utc
            )
            """;
        
        command.Parameters.AddWithValue("$refresh_token_id", token.RefreshTokenId);
        command.Parameters.AddWithValue("$user_id", token.UserId);
        command.Parameters.AddWithValue("$token_hash", token.TokenHash);
        command.Parameters.AddWithValue("$expires_at_utc", token.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$revoked_at_utc", token.RevokedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$replaced_by_token_id", token.ReplacedByTokenId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$created_at_utc", token.CreatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateRefreshTokenAsync(AuthRefreshToken token)
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            UPDATE auth_refresh_tokens SET
                revoked_at_utc = $revoked_at_utc,
                replaced_by_token_id = $replaced_by_token_id
            WHERE refresh_token_id = $refresh_token_id
            """;
        
        command.Parameters.AddWithValue("$refresh_token_id", token.RefreshTokenId);
        command.Parameters.AddWithValue("$revoked_at_utc", token.RevokedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$replaced_by_token_id", token.ReplacedByTokenId ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static AuthUser MapAuthUser(SqliteDataReader reader)
    {
        return new AuthUser
        {
            UserId = reader.GetString(0),
            EmailNormalized = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            DisplayName = reader.GetString(3),
            Role = reader.GetString(4),
            Status = reader.GetString(5),
            FailedLoginAttempts = reader.GetInt32(6),
            LockedUntilUtc = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)).ToUniversalTime(),
            CreatedAtUtc = DateTime.Parse(reader.GetString(8)).ToUniversalTime(),
            UpdatedAtUtc = DateTime.Parse(reader.GetString(9)).ToUniversalTime()
        };
    }

    private static AuthRefreshToken MapAuthRefreshToken(SqliteDataReader reader)
    {
        return new AuthRefreshToken
        {
            RefreshTokenId = reader.GetString(0),
            UserId = reader.GetString(1),
            TokenHash = reader.GetString(2),
            ExpiresAtUtc = DateTime.Parse(reader.GetString(3)).ToUniversalTime(),
            RevokedAtUtc = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)).ToUniversalTime(),
            ReplacedByTokenId = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAtUtc = DateTime.Parse(reader.GetString(6)).ToUniversalTime()
        };
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
