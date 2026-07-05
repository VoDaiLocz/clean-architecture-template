using MediatR;

namespace Application.Features.Identity.Register;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName) : IRequest<string>;
