using Domain.Aggregates.Identity;
using MediatR;

namespace Application.Features.Identity.Me;

public sealed record GetCurrentUserQuery(string UserId) : IRequest<AuthUser>;
