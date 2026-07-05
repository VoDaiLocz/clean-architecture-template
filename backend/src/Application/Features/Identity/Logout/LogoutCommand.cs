using MediatR;

namespace Application.Features.Identity.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
