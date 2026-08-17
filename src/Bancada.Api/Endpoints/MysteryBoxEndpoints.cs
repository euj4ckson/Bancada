using Bancada.Application;
using Bancada.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Endpoints;

public static class MysteryBoxEndpoints
{
    private static readonly string[] FallbackIngredients =
        ["Frango", "Limão", "Mel", "Cenoura", "Arroz", "Abóbora", "Queijo", "Tomate"];

    public static IEndpointRouteBuilder MapMysteryBoxEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/mystery-box", DrawAsync).WithTags("Mystery box");
        return endpoints;
    }

    private static async Task<IResult> DrawAsync(BancadaDbContext dbContext, CancellationToken cancellationToken)
    {
        var available = await dbContext.Ingredients.AsNoTracking()
            .Select(ingredient => ingredient.Name)
            .Take(100)
            .ToListAsync(cancellationToken);
        if (available.Count < 4)
        {
            available = FallbackIngredients.ToList();
        }

        var selected = available.OrderBy(_ => Random.Shared.Next()).Take(4).ToList();
        return Results.Ok(new MysteryBoxResponse(selected, "Monte um prato usando pelo menos 3 destes ingredientes."));
    }
}
