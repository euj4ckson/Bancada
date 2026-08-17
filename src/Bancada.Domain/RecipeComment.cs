namespace Bancada.Domain;

public sealed class RecipeComment
{
    private RecipeComment()
    {
    }

    public RecipeComment(Guid recipeId, Guid userId, string content, DateTimeOffset createdAt)
    {
        var trimmed = content.Trim();
        if (trimmed.Length is < 2 or > 1000)
        {
            throw new ArgumentException("Comment must contain between 2 and 1000 characters.", nameof(content));
        }

        Id = Guid.NewGuid();
        RecipeId = recipeId;
        UserId = userId;
        Content = trimmed;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Recipe Recipe { get; private set; } = null!;
}
