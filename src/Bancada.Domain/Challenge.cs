namespace Bancada.Domain;

public sealed class Challenge
{
    private Challenge()
    {
    }

    public Challenge(string title, string description, DateTimeOffset startsAt, DateTimeOffset endsAt,
        ChallengeStatus status, string? coverImageUrl = null)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Challenge end must be after its start.", nameof(endsAt));
        }

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
        Status = status;
        CoverImageUrl = coverImageUrl;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public ChallengeStatus Status { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public ICollection<ChallengeIngredient> Ingredients { get; private set; } = [];
    public ICollection<ChallengeSubmission> Submissions { get; private set; } = [];

    public bool AcceptsSubmissions(DateTimeOffset now) =>
        Status == ChallengeStatus.Active && now >= StartsAt && now <= EndsAt;
}
