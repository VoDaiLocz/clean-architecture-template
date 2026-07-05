using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Security;
using Domain.Aggregates.Identity;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Identity.Login;

public sealed class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginResult>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly int _refreshTokenExpiryDays;

    public LoginUserHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenExpiryDays = configuration.GetValue<int?>("JwtSettings:RefreshTokenExpiryDays") ?? 7;
    }

    public async Task<LoginResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.ToUpperInvariant();
        var user = await _authRepository.GetUserByEmailAsync(emailNormalized);

        if (user == null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        var authRefreshToken = new AuthRefreshToken
        {
            RefreshTokenId = Guid.NewGuid().ToString(),
            UserId = user.UserId,
            TokenHash = _passwordHasher.Hash(refreshToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _authRepository.AddRefreshTokenAsync(authRefreshToken);

        return new LoginResult(accessToken, refreshToken, expiresAtUtc, user);
    }
}
