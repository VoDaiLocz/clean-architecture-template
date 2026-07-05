using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Security;
using Domain.Aggregates.Identity;
using MediatR;

namespace Application.Features.Identity.Register;

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, string>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IAuthRepository authRepository, IPasswordHasher passwordHasher)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.ToUpperInvariant();
        var existingUser = await _authRepository.GetUserByEmailAsync(emailNormalized);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new AuthUser
        {
            UserId = Guid.NewGuid().ToString(),
            EmailNormalized = emailNormalized,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName,
            Role = "Learner",
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _authRepository.CreateUserAsync(user);

        return user.UserId;
    }
}
