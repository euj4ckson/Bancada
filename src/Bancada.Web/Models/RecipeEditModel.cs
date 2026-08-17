using System.ComponentModel.DataAnnotations;
using Bancada.Domain;

namespace Bancada.Web.Models;

public sealed class RecipeEditModel
{
    [Required(ErrorMessage = "Dê um nome para a receita."), MinLength(3, ErrorMessage = "Use pelo menos 3 caracteres."), MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Conte um pouco sobre o prato."), MinLength(10, ErrorMessage = "Escreva pelo menos 10 caracteres."), MaxLength(600)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Explique o modo de preparo."), MinLength(10, ErrorMessage = "Escreva pelo menos 10 caracteres."), MaxLength(8000)]
    public string Instructions { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Informe um tempo entre 1 minuto e 24 horas.")]
    public int PreparationTimeMinutes { get; set; } = 30;

    public RecipeDifficulty Difficulty { get; set; } = RecipeDifficulty.Easy;

    [Range(1, 100, ErrorMessage = "Informe um rendimento entre 1 e 100 porções.")]
    public int Servings { get; set; } = 2;

    public bool IsPublished { get; set; }
    public List<IngredientEditModel> Ingredients { get; set; } = [new()];
}

public sealed class IngredientEditModel
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Notes { get; set; }
}
