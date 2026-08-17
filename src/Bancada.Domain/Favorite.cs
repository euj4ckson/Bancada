namespace Bancada.Domain;

public sealed class Favorite
{
    private Favorite()
    {
    }

    public Favorite(Guid userId, Guid recipeId, DateTimeOffset createdAt)
    {
        UserId = userId;
        RecipeId = recipeId;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; private set; }
    public Guid RecipeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Recipe Recipe { get; private set; } = null!;
}
