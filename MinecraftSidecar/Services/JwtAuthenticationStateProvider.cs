using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MinecraftSidecar.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<JwtAuthenticationStateProvider> _logger;
    private readonly TokenRefreshService _tokenRefreshService;
    private ClaimsPrincipal? _currentUser;

    public JwtAuthenticationStateProvider(
        IHttpContextAccessor accessor,
        ILogger<JwtAuthenticationStateProvider> logger,
        TokenRefreshService tokenRefreshService)
    {
        _accessor = accessor;
        _logger = logger;
        _tokenRefreshService = tokenRefreshService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser == null)
        {
            var httpContext = _accessor.HttpContext;
            if (httpContext != null)
            {
                var jwtToken = httpContext.Request.Cookies["id_token"];
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(jwtToken);

                        // Convert ValidTo to UTC for proper comparison
                        var tokenExpiryUtc = token.ValidTo.ToUniversalTime();
                        var now = DateTime.UtcNow;
                        var bufferTime = now.AddSeconds(30);
                        
                        _logger.LogDebug("Token expiry check - ValidTo: {ValidTo} (UTC: {ValidToUtc}), Now: {Now}, Buffer: {Buffer}", 
                            token.ValidTo, tokenExpiryUtc, now, bufferTime);

                        // Check if token is expired or near expiration (1 minute buffer)
                        if (tokenExpiryUtc < bufferTime)
                        {
                            var timeRemaining = tokenExpiryUtc - now;
                            _logger.LogInformation("JWT token expired or near expiration (remaining: {TimeRemaining}), attempting refresh", timeRemaining);
                            
                            // Try to refresh token
                            var refreshed = await _tokenRefreshService.TryRefreshTokenAsync();
                            
                            if (refreshed)
                            {
                                // Re-read the new token
                                jwtToken = httpContext.Request.Cookies["id_token"];
                                if (!string.IsNullOrEmpty(jwtToken))
                                {
                                    token = handler.ReadJwtToken(jwtToken);
                                    _logger.LogInformation("Token refreshed successfully, new expiry: {ValidTo}", token.ValidTo);
                                }
                                else
                                {
                                    _logger.LogWarning("Token refresh succeeded but no new token found in cookie");
                                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                                    return new AuthenticationState(_currentUser);
                                }
                            }
                            else
                            {
                                // Refresh failed, user needs to re-login
                                _logger.LogWarning("Token refresh failed, user needs to re-login");
                                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                                return new AuthenticationState(_currentUser);
                            }
                        }

                        var identity = new ClaimsIdentity(token.Claims, "JwtAuthType");
                        foreach (var claim in identity.FindAll("groups").ToArray())
                        {
                            var role = claim.Value switch
                            {
                                "admin" => "Administrator",
                                _ => "Guest"
                            };

                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }

                        _currentUser = new ClaimsPrincipal(identity);
                        
                        tokenExpiryUtc = token.ValidTo.ToUniversalTime();
                        var timeUntilExpiry = tokenExpiryUtc - DateTime.UtcNow;
                        _logger.LogDebug("JWT token validated successfully. Expires at: {ValidTo} UTC (in {TimeRemaining})", 
                            tokenExpiryUtc, timeUntilExpiry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse JWT token");
                        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                    }
                }
            }

            _currentUser ??= new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_currentUser);
    }

    public void ClearAuthenticationState()
    {
        _currentUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
