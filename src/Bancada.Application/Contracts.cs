using System.ComponentModel.DataAnnotations;
using Bancada.Domain;

namespace Bancada.Application;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(8), MaxLength(100)] string Password,
    [property: Required, MinLength(2), MaxLength(80)] string DisplayName);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    bool RememberMe = false);

public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, string? AvatarUrl);

public sealed record RecipeIngredientInput(
    [property: Required, MinLength(2), MaxLength(100)] string Name,
    [property: Required, MaxLength(40)] string Quantity,
    [property: MaxLength(30)] string? Unit,
    [property: MaxLength(120)] string? Notes,
    [property: Range(0, 49)] int SortOrder);

public sealed record SaveRecipeRequest(
    [property: Required, MinLength(3), MaxLength(140)] string Title,
    [property: Required, MinLength(10), MaxLength(600)] string Description,
    [property: Required, MinLength(10), MaxLength(8000)] string Instructions,
    [property: Range(1, 1440)] int PreparationTimeMinutes,
    RecipeDifficulty Difficulty,
    [property: Range(1, 100)] int Servings,
    bool IsPublished,
    [property: Required, MinLength(1), MaxLength(50)] IReadOnlyList<RecipeIngredientInput> Ingredients);

public sealed record RecipeIngredientResponse(Guid Id, string Name, string Quantity, string? Unit, string? Notes, int SortOrder);

public sealed record RecipeCardResponse(
    Guid Id,
    string Title,
    string Description,
    int PreparationTimeMinutes,
    RecipeDifficulty Difficulty,
    string? CoverImageUrl,
    DateTimeOffset CreatedAt,
    Guid AuthorId,
    string AuthorDisplayName,
    bool IsFavorite = false,
    bool IsPublished = true);

public sealed record RecipeDetailResponse(
    Guid Id,
    string Title,
    string Description,
    string Instructions,
    int PreparationTimeMinutes,
    RecipeDifficulty Difficulty,
    int Servings,
    string? CoverImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsPublished,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    bool IsFavorite,
    int FavoriteCount,
    int CommentCount);

public sealed record CommentRequest([property: Required, MinLength(2), MaxLength(1000)] string Content);
public sealed record CommentResponse(Guid Id, string Content, DateTimeOffset CreatedAt, Guid UserId, string UserDisplayName, string? UserAvatarUrl);

public sealed record ChallengeCardResponse(Guid Id, string Title, string Description, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, ChallengeStatus Status, string? CoverImageUrl, IReadOnlyList<string> RequiredIngredients, int SubmissionCount);

public sealed record ChallengeDetailResponse(Guid Id, string Title, string Description, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, ChallengeStatus Status, string? CoverImageUrl,
    IReadOnlyList<string> RequiredIngredients, IReadOnlyList<ChallengeSubmissionResponse> Submissions);

public sealed record ChallengeSubmissionRequest(Guid RecipeId, [property: MaxLength(500)] string? Description);
public sealed record ChallengeSubmissionResponse(Guid Id, Guid RecipeId, string RecipeTitle, string? RecipeImageUrl,
    Guid UserId, string UserDisplayName, string? Description, DateTimeOffset SubmittedAt);

public sealed record UpdateProfileRequest(
    [property: Required, MinLength(2), MaxLength(80)] string DisplayName,
    [property: MaxLength(500)] string? Bio);

public sealed record ProfileResponse(Guid Id, string DisplayName, string? Bio, string? AvatarUrl, DateTimeOffset CreatedAt,
    IReadOnlyList<RecipeCardResponse> Recipes, IReadOnlyList<ChallengeSubmissionResponse> ChallengeSubmissions, bool IsOwner);

public sealed record MysteryBoxResponse(IReadOnlyList<string> Ingredients, string Prompt);

public sealed record FileUpload(Stream Content, string ContentType, long Length);

public interface IFileStorage
{
    Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}
