using Bancada.Application;
using Bancada.Domain;
using Bancada.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Endpoints;

public static class ChallengeEndpoints
{
    public static IEndpointRouteBuilder MapChallengeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/challenges").WithTags("Challenges");
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/submissions", SubmitAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListAsync(BancadaDbContext dbContext, ChallengeStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Challenges.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(challenge => challenge.Status == status);
        }

        var challenges = await query
            .OrderBy(challenge => challenge.Status == ChallengeStatus.Active ? 0 : 1)
            .ThenByDescending(challenge => challenge.StartsAt)
            .Select(challenge => new ChallengeCardResponse(challenge.Id, challenge.Title, challenge.Description,
                challenge.StartsAt, challenge.EndsAt, challenge.Status, challenge.CoverImageUrl,
                challenge.Ingredients.Where(item => item.IsRequired).Select(item => item.Ingredient.Name).ToList(),
                challenge.Submissions.Count))
            .ToListAsync(cancellationToken);
        return Results.Ok(challenges);
    }

    private static async Task<IResult> GetAsync(Guid id, BancadaDbContext dbContext, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.Challenges.AsNoTracking()
            .Include(item => item.Ingredients)
            .ThenInclude(item => item.Ingredient)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null)
        {
            return Results.NotFound();
        }

        var submissions = await (from submission in dbContext.ChallengeSubmissions.AsNoTracking()
                                 join recipe in dbContext.Recipes.AsNoTracking() on submission.RecipeId equals recipe.Id
                                 join user in dbContext.Users.AsNoTracking() on submission.UserId equals user.Id
                                 where submission.ChallengeId == id
                                 orderby submission.SubmittedAt descending
                                 select new ChallengeSubmissionResponse(submission.Id, recipe.Id, recipe.Title,
                                     recipe.CoverImageUrl, user.Id, user.DisplayName, submission.Description,
                                     submission.SubmittedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(new ChallengeDetailResponse(challenge.Id, challenge.Title, challenge.Description,
            challenge.StartsAt, challenge.EndsAt, challenge.Status, challenge.CoverImageUrl,
            challenge.Ingredients.Where(item => item.IsRequired).Select(item => item.Ingredient.Name).ToList(),
            submissions));
    }

    private static async Task<IResult> SubmitAsync(Guid id, ChallengeSubmissionRequest request,
        BancadaDbContext dbContext, HttpContext context, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var userId = context.User.GetUserId()!.Value;
        var challenge = await dbContext.Challenges.Include(item => item.Ingredients)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null)
        {
            return Results.NotFound();
        }

        if (!challenge.AcceptsSubmissions(timeProvider.GetUtcNow()))
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Este desafio não está recebendo participações.");
        }

        var recipe = await dbContext.Recipes.Include(item => item.Ingredients)
            .SingleOrDefaultAsync(item => item.Id == request.RecipeId, cancellationToken);
        if (recipe is null || !recipe.IsPublished)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Escolha uma receita publicada.");
        }

        if (recipe.AuthorId != userId)
        {
            return Results.Forbid();
        }

        var requiredIngredients = challenge.Ingredients.Where(item => item.IsRequired).Select(item => item.IngredientId).ToHashSet();
        var recipeIngredients = recipe.Ingredients.Select(item => item.IngredientId).ToHashSet();
        if (!requiredIngredients.IsSubsetOf(recipeIngredients))
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "A receita precisa conter todos os ingredientes obrigatórios do desafio.");
        }

        if (await dbContext.ChallengeSubmissions.AnyAsync(
                submission => submission.ChallengeId == id && submission.UserId == userId, cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Você já enviou uma receita para este desafio.");
        }

        var submission = new ChallengeSubmission(id, userId, recipe.Id, request.Description, timeProvider.GetUtcNow());
        dbContext.ChallengeSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/challenges/{id}", new { submission.Id });
    }
}
