using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Security;
using Application.Features.Identity.Login;
using Domain.Aggregates.Identity;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Identity.Refresh;

public sealed class RefreshAuthHandler : IRequestHandler<RefreshAuthCommand, LoginResult>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly int _refreshTokenExpiryDays;

    public RefreshAuthHandler(
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

    public async Task<LoginResult> Handle(RefreshAuthCommand request, CancellationToken cancellationToken)
    {
        var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

        if (token == null || token.RevokedAtUtc != null || token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        if (token.ReplacedByTokenId != null)
        {
            // Revoke all active tokens for that user could be implemented here.
            // For now, throw an error.
            throw new UnauthorizedAccessException("Refresh token has been reused.");
        }

        var user = await _authRepository.GetUserByIdAsync(token.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        var newAuthRefreshToken = new AuthRefreshToken
        {
            RefreshTokenId = Guid.NewGuid().ToString(),
            UserId = user.UserId,
            TokenHash = _passwordHasher.Hash(newRefreshToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        var updatedOldToken = token with 
        { 
            RevokedAtUtc = DateTime.UtcNow,
            ReplacedByTokenId = newAuthRefreshToken.RefreshTokenId 
        };

        await _authRepository.UpdateRefreshTokenAsync(updatedOldToken);
        await _authRepository.AddRefreshTokenAsync(newAuthRefreshToken);

        return new LoginResult(accessToken, newRefreshToken, expiresAtUtc, user);
    }
}
