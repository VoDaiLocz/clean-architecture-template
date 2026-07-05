using System.Threading.Tasks;
using Domain.Aggregates.Identity;

namespace Application.Common.Interfaces.Repositories;

public interface IAuthRepository
{
    void Initialize();
    Task<AuthUser?> GetUserByIdAsync(string userId);
    Task<AuthUser?> GetUserByEmailAsync(string emailNormalized);
    Task CreateUserAsync(AuthUser user);
    Task UpdateUserAsync(AuthUser user);

    Task<AuthRefreshToken?> GetRefreshTokenAsync(string tokenId);
    Task AddRefreshTokenAsync(AuthRefreshToken token);
    Task UpdateRefreshTokenAsync(AuthRefreshToken token);
}
