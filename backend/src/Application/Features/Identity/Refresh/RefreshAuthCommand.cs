using Application.Features.Identity.Login;
using MediatR;

namespace Application.Features.Identity.Refresh;

public sealed record RefreshAuthCommand(string RefreshToken) : IRequest<LoginResult>;
