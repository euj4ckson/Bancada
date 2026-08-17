using Bancada.Application;
using Bancada.Domain;
using Bancada.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Endpoints;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var recipes = endpoints.MapGroup("/api/recipes").WithTags("Recipes");

        recipes.MapGet("/", ListAsync);
        recipes.MapGet("/{id:guid}", GetAsync);
        recipes.MapPost("/", CreateAsync).RequireAuthorization();
        recipes.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization();
        recipes.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization();
        recipes.MapPost("/{id:guid}/image", UploadImageAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .RequireAuthorization();

        recipes.MapGet("/{id:guid}/comments", ListCommentsAsync);
        recipes.MapPost("/{id:guid}/comments", AddCommentAsync).RequireAuthorization();
        recipes.MapPost("/{id:guid}/favorite", AddFavoriteAsync).RequireAuthorization();
        recipes.MapDelete("/{id:guid}/favorite", RemoveFavoriteAsync).RequireAuthorization();

        endpoints.MapGet("/api/favorites", ListFavoritesAsync).WithTags("Favorites").RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListAsync(BancadaDbContext dbContext, HttpContext context,
        int page = 1, int pageSize = 12, string? search = null, RecipeDifficulty? difficulty = null,
        int? maxTime = null, string? ingredient = null, CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 24);
        var userId = context.User.GetUserId();

        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(recipe => recipe.Title.ToLower().Contains(term) || recipe.Description.ToLower().Contains(term));
        }

        if (difficulty is not null)
        {
            query = query.Where(recipe => recipe.Difficulty == difficulty);
        }

        if (maxTime is > 0)
        {
            query = query.Where(recipe => recipe.PreparationTimeMinutes <= maxTime);
        }

        if (!string.IsNullOrWhiteSpace(ingredient))
        {
            var normalized = Ingredient.Normalize(ingredient);
            query = query.Where(recipe => recipe.Ingredients.Any(item => item.Ingredient.NormalizedName.Contains(normalized)));
        }

        var total = await query.CountAsync(cancellationToken);
        var pageQuery = query.OrderByDescending(recipe => recipe.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var items = await ProjectCards(pageQuery, dbContext, userId)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<RecipeCardResponse>(items, page, pageSize, total));
    }

    private static async Task<IResult> GetAsync(Guid id, BancadaDbContext dbContext, HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.User.GetUserId();
        var recipe = await dbContext.Recipes.AsNoTracking()
            .Include(item => item.Ingredients)
            .ThenInclude(item => item.Ingredient)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (recipe is null || (!recipe.IsPublished && recipe.AuthorId != userId))
        {
            return Results.NotFound();
        }

        var author = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == recipe.AuthorId, cancellationToken);
        var isFavorite = userId.HasValue && await dbContext.Favorites.AnyAsync(
            favorite => favorite.UserId == userId && favorite.RecipeId == id, cancellationToken);
        var favoriteCount = await dbContext.Favorites.CountAsync(favorite => favorite.RecipeId == id, cancellationToken);
        var commentCount = await dbContext.RecipeComments.CountAsync(comment => comment.RecipeId == id, cancellationToken);

        return Results.Ok(ToDetail(recipe, author, isFavorite, favoriteCount, commentCount));
    }

    private static async Task<IResult> CreateAsync(SaveRecipeRequest request, BancadaDbContext dbContext,
        HttpContext context, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var userId = context.User.GetUserId()!.Value;
        var now = timeProvider.GetUtcNow();
        var recipe = new Recipe(userId, request.Title, request.Description, request.Instructions,
            request.PreparationTimeMinutes, request.Difficulty, request.Servings, now);

        await ReplaceIngredientsAsync(recipe, request.Ingredients, dbContext, cancellationToken);
        if (request.IsPublished)
        {
            recipe.Publish(now);
        }

        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/recipes/{recipe.Id}", new { recipe.Id });
    }

    private static async Task<IResult> UpdateAsync(Guid id, SaveRecipeRequest request, BancadaDbContext dbContext,
        HttpContext context, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        var userId = context.User.GetUserId()!.Value;
        var recipe = await dbContext.Recipes.Include(item => item.Ingredients)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (recipe.AuthorId != userId)
        {
            return Results.Forbid();
        }

        var now = timeProvider.GetUtcNow();
        recipe.Update(request.Title, request.Description, request.Instructions,
            request.PreparationTimeMinutes, request.Difficulty, request.Servings, now);
        await ReplaceIngredientsAsync(recipe, request.Ingredients, dbContext, cancellationToken);

        if (request.IsPublished)
        {
            recipe.Publish(now);
        }
        else
        {
            recipe.Unpublish(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(Guid id, BancadaDbContext dbContext, HttpContext context,
        IFileStorage fileStorage, CancellationToken cancellationToken)
    {
        var userId = context.User.GetUserId()!.Value;
        var recipe = await dbContext.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (recipe.AuthorId != userId)
        {
            return Results.Forbid();
        }

        if (await dbContext.ChallengeSubmissions.AnyAsync(submission => submission.RecipeId == id, cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Esta receita participa de um desafio e não pode ser excluída.");
        }

        var imageUrl = recipe.CoverImageUrl;
        dbContext.Recipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (imageUrl is not null)
        {
            await fileStorage.DeleteAsync(imageUrl, cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> UploadImageAsync(Guid id, IFormFile file, BancadaDbContext dbContext,
        HttpContext context, IFileStorage fileStorage, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (recipe.AuthorId != context.User.GetUserId())
        {
            return Results.Forbid();
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var newUrl = await fileStorage.SaveAsync(new FileUpload(stream, file.ContentType, file.Length), "recipes", cancellationToken);
            var oldUrl = recipe.CoverImageUrl;
            recipe.SetCoverImage(newUrl, timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);

            if (oldUrl is not null)
            {
                await fileStorage.DeleteAsync(oldUrl, cancellationToken);
            }

            return Results.Ok(new { Url = newUrl });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Message);
        }
    }

    private static async Task<IResult> ListCommentsAsync(Guid id, BancadaDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Recipes.AnyAsync(recipe => recipe.Id == id && recipe.IsPublished, cancellationToken))
        {
            return Results.NotFound();
        }

        var comments = await (from comment in dbContext.RecipeComments.AsNoTracking()
                              join user in dbContext.Users.AsNoTracking() on comment.UserId equals user.Id
                              where comment.RecipeId == id
                              orderby comment.CreatedAt
                              select new CommentResponse(comment.Id, comment.Content, comment.CreatedAt,
                                  user.Id, user.DisplayName, user.AvatarUrl))
            .ToListAsync(cancellationToken);
        return Results.Ok(comments);
    }

    private static async Task<IResult> AddCommentAsync(Guid id, CommentRequest request, BancadaDbContext dbContext,
        HttpContext context, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var validation = EndpointSupport.ValidationProblemIfInvalid(request, out var invalid);
        if (invalid)
        {
            return validation;
        }

        if (!await dbContext.Recipes.AnyAsync(recipe => recipe.Id == id && recipe.IsPublished, cancellationToken))
        {
            return Results.NotFound();
        }

        var comment = new RecipeComment(id, context.User.GetUserId()!.Value, request.Content, timeProvider.GetUtcNow());
        dbContext.RecipeComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/recipes/{id}/comments", new { comment.Id });
    }

    private static async Task<IResult> AddFavoriteAsync(Guid id, BancadaDbContext dbContext, HttpContext context,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (!await dbContext.Recipes.AnyAsync(recipe => recipe.Id == id && recipe.IsPublished, cancellationToken))
        {
            return Results.NotFound();
        }

        var userId = context.User.GetUserId()!.Value;
        if (!await dbContext.Favorites.AnyAsync(item => item.UserId == userId && item.RecipeId == id, cancellationToken))
        {
            dbContext.Favorites.Add(new Favorite(userId, id, timeProvider.GetUtcNow()));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavoriteAsync(Guid id, BancadaDbContext dbContext, HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.User.GetUserId()!.Value;
        var favorite = await dbContext.Favorites.SingleOrDefaultAsync(
            item => item.UserId == userId && item.RecipeId == id, cancellationToken);
        if (favorite is not null)
        {
            dbContext.Favorites.Remove(favorite);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ListFavoritesAsync(BancadaDbContext dbContext, HttpContext context,
        int page = 1, int pageSize = 12, CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 24);
        var userId = context.User.GetUserId()!.Value;
        var query = dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.IsPublished && recipe.Favorites.Any(favorite => favorite.UserId == userId));
        var total = await query.CountAsync(cancellationToken);
        var pageQuery = query.OrderByDescending(recipe => recipe.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var items = await ProjectCards(pageQuery, dbContext, userId)
            .ToListAsync(cancellationToken);
        return Results.Ok(new PagedResult<RecipeCardResponse>(items, page, pageSize, total));
    }

    private static IQueryable<RecipeCardResponse> ProjectCards(IQueryable<Recipe> recipes, BancadaDbContext dbContext, Guid? userId) =>
        from recipe in recipes
        join author in dbContext.Users.AsNoTracking() on recipe.AuthorId equals author.Id
        select new RecipeCardResponse(recipe.Id, recipe.Title, recipe.Description, recipe.PreparationTimeMinutes,
            recipe.Difficulty, recipe.CoverImageUrl, recipe.CreatedAt, author.Id, author.DisplayName,
            userId.HasValue && recipe.Favorites.Any(favorite => favorite.UserId == userId));

    private static RecipeDetailResponse ToDetail(Recipe recipe, ApplicationUser author, bool isFavorite, int favoriteCount, int commentCount) =>
        new(recipe.Id, recipe.Title, recipe.Description, recipe.Instructions, recipe.PreparationTimeMinutes,
            recipe.Difficulty, recipe.Servings, recipe.CoverImageUrl, recipe.CreatedAt, recipe.UpdatedAt,
            recipe.IsPublished, author.Id, author.DisplayName, author.AvatarUrl,
            recipe.Ingredients.OrderBy(item => item.SortOrder)
                .Select(item => new RecipeIngredientResponse(item.IngredientId, item.Ingredient.Name,
                    item.Quantity, item.Unit, item.Notes, item.SortOrder)).ToList(),
            isFavorite, favoriteCount, commentCount);

    private static async Task ReplaceIngredientsAsync(Recipe recipe, IReadOnlyList<RecipeIngredientInput> inputs,
        BancadaDbContext dbContext, CancellationToken cancellationToken)
    {
        if (recipe.Ingredients.Count > 0)
        {
            dbContext.RecipeIngredients.RemoveRange(recipe.Ingredients);
            recipe.Ingredients.Clear();
        }

        var normalizedNames = inputs.Select(input => Ingredient.Normalize(input.Name)).ToArray();
        var existing = await dbContext.Ingredients
            .Where(ingredient => normalizedNames.Contains(ingredient.NormalizedName))
            .ToDictionaryAsync(ingredient => ingredient.NormalizedName, cancellationToken);

        foreach (var input in inputs.OrderBy(input => input.SortOrder))
        {
            var normalized = Ingredient.Normalize(input.Name);
            if (!existing.TryGetValue(normalized, out var ingredient))
            {
                ingredient = new Ingredient(input.Name);
                existing.Add(normalized, ingredient);
                dbContext.Ingredients.Add(ingredient);
            }

            recipe.Ingredients.Add(new RecipeIngredient(recipe.Id, ingredient.Id, input.Quantity,
                input.Unit, input.Notes, input.SortOrder));
        }
    }
}
