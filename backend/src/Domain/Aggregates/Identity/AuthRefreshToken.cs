using System;

namespace Domain.Aggregates.Identity;

public sealed record AuthRefreshToken
{
    public required string RefreshTokenId { get; init; }
    public required string UserId { get; init; }
    public required string TokenHash { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
    public string? ReplacedByTokenId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
