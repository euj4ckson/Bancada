namespace Bancada.Domain;

public sealed class Recipe
{
    private Recipe()
    {
    }

    public Recipe(Guid authorId, string title, string description, string instructions,
        int preparationTimeMinutes, RecipeDifficulty difficulty, int servings, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        AuthorId = authorId;
        CreatedAt = now;
        Update(title, description, instructions, preparationTimeMinutes, difficulty, servings, now);
    }

    public Guid Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public int PreparationTimeMinutes { get; private set; }
    public RecipeDifficulty Difficulty { get; private set; }
    public int Servings { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public ICollection<RecipeIngredient> Ingredients { get; private set; } = [];
    public ICollection<Favorite> Favorites { get; private set; } = [];
    public ICollection<RecipeComment> Comments { get; private set; } = [];

    public void Update(string title, string description, string instructions,
        int preparationTimeMinutes, RecipeDifficulty difficulty, int servings, DateTimeOffset now)
    {
        Title = Required(title, 140, nameof(title));
        Description = Required(description, 600, nameof(description));
        Instructions = Required(instructions, 8000, nameof(instructions));

        if (preparationTimeMinutes is < 1 or > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(preparationTimeMinutes));
        }

        if (servings is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(servings));
        }

        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        PreparationTimeMinutes = preparationTimeMinutes;
        Difficulty = difficulty;
        Servings = servings;
        UpdatedAt = now;
    }

    public void SetCoverImage(string url, DateTimeOffset now)
    {
        CoverImageUrl = Required(url, 2000, nameof(url));
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Ingredients.Count == 0)
        {
            throw new InvalidOperationException("A recipe needs at least one ingredient before publishing.");
        }

        IsPublished = true;
        UpdatedAt = now;
    }

    public void Unpublish(DateTimeOffset now)
    {
        IsPublished = false;
        UpdatedAt = now;
    }

    private static string Required(string value, int maxLength, string parameterName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} must contain between 1 and {maxLength} characters.", parameterName);
        }

        return trimmed;
    }
}
