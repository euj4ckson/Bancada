using Bancada.Domain;

namespace Bancada.Domain.Tests;

public sealed class RecipeTests
{
    [Fact]
    public void Publish_requires_an_ingredient()
    {
        var recipe = NewRecipe();

        var action = () => recipe.Publish(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(action);
        Assert.False(recipe.IsPublished);
    }

    [Fact]
    public void Publish_marks_a_complete_recipe_as_published()
    {
        var recipe = NewRecipe();
        recipe.Ingredients.Add(new RecipeIngredient(recipe.Id, Guid.NewGuid(), "2", "xícaras", null, 0));

        recipe.Publish(DateTimeOffset.UtcNow);

        Assert.True(recipe.IsPublished);
    }

    [Theory]
    [InlineData("Limão", "limao")]
    [InlineData("  AÇÚCAR mascavo ", "acucar mascavo")]
    public void Ingredient_normalization_is_search_friendly(string name, string expected)
    {
        Assert.Equal(expected, Ingredient.Normalize(name));
    }

    private static Recipe NewRecipe() => new(
        Guid.NewGuid(),
        "Bolo de fubá",
        "Um bolo simples para o café da tarde.",
        "Misture os ingredientes e asse até dourar.",
        50,
        RecipeDifficulty.Easy,
        8,
        DateTimeOffset.UtcNow);
}
