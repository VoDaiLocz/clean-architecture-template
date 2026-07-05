using System;
using Domain.Aggregates.Identity;

namespace Application.Common.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(AuthUser user);
    string GenerateRefreshToken();
}
