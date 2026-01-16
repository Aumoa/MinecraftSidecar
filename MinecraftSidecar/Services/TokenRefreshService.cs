using Microsoft.Extensions.Options;
using MinecraftSidecar.Options;
using OAuth2.DTO;

namespace MinecraftSidecar.Services;

public class TokenRefreshService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<OIDC> _oidcOptions;
    private readonly ILogger<TokenRefreshService> _logger;
    private readonly HttpClient _httpClient;

    public TokenRefreshService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<OIDC> oidcOptions,
        ILogger<TokenRefreshService> logger,
        HttpClient httpClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _oidcOptions = oidcOptions;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, cannot refresh token");
            return false;
        }

        var refreshToken = httpContext.Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogInformation("No refresh token found");
            return false;
        }

        try
        {
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _oidcOptions.Value.ClientId,
                ["client_secret"] = _oidcOptions.Value.ClientSecret
            };

            using var content = new FormUrlEncodedContent(formData);
            using var response = await _httpClient.PostAsync($"{_oidcOptions.Value.Uri}/api/v1/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                // Refresh token invalid, delete cookies
                httpContext.Response.Cookies.Delete("id_token");
                httpContext.Response.Cookies.Delete("refresh_token");
                return false;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse?.IdToken == null)
            {
                _logger.LogError("Token response is null or missing IdToken");
                return false;
            }

            // Update cookies with new tokens
            httpContext.Response.Cookies.Append("id_token", tokenResponse.IdToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                httpContext.Response.Cookies.Append("refresh_token", tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }

            _logger.LogInformation("Token refreshed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }
}
