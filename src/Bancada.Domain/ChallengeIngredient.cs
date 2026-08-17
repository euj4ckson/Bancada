namespace Bancada.Domain;

public sealed class ChallengeIngredient
{
    private ChallengeIngredient()
    {
    }

    public ChallengeIngredient(Guid challengeId, Guid ingredientId, bool isRequired)
    {
        ChallengeId = challengeId;
        IngredientId = ingredientId;
        IsRequired = isRequired;
    }

    public Guid ChallengeId { get; private set; }
    public Guid IngredientId { get; private set; }
    public bool IsRequired { get; private set; }
    public Challenge Challenge { get; private set; } = null!;
    public Ingredient Ingredient { get; private set; } = null!;
}
