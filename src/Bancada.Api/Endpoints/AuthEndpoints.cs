using Bancada.Application;
using Bancada.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Bancada.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", CurrentUserAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? nameof(request.Password) : nameof(request.Email))
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
            return Results.ValidationProblem(errors);
        }

        await signInManager.SignInAsync(user, false);
        return Results.Ok(ToResponse(user));
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos.");
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, true);
        return result.Succeeded
            ? Results.Ok(ToResponse(user))
            : Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos.");
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> CurrentUserAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(context.User);
        return user is null ? Results.Unauthorized() : Results.Ok(ToResponse(user));
    }

    private static CurrentUserResponse ToResponse(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.AvatarUrl);
}
