using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bancada.Application;
using Bancada.Domain;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Bancada.Web.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<CurrentUserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserResponse>(HttpMethod.Post, "api/auth/register", request, cancellationToken);

    public Task<CurrentUserResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserResponse>(HttpMethod.Post, "api/auth/login", request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", null, cancellationToken);

    public Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserResponse>(HttpMethod.Get, "api/auth/me", null, cancellationToken);

    public Task<PagedResult<RecipeCardResponse>> GetRecipesAsync(int page = 1, int pageSize = 12,
        string? search = null, RecipeDifficulty? difficulty = null, int? maxTime = null,
        string? ingredient = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (difficulty is not null) query.Add($"difficulty={(int)difficulty}");
        if (maxTime is not null) query.Add($"maxTime={maxTime}");
        if (!string.IsNullOrWhiteSpace(ingredient)) query.Add($"ingredient={Uri.EscapeDataString(ingredient)}");
        return SendAsync<PagedResult<RecipeCardResponse>>(HttpMethod.Get, $"api/recipes?{string.Join('&', query)}", null, cancellationToken);
    }

    public Task<RecipeDetailResponse> GetRecipeAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<RecipeDetailResponse>(HttpMethod.Get, $"api/recipes/{id}", null, cancellationToken);

    public async Task<Guid> CreateRecipeAsync(SaveRecipeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IdResponse>(HttpMethod.Post, "api/recipes", request, cancellationToken);
        return response.Id;
    }

    public Task UpdateRecipeAsync(Guid id, SaveRecipeRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/recipes/{id}", request, cancellationToken);

    public Task DeleteRecipeAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/recipes/{id}", null, cancellationToken);

    public Task SetFavoriteAsync(Guid id, bool favorite, CancellationToken cancellationToken = default) =>
        SendAsync(favorite ? HttpMethod.Post : HttpMethod.Delete, $"api/recipes/{id}/favorite", null, cancellationToken);

    public Task<PagedResult<RecipeCardResponse>> GetFavoritesAsync(int page = 1, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<RecipeCardResponse>>(HttpMethod.Get, $"api/favorites?page={page}", null, cancellationToken);

    public Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(Guid recipeId, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CommentResponse>>(HttpMethod.Get, $"api/recipes/{recipeId}/comments", null, cancellationToken);

    public Task AddCommentAsync(Guid recipeId, CommentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/recipes/{recipeId}/comments", request, cancellationToken);

    public Task<IReadOnlyList<ChallengeCardResponse>> GetChallengesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ChallengeCardResponse>>(HttpMethod.Get, "api/challenges", null, cancellationToken);

    public Task<ChallengeDetailResponse> GetChallengeAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<ChallengeDetailResponse>(HttpMethod.Get, $"api/challenges/{id}", null, cancellationToken);

    public Task SubmitChallengeAsync(Guid id, ChallengeSubmissionRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/challenges/{id}/submissions", request, cancellationToken);

    public Task<ProfileResponse> GetProfileAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<ProfileResponse>(HttpMethod.Get, $"api/profiles/{id}", null, cancellationToken);

    public Task<ProfileResponse> GetCurrentProfileAsync(CancellationToken cancellationToken = default) =>
        SendAsync<ProfileResponse>(HttpMethod.Get, "api/profile", null, cancellationToken);

    public Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, "api/profile", request, cancellationToken);

    public Task<MysteryBoxResponse> GetMysteryBoxAsync(CancellationToken cancellationToken = default) =>
        SendAsync<MysteryBoxResponse>(HttpMethod.Get, "api/mystery-box", null, cancellationToken);

    public async Task<string> UploadRecipeImageAsync(Guid id, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(5 * 1024 * 1024, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", "upload");
        var result = await SendAsync<ImageResponse>(HttpMethod.Post, $"api/recipes/{id}/image", content, cancellationToken);
        return result.Url;
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, path, body);
        using var response = await SendHttpAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(method, path, body);
        using var response = await SendHttpAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ApiException(response.StatusCode, "A resposta do servidor veio vazia.");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = body switch
        {
            null => null,
            HttpContent content => content,
            _ => JsonContent.Create(body, body.GetType(), options: JsonOptions)
        };
        return request;
    }

    private async Task<HttpResponseMessage> SendHttpAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable,
                "Não foi possível falar com a cozinha agora. Verifique se a API está em execução.")
            { Source = exception.Source };
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Entre na sua conta para continuar.",
            HttpStatusCode.Forbidden => "Você não tem permissão para fazer isso.",
            HttpStatusCode.NotFound => "Não encontramos este conteúdo.",
            _ => "Não foi possível concluir a operação."
        };

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, cancellationToken);
            message = problem?.Errors?.SelectMany(item => item.Value).FirstOrDefault()
                ?? problem?.Detail
                ?? problem?.Title
                ?? message;
        }
        catch (JsonException)
        {
        }

        throw new ApiException(response.StatusCode, message);
    }

    private sealed record IdResponse(Guid Id);
    private sealed record ImageResponse(string Url);
    private sealed record ApiProblem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
