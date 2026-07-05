using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using MediatR;

namespace Application.Features.Identity.Logout;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthRepository _authRepository;

    public LogoutHandler(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

        if (token != null && token.RevokedAtUtc == null)
        {
            var updatedToken = token with 
            { 
                RevokedAtUtc = DateTime.UtcNow 
            };
            await _authRepository.UpdateRefreshTokenAsync(updatedToken);
        }
    }
}
