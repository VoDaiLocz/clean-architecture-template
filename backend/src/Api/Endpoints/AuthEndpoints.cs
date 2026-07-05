using Application.Features.Identity.Login;
using Application.Features.Identity.Register;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (ISender sender, [FromBody] RegisterUserCommand command) =>
        {
            var userId = await sender.Send(command);
            return Results.Ok(new { UserId = userId });
        });

        group.MapPost("/login", async (ISender sender, [FromBody] LoginUserCommand command) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });
    }
}
