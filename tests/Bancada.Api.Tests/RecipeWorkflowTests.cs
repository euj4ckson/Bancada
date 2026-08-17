using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bancada.Application;
using Bancada.Domain;
using Bancada.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bancada.Api.Tests;

public sealed class RecipeWorkflowTests : IDisposable
{
    private readonly BancadaWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public RecipeWorkflowTests()
    {
        _client = _factory.CreateIsolatedClient();
    }

    [Fact]
    public async Task Authenticated_user_can_create_and_edit_a_recipe()
    {
        await RegisterAsync(_client, "ana");
        var recipeId = await CreateRecipeAsync(_client, "Torta de palmito", "Palmito");
        var update = RecipeRequest("Torta cremosa de palmito", "Palmito");

        var response = await _client.PutAsJsonAsync($"/api/recipes/{recipeId}", update);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var recipe = await _client.GetFromJsonAsync<RecipeDetailResponse>($"/api/recipes/{recipeId}");
        Assert.Equal("Torta cremosa de palmito", recipe!.Title);
    }

    [Fact]
    public async Task Another_user_cannot_edit_the_recipe()
    {
        await RegisterAsync(_client, "bia");
        var recipeId = await CreateRecipeAsync(_client, "Arroz com pequi", "Pequi");
        using var otherClient = _factory.CreateIsolatedClient();
        await RegisterAsync(otherClient, "carol");

        var response = await otherClient.PutAsJsonAsync($"/api/recipes/{recipeId}", RecipeRequest("Receita alterada", "Pequi"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Favorite_can_be_added_listed_and_removed()
    {
        await RegisterAsync(_client, "daniel");
        var recipeId = await CreateRecipeAsync(_client, "Abóbora assada", "Abóbora");

        var addResponse = await _client.PostAsync($"/api/recipes/{recipeId}/favorite", null);
        var favoritesResponse = await _client.GetAsync("/api/favorites");
        var error = await favoritesResponse.Content.ReadAsStringAsync();
        Assert.True(favoritesResponse.IsSuccessStatusCode, error);
        var favorites = await favoritesResponse.Content.ReadFromJsonAsync<PagedResult<RecipeCardResponse>>();
        var removeResponse = await _client.DeleteAsync($"/api/recipes/{recipeId}/favorite");

        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);
        Assert.Contains(favorites!.Items, recipe => recipe.Id == recipeId && recipe.IsFavorite);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_recipe_inputs_return_a_validation_problem()
    {
        await RegisterAsync(_client, "fabio");
        var request = RecipeRequest("Cuscuz com legumes", "Cenoura") with
        {
            Difficulty = (RecipeDifficulty)99,
            Ingredients =
            [
                new RecipeIngredientInput("Cenoura", "1", "unidade", null, 0),
                new RecipeIngredientInput("cenoura", "2", "unidades", null, 0)
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/recipes", request);
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Difficulty", problem);
        Assert.Contains("Ingredients", problem);
    }

    [Fact]
    public async Task Active_challenge_accepts_one_eligible_recipe_per_user()
    {
        await RegisterAsync(_client, "elisa");
        var recipeId = await CreateRecipeAsync(_client, "Pudim de pão", "Pão amanhecido");
        Guid challengeId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BancadaDbContext>();
            var ingredient = dbContext.Ingredients.Single(item => item.NormalizedName == Ingredient.Normalize("Pão amanhecido"));
            var now = DateTimeOffset.UtcNow;
            var challenge = new Challenge("Até a última migalha", "Use pão amanhecido.", now.AddDays(-1), now.AddDays(2), ChallengeStatus.Active);
            challenge.Ingredients.Add(new ChallengeIngredient(challenge.Id, ingredient.Id, true));
            dbContext.Challenges.Add(challenge);
            await dbContext.SaveChangesAsync();
            challengeId = challenge.Id;
        }

        var request = new ChallengeSubmissionRequest(recipeId, "Uma sobremesa para aproveitar todo o pão.");
        var first = await _client.PostAsJsonAsync($"/api/challenges/{challengeId}/submissions", request);
        var duplicate = await _client.PostAsJsonAsync($"/api/challenges/{challengeId}/submissions", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task RegisterAsync(HttpClient client, string name)
    {
        var email = $"{name}-{Guid.NewGuid():N}@bancada.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Bancada123!", name));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, string title, string ingredient)
    {
        var response = await client.PostAsJsonAsync("/api/recipes", RecipeRequest(title, ingredient));
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private static SaveRecipeRequest RecipeRequest(string title, string ingredient) => new(
        title,
        "Uma receita caseira com sabor equilibrado e preparo direto.",
        "Prepare os ingredientes, cozinhe com atenção e sirva ainda quente.",
        45,
        RecipeDifficulty.Medium,
        4,
        true,
        [new RecipeIngredientInput(ingredient, "2", "xícaras", null, 0)]);
}
