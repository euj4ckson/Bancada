namespace Bancada.Domain;

public sealed class ChallengeSubmission
{
    private ChallengeSubmission()
    {
    }

    public ChallengeSubmission(Guid challengeId, Guid userId, Guid recipeId, string? description, DateTimeOffset submittedAt)
    {
        Id = Guid.NewGuid();
        ChallengeId = challengeId;
        UserId = userId;
        RecipeId = recipeId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SubmittedAt = submittedAt;
    }

    public Guid Id { get; private set; }
    public Guid ChallengeId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RecipeId { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public Challenge Challenge { get; private set; } = null!;
    public Recipe Recipe { get; private set; } = null!;
}
