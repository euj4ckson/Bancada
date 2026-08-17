using System.Security.Claims;
using Bancada.Application;
using Microsoft.AspNetCore.Components.Authorization;

namespace Bancada.Web.Services;

public sealed class BancadaAuthStateProvider(ApiClient apiClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private CurrentUserResponse? _currentUser;
    private bool _loaded;

    public CurrentUserResponse? CurrentUser => _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_loaded)
        {
            try
            {
                _currentUser = await apiClient.GetCurrentUserAsync();
            }
            catch (ApiException)
            {
                _currentUser = null;
            }

            _loaded = true;
        }

        return _currentUser is null ? Anonymous : Authenticated(_currentUser);
    }

    public async Task LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser = await apiClient.LoginAsync(request, cancellationToken);
        SetAuthenticated();
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser = await apiClient.RegisterAsync(request, cancellationToken);
        SetAuthenticated();
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await apiClient.LogoutAsync(cancellationToken);
        _currentUser = null;
        _loaded = true;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private void SetAuthenticated()
    {
        _loaded = true;
        NotifyAuthenticationStateChanged(Task.FromResult(Authenticated(_currentUser!)));
    }

    private static AuthenticationState Authenticated(CurrentUserResponse user)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        ], "Identity.Application");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
