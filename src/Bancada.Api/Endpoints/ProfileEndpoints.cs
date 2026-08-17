using Bancada.Application;
using Bancada.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/profiles/{id:guid}", GetAsync).WithTags("Profiles");
        endpoints.MapGet("/api/profile", GetCurrentAsync).WithTags("Profiles").RequireAuthorization();
        endpoints.MapPut("/api/profile", UpdateAsync).WithTags("Profiles").RequireAuthorization();
        return endpoints;
    }

    private static Task<IResult> GetCurrentAsync(BancadaDbContext dbContext, HttpContext context,
        CancellationToken cancellationToken) => GetAsync(context.User.GetUserId()!.Value, dbContext, context, cancellationToken);

    private static async Task<IResult> GetAsync(Guid id, BancadaDbContext dbContext, HttpContext context,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var currentUserId = context.User.GetUserId();
        var isOwner = currentUserId == id;
        var recipeQuery = dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.AuthorId == id && (recipe.IsPublished || isOwner));
        var recipes = await recipeQuery.OrderByDescending(recipe => recipe.CreatedAt)
            .Select(recipe => new RecipeCardResponse(recipe.Id, recipe.Title, recipe.Description,
                recipe.PreparationTimeMinutes, recipe.Difficulty, recipe.CoverImageUrl, recipe.CreatedAt,
                user.Id, user.DisplayName,
                currentUserId.HasValue && recipe.Favorites.Any(favorite => favorite.UserId == currentUserId)))
            .ToListAsync(cancellationToken);

        var submissions = await (from submission in dbContext.ChallengeSubmissions.AsNoTracking()
                                 join recipe in dbContext.Recipes.AsNoTracking() on submission.RecipeId equals recipe.Id
                                 where submission.UserId == id
                                 orderby submission.SubmittedAt descending
                                 select new ChallengeSubmissionResponse(submission.Id, recipe.Id, recipe.Title,
                                     recipe.CoverImageUrl, user.Id, user.DisplayName, submission.Description,
                                     submission.SubmittedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(new ProfileResponse(user.Id, user.DisplayName, user.Bio, user.AvatarUrl,
            user.CreatedAt, recipes, submissions, isOwner));
    }

    private static async Task<IResult> UpdateAsync(UpdateProfileRequest request, BancadaDbContext dbContext,
        HttpContext context, CancellationToken cancellationToken)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var userId = context.User.GetUserId()!.Value;
        var user = await dbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
