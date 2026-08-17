using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bancada.Application;

namespace Bancada.Api;

internal static class EndpointSupport
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Dictionary<string, string[]> Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, true);

        if (request is SaveRecipeRequest recipe)
        {
            foreach (var (ingredient, index) in recipe.Ingredients.Select((item, index) => (item, index)))
            {
                var ingredientResults = new List<ValidationResult>();
                Validator.TryValidateObject(ingredient, new ValidationContext(ingredient), ingredientResults, true);
                results.AddRange(ingredientResults.Select(result => new ValidationResult(
                    result.ErrorMessage,
                    result.MemberNames.Select(member => $"Ingredients[{index}].{member}"))));
            }

            var duplicates = recipe.Ingredients
                .GroupBy(item => Bancada.Domain.Ingredient.Normalize(item.Name))
                .Any(group => group.Count() > 1);
            if (duplicates)
            {
                results.Add(new ValidationResult("Não repita o mesmo ingrediente.", [nameof(recipe.Ingredients)]));
            }
        }

        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Message: result.ErrorMessage ?? "Valor inválido.")))
            .GroupBy(item => item.Member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Message).Distinct().ToArray());
    }

    public static IResult ValidationProblemIfInvalid(object request, out bool invalid)
    {
        var errors = Validate(request);
        invalid = errors.Count > 0;
        return Results.ValidationProblem(errors);
    }
}
