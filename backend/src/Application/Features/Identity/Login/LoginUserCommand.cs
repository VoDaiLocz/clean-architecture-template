using System;
using MediatR;
using Domain.Aggregates.Identity;

namespace Application.Features.Identity.Login;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(string AccessToken, string RefreshToken, DateTime ExpiresAt, AuthUser UserSummary);
