using System.Globalization;
using System.Text;

namespace Bancada.Domain;

public sealed class Ingredient
{
    private Ingredient()
    {
    }

    public Ingredient(string name)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        NormalizedName = Normalize(name);

        if (Name.Length is < 2 or > 100)
        {
            throw new ArgumentException("Ingredient name must contain between 2 and 100 characters.", nameof(name));
        }
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;

    public static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
