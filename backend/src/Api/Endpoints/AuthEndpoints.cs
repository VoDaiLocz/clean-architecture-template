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

        group.MapPost("/refresh", async (ISender sender, [FromBody] Application.Features.Identity.Refresh.RefreshAuthCommand command) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapPost("/logout", async (ISender sender, [FromBody] Application.Features.Identity.Logout.LogoutCommand command) =>
        {
            await sender.Send(command);
            return Results.Ok();
        });

        group.MapGet("/me", async (ISender sender, System.Security.Claims.ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                         
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var query = new Application.Features.Identity.Me.GetCurrentUserQuery(userId);
            var result = await sender.Send(query);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
