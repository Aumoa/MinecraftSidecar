using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using OAuth2.DTO;

namespace SidecarWeb.Services;

public class AuthProvider(IHttpContextAccessor Accessor) : AuthenticationStateProvider
{
    private ClaimsPrincipal? m_User;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (m_User == null)
        {
            var httpContext = Accessor.HttpContext;
            if (httpContext != null)
            {
                var jwtToken = httpContext.Request.Cookies["id_token"];
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(jwtToken);
                    var claims = token.Claims.ToList();
                    var identity = new ClaimsIdentity(claims, "JwtAuthType");
                    var principal = new ClaimsPrincipal(identity);
                    m_User = principal;

                    Sub = m_User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                    Name = m_User.FindFirstValue(JwtRegisteredClaimNames.Name);
                    Email = m_User.FindFirstValue(JwtRegisteredClaimNames.Email);
                    Picture = m_User.FindFirstValue(JwtRegisteredClaimNames.Picture);
                }
            }

            m_User ??= new ClaimsPrincipal(new ClaimsIdentity());
        }

        return Task.FromResult(new AuthenticationState(m_User));
    }

    public string? Sub { get; private set; }

    public string? Name { get; private set; }

    public string? Email { get; private set; }

    public string? Picture { get; private set; }
}
