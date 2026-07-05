using System;

namespace Domain.Aggregates.Identity;

public sealed record AuthUser
{
    public required string UserId { get; init; }
    public required string EmailNormalized { get; init; }
    public required string PasswordHash { get; init; }
    public required string DisplayName { get; init; }
    public required string Role { get; init; } // "Learner", "Operator", "Admin", "SuperAdmin"
    public required string Status { get; init; } // "Active", "Locked", "Disabled"
    public int FailedLoginAttempts { get; init; } = 0;
    public DateTime? LockedUntilUtc { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}
