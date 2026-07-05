using Application.Common.Interfaces.Security;
using Domain.Aggregates.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<AuthUser> _passwordHasher;
    private readonly AuthUser _dummyUser;

    public PasswordHasherService()
    {
        _passwordHasher = new PasswordHasher<AuthUser>();
        _dummyUser = new AuthUser
        {
            UserId = "dummy",
            EmailNormalized = "DUMMY@EXAMPLE.COM",
            PasswordHash = "",
            DisplayName = "Dummy",
            Role = "Learner",
            Status = "Active",
            CreatedAtUtc = System.DateTime.UtcNow,
            UpdatedAtUtc = System.DateTime.UtcNow
        };
    }

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(_dummyUser, password);
    }

    public bool Verify(string passwordHash, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(_dummyUser, passwordHash, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
