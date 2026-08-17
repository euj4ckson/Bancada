namespace Bancada.Domain;

public sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    public RecipeIngredient(Guid recipeId, Guid ingredientId, string quantity, string? unit, string? notes, int sortOrder)
    {
        RecipeId = recipeId;
        IngredientId = ingredientId;
        Quantity = quantity.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SortOrder = sortOrder;
    }

    public Guid RecipeId { get; private set; }
    public Guid IngredientId { get; private set; }
    public string Quantity { get; private set; } = string.Empty;
    public string? Unit { get; private set; }
    public string? Notes { get; private set; }
    public int SortOrder { get; private set; }
    public Recipe Recipe { get; private set; } = null!;
    public Ingredient Ingredient { get; private set; } = null!;
}
