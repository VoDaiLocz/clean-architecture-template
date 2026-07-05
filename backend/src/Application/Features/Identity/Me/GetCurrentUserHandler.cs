using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Identity;
using MediatR;

namespace Application.Features.Identity.Me;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, AuthUser>
{
    private readonly IAuthRepository _authRepository;

    public GetCurrentUserHandler(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<AuthUser> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _authRepository.GetUserByIdAsync(request.UserId);
        
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return user;
    }
}
